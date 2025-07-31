using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace BuildSoft.OscCore;

internal sealed class OscSocket : IDisposable
{
    private readonly Socket _socket;
    private readonly Task _task;
    private bool _disposed;
    private bool _started;

    public int Port { get; }
    public OscServer Server { get; }

    public OscSocket(int port, OscServer server)
    {
        Port = port;
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp) { ReceiveTimeout = int.MaxValue };
        _task = new Task(Serve, TaskCreationOptions.LongRunning);
        Server = server;
    }

    public void Start()
    {
        // make sure redundant calls don't do anything after the first
        if (_started) return;

        _disposed = false;
        if (!_socket.IsBound)
            _socket.Bind(new IPEndPoint(IPAddress.Any, Port));

        _task.Start();
        _started = true;
    }

    private void Serve()
    {
        var buffer = Server.Parser._buffer;
        var socket = _socket;

        while (!_disposed)
        {
            try
            {
                // it's probably better to let Receive() block the thread than test socket.Available > 0 constantly
                int receivedByteCount = socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);
                if (receivedByteCount == 0) continue;
                Server.ParseBuffer(receivedByteCount);
            }
            // a read timeout can result in a socket exception, should just be ok to ignore
            catch (SocketException) { }
            catch (Exception)
            {
                if (!_disposed) throw;
                break;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Otherwise, the receive loop blocks and we hang on shutdown
        // This bumps us out of the receive loop, so the Serve method can
        // see that we're disposing the object.
        using (var dummy = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
        {
            dummy.SendTo(new byte[] { }, new IPEndPoint(IPAddress.Loopback, Port));
        }
        
        _socket.Dispose();
    }
}
