# OscCore
A performance-oriented OSC ([Open Sound Control](http://opensoundcontrol.org/spec-1_0)) library for .NET.

## About this Fork

This is a fork of the [ChanyaVRC/OscCore](https://github.com/ChanyaVRC/OscCore) project, which is itself a descendant of the original OscCore project by [Stella Cannefax](https://github.com/stella3d/OscCore). Since these original project haven't seen updates for a few years, this fork was created to address bugs and add features needed to support work with OSC in .NET.

## Versions and Platforms

Releases are checked for compatibility with the latest release of these versions.
- **.NET Standard 2.1**

## Installation

Download & import the .nupkg from the [Releases](https://github.com/billdeitrick/OscCore/releases) page.

I can also download from [NuGet Package Manager](https://docs.microsoft.com/nuget/quickstart/install-and-use-a-package-in-visual-studio). 
See [nuget.org](https://www.nuget.org/packages/Buildetech.OscCore/) for the NuGet package latest version.

# Usage

## Receiving messages

### Using Code

##### Adding address handlers

There are several different kinds of method that can be registered with a server via script.

##### Single method

You can register a single callback, to be executed on the server's background thread immediately when a message is received, by calling `oscServer.TryAddMethod`.

If you have no need to queue a method to be called on the main thread, you probably want this one.
```csharp
var Server = OscServer.GetOrCreate(7000);

// add a single callback that reads message values at `/layers/1/opacity`
Server.TryAddMethod("/layers/1/opacity", ReadValues);

void ReadValues(OscMessageValues values)
{
    // call ReadElement methods here to extract values
}
```

###### Method pair

You can register a pair of methods to an address by calling `oscServer.TryAddMethodPair`.

These pairs consist of two methods, with the main thread one being optional.

1) Runs on background thread, immediate execution, just like single methods
2) Runs on main thread, queued on the next frame

This is useful for invoking events on the main thread, or any other use case that needs a main thread api.
_Read the message values in the first method._

```csharp
var server = OscServer.GetOrCreate(7000);

float messageValue = 0.0f;

void ReadValues(OscMessageValues values) 
{
    messageValue = values.ReadFloatElement(0);
}

void MainThreadMethod() 
{
    // do something with MessageValue on the main thread
}

public CallbackPairExample()
{
    // add a pair of methods for the OSC address "/layers/2/color/red"
    server.TryAddMethodPair("/layers/2/color/red", ReadValues, MainThreadMethod);
}
```

### Reading Message Values

Reading values from incoming messages is done on a per-element basis, using methods named like `Read<Type>Element(int elementIndex)`.  

Your value-reading methods will probably look something like this.

```csharp
// these methods would be registered as background thread callbacks for an address
int ReadSingleIntMessage(OscMessageValues values)
{
    return values.ReadIntElement(0);
}

int ReadTripleFloatMessage(OscMessageValues values)
{
    float x  = values.ReadFloatElement(0);
    float y  = values.ReadFloatElement(1);
    float z  = values.ReadFloatElement(2);
}
```

Most data types offer an `Unchecked` version of the method that is slightly faster, and still safe to use if you know for sure what data type an element is.
```csharp
double ReadUncheckedDoubleMessage(OscMessageValues values)
{
    return values.ReadFloat64ElementUnchecked(0);
}
```

##### Monitor Callbacks

If you just want to inspect message, you can add a monitor callback to be able to inspect every incoming message.

A monitor callback is an `Action<BlobString, OscMessageValues>`, where the blob string is the address.

## Sending Messages

### Using Code

[OscWriter](https://github.com/ChanyaVRC/OscCore/blob/netstandard/all-in-one/src/BuildSoft.OscCore/OscWriter.cs) handles serialization of individual message elements.

[OscClient](https://github.com/ChanyaVRC/OscCore/blob/netstandard/all-in-one/src/BuildSoft.OscCore/OscClient.cs) wraps a writer and sends whole messages.

Sending of complex messages with multiple elements hasn't been abstracted yet - take a look at the methods in `OscClient` to see how to send any message you need.

```csharp

OscClient Client = new OscClient("127.0.0.1", 7000);

// single a single float element message
Client.Send("/layers/1/opacity", 0.5f);

// send a blob
byte[] Blob = new byte[256];
Client.Send("/blobs", Blob, Blob.Length);

// send a string
Client.Send("/layers/3/name", "Textural");
```

#### Additional safety checks

Define `OSCCORE_SAFETY_CHECKS` in your project to have reads of message elements be bounds-checked.  It amounts to making sure the element you asked for isn't beyond the number of elements in the message.  

## Protocol Support Details

All OSC 1.0 types, required and non-standard are supported.  

The notable parts missing from [the spec](http://opensoundcontrol.org/spec-1_0) for the initial release are:
- **Matching incoming Address Patterns**

  "_A received OSC Message must be disptched to every OSC method in the current OSC Address Space whose OSC Address matches the OSC Message's OSC Address Pattern"_
   
   Currently, an exact address match is required for incoming messages.
   If our address space has two methods:
   - `/layer/1/opacity`
   - `/layer/2/opacity`

  and we get a message at `/layer/?/opacity`, we _should_ invoke both messages.

  Right now, we would not invoke either message.  We would only invoke messages received at exactly one of the two addresses.
  This is the first thing that i think will be implemented after initial release - some of the other packages also lack this feature, and you can get far without it.

- **Syncing to a source of absolute time**

  "_An OSC server must have access to a representation of the correct current absolute time_". 

  I've implemented this, as a class that syncs to an external NTP server, but without solving clock sync i'm not ready to add it.

- **Respecting bundle timestamps**

  "_If the time represented by the OSC Time Tag is before or equal to the current time, the OSC Server should invoke the methods immediately... Otherwise the OSC Time Tag represents a time in the future, and the OSC server must store the OSC Bundle until the specified time and then invoke the appropriate OSC Methods._"

  This is simple enough to implement, but without a mechanism for clock synchronization, it could easily lead to errors.  If the sending application worked from a different time source than OscCore, events would happen at the wrong time.

## Performance Details

##### Strings and Addresses

Every OSC message starts with an "address", specified as an ascii string.  
It's perfectly reasonable to represent this address in C# as a standard `string`, which is how other libraries work.

However, because strings in C# are immutable & UTF16, every time we receive a message from the network, this now requires us to allocate a new `string`, and in the process expand the received ascii string's bytes (where each character is a single byte) to UTF16 (each `char` is two bytes).   

OscCore eliminates both 
- string allocation
- the need to convert ascii bytes to UTF16

This works through leveraging the [BlobHandles](https://github.com/stella3d/BlobHandles) package.  
Incoming message's addresses are matched directly against their unmanaged ascii bytes.  

This has two benefits 
- no memory is allocated when a message is received
- takes less CPU time to parse a message
