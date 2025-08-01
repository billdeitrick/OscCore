using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Buildetech.OscCore.UnityObjects;

namespace Buildetech.OscCore;

public class OscClient : IDisposable
{
    /// <summary>
    /// The socket for sending messages
    /// </summary>
    public Socket Socket { get; }

    /// <summary>Serializes outgoing messages</summary>
    public OscWriter Writer { get; }

    /// <summary>Where this client is sending messages to</summary>
    public IPEndPoint Destination { get; }

    public OscClient(string ipAddress, int port)
    {
        Writer = new OscWriter();

        Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        if (ipAddress == "255.255.255.255")
            Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);

        Destination = new IPEndPoint(IPAddress.Parse(ipAddress), port);
        Socket.Connect(Destination);
    }

    ~OscClient()
    {
        Dispose();
    }

    /// <summary>Send a message with no elements</summary>
    public void Send(string address)
    {
        Writer.Reset();
        Writer.Write(address);
        Writer.Write(",");
        Socket.Send(Writer.Buffer, Writer.Length, SocketFlags.None);
    }

    private const uint Int32TypeTagBytes = 26924;    // ",i  " 

    /// <summary>Send a message with a single 32-bit integer element</summary>
    public void Send(string address, int element)
    {
        Writer.WriteAddressAndTags(address, Int32TypeTagBytes);
        Writer.Write(element);
        Socket.Send(Writer.Buffer, Writer.Length, SocketFlags.None);
    }

    private const uint Float32TypeTagBytes = 26156;    // ",f  " 

    /// <summary>Send a message with a single 32-bit float element</summary>
    public void Send(string address, float element)
    {
        Writer.WriteAddressAndTags(address, Float32TypeTagBytes);
        Writer.Write(element);
        Socket.Send(Writer.Buffer, Writer.Length, SocketFlags.None);
    }

    private const uint StringTypeTagBytes = 29484;    // ",s  " 

    /// <summary>Send a message with a single string element</summary>
    public void Send(string address, string element)
    {
        Writer.WriteAddressAndTags(address, StringTypeTagBytes);
        Writer.Write(element);
        Socket.Send(Writer.Buffer, Writer.Length, SocketFlags.None);
    }

    private const uint BlobTypeTagBytes = 25132;    // ",b  " 

    /// <summary>Send a message with a single blob element</summary>
    /// <param name="address">The OSC address</param>
    /// <param name="bytes">The bytes to copy from</param>
    /// <param name="length">The number of bytes in the blob element</param>
    /// <param name="start">The index in the bytes array to start copying from</param>
    public void Send(string address, byte[] bytes, int length, int start = 0)
    {
        Writer.WriteAddressAndTags(address, BlobTypeTagBytes);
        Writer.Write(bytes, length, start);
        Socket.Send(Writer.Buffer, Writer.Length, SocketFlags.None);
    }

    /// <summary>Send a message with 2 32-bit float elements</summary>
    public void Send(string address, Vector2 element)
    {
        Writer.Reset();
        Writer.Write(address);
        const string typeTags = ",ff";
        Writer.Write(typeTags);
        Writer.Write(element);
        Socket.Send(Writer.Buffer, Writer.Length, SocketFlags.None);
    }

    /// <summary>Send a message with 3 32-bit float elements</summary>
    public void Send(string address, Vector3 element)
    {
        Writer.Reset();
        Writer.Write(address);
        const string typeTags = ",fff";
        Writer.Write(typeTags);
        Writer.Write(element);
        Socket.Send(Writer.Buffer, Writer.Length, SocketFlags.None);
    }

    private const uint Int64TypeTagBytes = 25644;    // ",d  " 

    /// <summary>Send a message with a single 64-bit float element</summary>
    public void Send(string address, double element)
    {
        Writer.WriteAddressAndTags(address, Int64TypeTagBytes);
        Writer.Write(element);
        Socket.Send(Writer.Buffer, Writer.Length, SocketFlags.None);
    }

    private const uint Float64TypeTagBytes = 26668;    // ",h  " 

    /// <summary>Send a message with a single 64-bit integer element</summary>
    public void Send(string address, long element)
    {
        Writer.WriteAddressAndTags(address, Float64TypeTagBytes);
        Writer.Write(element);
        Socket.Send(Writer.Buffer, Writer.Length, SocketFlags.None);
    }

    private const uint Color32TypeTagBytes = 29228;    // ",r  " 

    /// <summary>Send a message with a single 32-bit color element</summary>
    public void Send(string address, Color32 element)
    {
        Writer.WriteAddressAndTags(address, Color32TypeTagBytes);
        Writer.Write(element);
        Socket.Send(Writer.Buffer, Writer.Length, SocketFlags.None);
    }

    private const uint MidiTypeTagBytes = 27948;    // ",m  " 

    /// <summary>Send a message with a single MIDI message element</summary>
    public void Send(string address, MidiMessage element)
    {
        Writer.WriteAddressAndTags(address, MidiTypeTagBytes);
        Writer.Write(element);
        Socket.Send(Writer.Buffer, Writer.Length, SocketFlags.None);
    }

    private const uint CharTypeTagBytes = 25388;    // ",c  " 

    /// <summary>Send a message with a single ascii character element</summary>
    public void Send(string address, char element)
    {
        Writer.WriteAddressAndTags(address, CharTypeTagBytes);
        Writer.Write(element);
        Socket.Send(Writer.Buffer, Writer.Length, SocketFlags.None);
    }

    private const uint TrueTypeTagBytes = 21548;    // ",T  " 
    private const uint FalseTypeTagBytes = 17964;    // ",F  " 

    /// <summary>Send a message with a single True or False tag element</summary>
    public void Send(string address, bool element)
    {
        Writer.WriteAddressAndTags(address, element ? TrueTypeTagBytes : FalseTypeTagBytes);
        Socket.Send(Writer.Buffer, Writer.Length, SocketFlags.None);
    }

    private const uint NilTypeTagBytes = 20012;    // ",N  " 

    /// <summary>Send a message with a single Nil ('N') tag element</summary>
    public void SendNil(string address)
    {
        Writer.WriteAddressAndTags(address, NilTypeTagBytes);
        Socket.Send(Writer.Buffer, Writer.Length, SocketFlags.None);
    }

    private const uint InfinitumTypeTagBytes = 18732;    // ",I  " 

    /// <summary>Send a message with a single Infinitum ('I') tag element</summary>
    public void SendInfinitum(string address)
    {
        Writer.WriteAddressAndTags(address, InfinitumTypeTagBytes);
        Socket.Send(Writer.Buffer, Writer.Length, SocketFlags.None);
    }

    private static unsafe uint[] GetAlignedAsciiBytes(string input)
    {
        var count = Encoding.ASCII.GetByteCount(input);
        var alignedCount = (count + 3) & ~3;
        var bytes = new uint[alignedCount / 4];

        fixed (uint* bPtr = bytes)
        fixed (char* strPtr = input)
        {
            Encoding.ASCII.GetBytes(strPtr, input.Length, (byte*)bPtr, count);
        }

        return bytes;
    }

    private bool _isDisporsed = false;
    public void Dispose()
    {
        if (!_isDisporsed)
        {
            Socket.Dispose();
            Writer.Dispose();
            _isDisporsed = true;
        }
    }
}
