using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using BlobHandles;

namespace Buildetech.OscCore;

public sealed unsafe class OscServer : IDisposable
{
    // used to allow easy removal of single callbacks
    private static readonly Dictionary<Action<OscMessageValues>, OscActionPair> _singleCallbackToPair = new();
    private readonly OscSocket _socket;
    private bool _disposed;
    private bool _started;
    private readonly Queue<Action> _mainThreadQueue = new();
    private readonly Dictionary<int, string> _byteLengthToStringBuffer = new();
    private readonly List<MonitorCallback> _monitorCallbacks = new();
    private readonly List<OscActionPair> _patternMatchedMethods = new();

    internal bool Running { get; set; }

    /// <summary>
    /// Map from port number to the server that handles incoming messages for it
    /// </summary>
    public static readonly Dictionary<int, OscServer> PortToServer = new();

    public int Port => _socket.Port;
    public OscAddressSpace AddressSpace { get; }
    public OscParser Parser { get; }

    public OscServer(int port, int bufferSize = 4096)
    {
        if (PortToServer.ContainsKey(port))
        {
            throw new ArgumentException($"port {port} is already in use, cannot start a new OSC Server on it", nameof(port));
        }

        _singleCallbackToPair.Clear();
        AddressSpace = new OscAddressSpace();
        Parser = new OscParser(bufferSize);
        _socket = new OscSocket(port, this);
        Start();
    }

    public void Start()
    {
        // make sure redundant calls don't do anything after the first
        if (_started)
        {
            Running = true;
            return;
        }

        _socket.Start();

        _disposed = false;
        _started = true;
        Running = true;
    }

    /// <summary>
    /// Get an existing OSC server on the given port, or create one if it doesn't exist.
    /// </summary>
    /// <param name="port">The port to listen for incoming message on</param>
    /// <returns></returns>
    public static OscServer GetOrCreate(int port)
    {
        if (!PortToServer.TryGetValue(port, out var server))
        {
            server = new OscServer(port);
            PortToServer[port] = server;
        }
        return server;
    }

    /// <summary>Dispose of an OSC Server</summary>
    /// <param name="port">The port associated with the server</param>
    /// <returns>True if the server was found and disposed of, false otherwise</returns>
    public static bool Remove(int port)
    {
        if (PortToServer.TryGetValue(port, out var server))
        {
            server.Dispose();
            return PortToServer.Remove(port);
        }
        return false;
    }

    /// <summary>
    /// Register a single background thread method for an OSC address
    /// </summary>
    /// <param name="address">The OSC address to handle messages for</param>
    /// <param name="valueReadMethod">
    /// The method to execute immediately on the worker thread that reads values from the message
    /// </param>
    /// <returns>True if the address was valid, false otherwise</returns>
    public bool TryAddMethod(string address, Action<OscMessageValues> valueReadMethod)
    {
        var pair = new OscActionPair(valueReadMethod);
        _singleCallbackToPair.Add(valueReadMethod, pair);
        return AddressSpace.TryAddMethod(address, pair);
    }

    /// <summary>
    /// Remove a single background thread method from an OSC address
    /// </summary>
    /// <param name="address">The OSC address to handle messages for</param>
    /// <param name="valueReadMethod">
    /// The method to execute immediately on the worker thread that reads values from the message
    /// </param>
    /// <returns>True if the method was removed from this address, false otherwise</returns>
    public bool RemoveMethod(string address, Action<OscMessageValues> valueReadMethod)
    {
        if (_singleCallbackToPair.TryGetValue(valueReadMethod, out var pair))
        {
            return AddressSpace.RemoveMethod(address, pair) &&
                    _singleCallbackToPair.Remove(valueReadMethod);
        }

        return false;
    }

    /// <summary>Remove an address and all its methods from the server's address space</summary>
    /// <param name="address">The OSC address to handle messages for</param>
    /// <returns>True if the method address was found and removed, false otherwise</returns>
    public bool RemoveAddress(string address)
    {
        return AddressSpace.RemoveAddressMethod(address);
    }

