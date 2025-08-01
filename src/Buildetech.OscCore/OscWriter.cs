using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BlobHandles;
using Buildetech.OscCore.UnityObjects;
using MiniNtp;

namespace Buildetech.OscCore;

public sealed unsafe class OscWriter : IDisposable
{
    public byte[] Buffer { get; }

    private int _length;

    /// <summary>The number of bytes currently written to the buffer</summary>
    public int Length => _length;

    public OscWriter(int capacity = 4096)
    {
        Buffer = new byte[capacity];
    }

    ~OscWriter() { Dispose(); }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset() { _length = 0; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteToBigEndian(byte* bytes, int length)
    {
        int offset = _length;
        _length += length;
        if (!BitConverter.IsLittleEndian)
        {
            fixed (byte* dest = &Buffer[offset])
            {
                System.Buffer.MemoryCopy(bytes, dest, length, length);
            }
        }

        if (length == 0)
        {
            return;
        }
        if (length == 4)
        {
            fixed (byte* dest = &Buffer[offset])
            {
                dest[0] = bytes[3];
                dest[1] = bytes[2];
                dest[2] = bytes[1];
                dest[3] = bytes[0];
            }
            return;
        }
        if (length == 8)
        {
            fixed (byte* dest = &Buffer[offset])
            {
                dest[0] = bytes[7];
                dest[1] = bytes[6];
                dest[2] = bytes[5];
                dest[3] = bytes[4];
                dest[4] = bytes[3];
                dest[5] = bytes[2];
                dest[6] = bytes[1];
                dest[7] = bytes[0];
            }
            return;
        }
        WriteAsLittleEndianWithLoop(bytes, length, offset);
    }

    private void WriteAsLittleEndianWithLoop(byte* bytes, int length, int bufferOffset)
    {
        fixed (byte* dest = &Buffer[bufferOffset])
        {
            for (int i = 0; i < length; i++)
            {
                dest[i] = bytes[length - i];
            }
        }
    }

    /// <summary>Write a 32-bit integer element</summary>
    public void Write(int data)
    {
        WriteToBigEndian((byte*)&data, sizeof(int));
    }

    /// <summary>Write a 32-bit floating point element</summary>
    public void Write(float data)
    {
        WriteToBigEndian((byte*)&data, sizeof(float));
    }

    /// <summary>Write a 2D vector as two float elements</summary>
    public void Write(Vector2 data)
    {
        Write(data.x);
        Write(data.y);
    }

    /// <summary>Write a 3D vector as three float elements</summary>
    public void Write(Vector3 data)
    {
        Write(data.x);
        Write(data.y);
        Write(data.z);
    }

    /// <summary>Write an ASCII string element. The string MUST be ASCII-encoded!</summary>
    public void Write(string data)
    {
        foreach (var chr in data)
            Buffer[_length++] = (byte)chr;

        var alignedLength = (data.Length + 3) & ~3;
        // if our length was already aligned to 4 bytes, that means we don't have a string terminator yet,
        // so we need to write one, which requires aligning to the next 4-byte mark.
        if (alignedLength == data.Length)
            alignedLength += 4;

        for (int i = data.Length; i < alignedLength; i++)
            Buffer[_length++] = 0;
    }

    /// <summary>Write an ASCII string element. The string MUST be ASCII-encoded!</summary>
    public void Write(BlobString data)
    {
        var strLength = data.Length;
        Unsafe.CopyBlock(ref Unsafe.AsRef(Buffer[_length]), ref data.Handle.Reference, (uint)strLength);
        _length += strLength;

        var alignedLength = (data.Length + 3) & ~3;
        if (alignedLength == data.Length)
            alignedLength += 4;

        for (int i = data.Length; i < alignedLength; i++)
            Buffer[_length++] = 0;
    }

    /// <summary>Write a blob element</summary>
    /// <param name="bytes">The bytes to copy from</param>
    /// <param name="length">The number of bytes in the blob element</param>
    /// <param name="start">The index in the bytes array to start copying from</param>
    public void Write(byte[] bytes, int length, int start = 0)
    {
        if (start + length > bytes.Length)
            return;

        Write(length);
        System.Buffer.BlockCopy(bytes, start, Buffer, _length, length);
        _length += length;

        // write any trailing zeros necessary
        var remainder = ((length + 3) & ~3) - length;
        for (int i = 0; i < remainder; i++)
        {
            Buffer[_length++] = 0;
        }
    }

    /// <summary>Write a 64-bit integer element</summary>
    public void Write(long data)
    {
        WriteToBigEndian((byte*)&data, sizeof(long));
    }

    /// <summary>Write a 64-bit floating point element</summary>
    public void Write(double data)
    {
        WriteToBigEndian((byte*)&data, sizeof(double));
    }

    /// <summary>Write a 32-bit RGBA color element</summary>
    public void Write(Color32 data)
    {
        WriteToBigEndian((byte*)&data, sizeof(Color32));
    }

    /// <summary>Write a MIDI message element</summary>
    public void Write(MidiMessage data)
    {
        Unsafe.As<byte, MidiMessage>(ref Buffer[_length]) = data;
        _length += 4;
    }

    /// <summary>Write a 64-bit NTP timestamp element</summary>
    public void Write(NtpTimestamp time)
    {
        fixed (byte* buffer = &Buffer[_length])
        {
            time.ToBigEndianBytes((uint*)buffer);
        }
        _length += 8;
    }

    /// <summary>Write a single ascii character element</summary>
    public void Write(char data)
    {
        // char is written in the last byte of the 4-byte block;
        Buffer[_length + 3] = (byte)data;
        _length += 4;
    }

    /// <summary>Write '#bundle ' at the start of a bundled message</summary>
    public void WriteBundlePrefix()
    {
        const int size = 8;
        // TODO replace with dereferencing the long  version ?
        System.Buffer.BlockCopy(Constant.BundlePrefixBytes, 0, Buffer, _length, size);
        _length += size;
    }

    /// <summary>
    /// Combines Reset(), Write(address), and Write(tags) in a single function to reduce call overhead
    /// </summary>
    /// <param name="address">The OSC address to send to</param>
    /// <param name="tags">4 bytes that represent up to 3 type tags</param>
    public void WriteAddressAndTags(string address, uint tags)
    {
        _length = 0;
        foreach (var chr in address)
            Buffer[_length++] = (byte)chr;

        var alignedLength = (address.Length + 3) & ~3;
        // if our length was already aligned to 4 bytes, that means we don't have a string terminator yet,
        // so we need to write one, which requires aligning to the next 4-byte mark.
        if (alignedLength == address.Length)
            alignedLength += 4;

        for (int i = address.Length; i < alignedLength; i++)
            Buffer[_length++] = 0;

        // write the 4 bytes for the type tags
        Unsafe.As<byte, uint>(ref Buffer[_length]) = tags;
        _length += 4;
    }

    public void CopyBuffer(byte[] copyTo, int copyOffset = 0)
    {
        System.Buffer.BlockCopy(Buffer, 0, copyTo, copyOffset, Length);
    }

    public void Dispose()
    {

    }
}