    /// <summary>
    /// Add a background thread read callback and main thread callback associated with an OSC address.
    /// </summary>
    /// <param name="address">The OSC address to associate a method with</param>
    /// <param name="actionPair">The pair of callbacks to add</param>
    /// <returns>True if the address was valid and methods associated with it, false otherwise</returns>
    public bool TryAddMethodPair(string address, OscActionPair actionPair) =>
        AddressSpace.TryAddMethod(address, actionPair);

    /// <summary>
    /// Add a background thread read callback and main thread callback associated with an OSC address.
    /// </summary>
    /// <param name="address">The OSC address to associate a method with</param>
    /// <param name="read"></param>
    /// <param name="mainThread"></param>
    /// <returns>True if the address was valid and methods associated with it, false otherwise</returns>
    public bool TryAddMethodPair(string address, Action<OscMessageValues> read, Action mainThread) =>
        AddressSpace.TryAddMethod(address, new OscActionPair(read, mainThread));

    /// <summary>
    /// Remove a background thread read callback and main thread callback associated with an OSC address.
    /// </summary>
    /// <param name="address">The OSC address to remove methods from</param>
    /// <param name="actionPair">The pair of callbacks to remove</param>
    /// <returns>True if successfully removed, false otherwise</returns>
    public bool RemoveMethodPair(string address, OscActionPair actionPair)
    {
        // if the address space is null, this got called during cleanup / shutdown,
        // and effectively all addresses are removed by setting it to null
        return AddressSpace == null || AddressSpace.RemoveMethod(address, actionPair);
    }

    /// <summary>
    /// Add a method to be invoked every time an OSC message is received. If there are any monitor callbacks added,
    /// memory has to be allocated for every message received, so it's recommended to only do this while editing.
    /// </summary>
    /// <param name="callback">The method to invoke</param>
    public void AddMonitorCallback(MonitorCallback callback)
    {
        _monitorCallbacks.Add(callback);
    }

    /// <summary>Remove a monitor method</summary>
    /// <param name="callback">The method to remove</param>
    public bool RemoveMonitorCallback(MonitorCallback callback)
    {
        return _monitorCallbacks.Remove(callback);
    }

    /// <summary>Must be called on the main thread every frame to handle queued events</summary>
    public void Update()
    {
        var queue = _mainThreadQueue;
        while (queue.Count > 0)
        {
            queue.Dequeue().Invoke();
        }
    }

    /// <summary>
    /// Parse a single OSC message that's been copied into the start of the buffer.
    /// Bundled messages can contain multiple elements
    /// </summary>
    /// <param name="byteLength">The length of the received message</param>
    public void ParseBuffer(int byteLength)
    {
        var parser = Parser;
        fixed (byte* bufferPtr = parser._buffer)
        {
            var addressToMethod = AddressSpace._addressToMethod;

            // determine if the message is a bundle or not 
            if (*(long*)bufferPtr != Constant.BundlePrefixLong)
            {
                // address length here doesn't include the null terminator and alignment padding.
                // this is so we can look up the address by only its content bytes.
                // var addressLength = parser.FindUnalignedAddressLength();
                var addressLength = parser.Parse();
                if (addressLength < 0)
                    return;    // address didn't start with '/'

                // see if we have a method registered for this address
                if (addressToMethod.TryGetValueFromBytes(bufferPtr, addressLength, out var methodPair))
                {
                    HandleCallbacks(methodPair, parser.MessageValues);
                }
                else if (AddressSpace._patternCount > 0)
                {
                    TryMatchPatterns(parser, bufferPtr, addressLength);
                }

                if (_monitorCallbacks.Count > 0)
                    HandleMonitorCallbacks(bufferPtr, addressLength, parser);

                return;
            }

            // the message is a bundle, so we need to recursively scan the bundle elements
            int MessageOffset = 0;
            bool recurse;
            // the outer do-while loop runs once for every #bundle encountered
            do
            {
                // Timestamp isn't used yet, but it will be eventually
                // var time = parser.MessageValues.ReadTimestampIndex(MessageOffset + 8);
                // '#bundle ' + timestamp = 16 bytes
                MessageOffset += 16;
                recurse = false;

                // the inner while loop runs once per bundle element
                while (MessageOffset < byteLength && !recurse)
                {
                    var messageSize = (int)parser.MessageValues.ReadUIntIndex(MessageOffset);
                    var contentIndex = MessageOffset + 4;

                    if (parser.IsBundleTagAtIndex(contentIndex))
                    {
                        // this bundle element's contents are a bundle, break out to the outer loop to scan it
                        MessageOffset = contentIndex;
                        recurse = true;
                        continue;
                    }

                    // parse the actual contents of this bundle element just like a non-bundled message
                    var bundleAddressLength = parser.Parse(contentIndex);
                    if (bundleAddressLength <= 0)
                    {
                        // if an error occured parsing the content, skip this messagse entirely
                        MessageOffset += messageSize + 4;
                        continue;
                    }

                    var contentPtr = bufferPtr + contentIndex;
                    if (addressToMethod.TryGetValueFromBytes(contentPtr, bundleAddressLength, out var bundleMethodPair))
                    {
                        HandleCallbacks(bundleMethodPair, parser.MessageValues);
                    }
                    // if we have no handler for this exact address, we may have a pattern that matches it
                    else if (AddressSpace._patternCount > 0)
                    {
                        TryMatchPatterns(parser, bufferPtr, bundleAddressLength);
                    }

                    MessageOffset += messageSize + 4;

                    if (_monitorCallbacks.Count > 0)
                        HandleMonitorCallbacks(contentPtr, bundleAddressLength, parser);
                }
            }
            // restart the outer while loop every time a bundle within a bundle is detected
            while (recurse);
        }
    }

    private void HandleCallbacks(OscActionPair pair, OscMessageValues messageValues)
    {
        // call the value read method associated with this OSC address    
        pair.ValueRead(messageValues);

        // if there's a main thread method, queue it
        if (pair.MainThreadQueued != null)
        {
            _mainThreadQueue.Enqueue(pair.MainThreadQueued);
        }
    }

    private void HandleMonitorCallbacks(byte* bufferPtr, int addressLength, OscParser parser)
    {
        // handle monitor callbacks
        var monitorAddressStr = new BlobString(bufferPtr, addressLength);
        foreach (var callback in _monitorCallbacks)
            callback(monitorAddressStr, parser.MessageValues);
    }

    private void TryMatchPatterns(OscParser parser, byte* bufferPtr, int addressLength)
    {
        // to support OSC address patterns, we test unmatched addresses against regular expressions
        // To do that, we need it as a regular string.  We may be able to mutate a previous string, 
        // instead of always allocating a new one
        if (!_byteLengthToStringBuffer.TryGetValue(addressLength, out var stringBuffer))
        {
            stringBuffer = Encoding.ASCII.GetString(bufferPtr, addressLength);
            _byteLengthToStringBuffer[addressLength] = stringBuffer;
        }
        else
        {
            // If we've previously received a message of the same byte length, we can re-use it
            OverwriteAsciiString(stringBuffer, bufferPtr);
        }

        if (AddressSpace.TryMatchPatternHandler(stringBuffer, _patternMatchedMethods))
        {
            var bufferCopy = string.Copy(stringBuffer);
            AddressSpace._addressToMethod.Add(bufferCopy, _patternMatchedMethods);
            foreach (var (valueRead, mainThreadQueued) in _patternMatchedMethods)
            {
                valueRead(parser.MessageValues);
                if (mainThreadQueued != null)
                {
                    _mainThreadQueue.Enqueue(mainThreadQueued);
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void OverwriteAsciiString(string str, byte* bufferPtr)
    {
        fixed (char* addressPtr = str)
        {
            for (int i = 0; i < str.Length; i++)
                addressPtr[i] = (char)bufferPtr[i];
        }
    }

    public void Dispose()
    {
        PortToServer.Remove(Port);

        if (_disposed) return;
        _disposed = true;

        AddressSpace._addressToMethod.Dispose();
        _socket.Dispose();
    }

    ~OscServer()
    {
        Dispose();
    }

    public int CountHandlers()
    {
        return AddressSpace._addressToMethod._sourceToBlob.Count;
    }
}
