using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Buildetech.OscCore;

public unsafe class OscParser
{
    // TODO - make these preferences options
    public const int MaxElementsPerMessage = 32;
    public const int MaxBlobSize = 1024 * 256;

    internal readonly byte[] _buffer;

    /// <summary>
    /// Holds all parsed values.  After calling Parse(), this should have data available to read
    /// </summary>
    public readonly OscMessageValues MessageValues;

    /// <summary>Create a new parser.</summary>
    /// <param name="fixedBuffer">The buffer to read messages from.</param>
    [Obsolete("Use OscParser(int bufferLength)")]
    public OscParser(byte[] fixedBuffer)
    {
        _buffer = fixedBuffer;
        MessageValues = new OscMessageValues(_buffer, MaxElementsPerMessage);
    }

    /// <summary>Create a new parser.</summary>
    /// <param name="capacity">The capacity of buffer to read messages from.</param>
    public OscParser(int capacity = 4096)
    {
        if (capacity < 256)
        {
            capacity = 4096;
        }
        _buffer = new byte[capacity];
        MessageValues = new OscMessageValues(_buffer, MaxElementsPerMessage);
    }

    /// <summary>
    /// Parse a single non-bundle message that starts at the beginning of the buffer
    /// </summary>
    /// <returns>The unaligned length of the message address</returns>
    public int Parse()
    {
        // address length here doesn't include the null terminator and alignment padding.
        // this is so we can look up the address by only its content bytes.
        var addressLength = FindUnalignedAddressLength();
        if (addressLength < 0)
            return addressLength;    // address didn't start with '/'

        var alignedAddressLength = (addressLength + 3) & ~3;
        // if the null terminator after the string comes at the beginning of a 4-byte block,
        // we need to add 4 bytes of padding
        if (alignedAddressLength == addressLength)
            alignedAddressLength += 4;

        var tagSize = ParseTags(_buffer, alignedAddressLength);
        var offset = alignedAddressLength + (tagSize + 4) & ~3;
        FindOffsets(offset);
        FindAddress();
        return addressLength;
    }

    /// <summary>
    /// Parse a single non-bundle message that starts at the given byte offset from the start of the buffer
    /// </summary>
    /// <returns>The unaligned length of the message address</returns>
    public int Parse(int startingByteOffset)
    {
        // address length here doesn't include the null terminator and alignment padding.
        // this is so we can look up the address by only its content bytes.
        var addressLength = FindUnalignedAddressLength(startingByteOffset);
        if (addressLength < 0)
            return addressLength;    // address didn't start with '/'

        var alignedAddressLength = (addressLength + 3) & ~3;
        // if the null terminator after the string comes at the beginning of a 4-byte block,
        // we need to add 4 bytes of padding
        if (alignedAddressLength == addressLength)
            alignedAddressLength += 4;

        var startPlusAlignedLength = startingByteOffset + alignedAddressLength;
        var tagSize = ParseTags(_buffer, startPlusAlignedLength);
        var offset = startPlusAlignedLength + (tagSize + 4) & ~3;
        FindOffsets(offset);
        FindAddress(offset);
        return addressLength;
    }

    internal static bool AddressIsValid(string address)
    {
        if (address[0] != '/') return false;

        foreach (var chr in address)
        {
            switch (chr)
            {
                case ' ':
                case '#':
                case '*':
                case ',':
                case '?':
                case '[':
                case ']':
                case '{':
                case '}':
                    return false;
            }
        }

        return true;
    }

    internal static bool CharacterIsValidInAddress(char c)
    {
        return c switch
        {
            ' ' or '#' or '*' or ',' or '?' or '[' or ']' or '{' or '}' => false,
            _ => true,
        };
    }

    internal static AddressType GetAddressType(string address)
    {
        if (address[0] != '/') return AddressType.Invalid;

        var addressValid = true;
        foreach (var chr in address)
        {
            switch (chr)
            {
                case ' ':
                case '#':
                case '*':
                case ',':
                case '?':
                case '[':
                case ']':
                case '{':
                case '}':
                    addressValid = false;
                    break;
            }
        }

        if (addressValid) return AddressType.Address;

        // if the address isn't valid, it might be a valid address pattern.
        foreach (var chr in address)
        {
            switch (chr)
            {
                case ' ':
                case '#':
                case ',':
                    return AddressType.Invalid;
            }
        }

        return AddressType.Pattern;
    }

    /// <returns> Size of tags in bytes, including ',' </returns>
    internal int ParseTags(byte[] bytes, int start = 0)
    {
        if (bytes[start] != Constant.Comma) return 0;

        var tagIndex = start + 1;         // skip the starting ','
        var outIndex = 0;
        var tags = MessageValues._tags;
        while (true)
        {
            var tag = (TypeTag)bytes[tagIndex];
            if (!tag.IsSupported()) break;
            tags[outIndex] = tag;
            tagIndex++;
            outIndex++;
        }

        MessageValues.ElementCount = outIndex;

        return outIndex + 1; // +1 includes the starting ',' in the tagSize
    }

    public int FindUnalignedAddressLength()
    {
        if (_buffer[0] != Constant.ForwardSlash)
            return -1;

        var index = 1;
        do
        {
            index++;
        }
        while (_buffer[index] != byte.MinValue);
        return index;
    }

    public int FindUnalignedAddressLength(int offset)
    {
        if (_buffer[offset] != Constant.ForwardSlash)
            return -1;

        var index = offset + 1;
        do
        {
            index++;
        }
        while (_buffer[index] != byte.MinValue);

        var length = index - offset;
        return length;
    }

    public int GetStringLength(int offset)
    {
        var end = _buffer.Length - offset;
        int index;
        for (index = offset; index < end; index++)
        {
            if (_buffer[index] == 0) break; // we break on the null terminator
        }

        var length = index - offset + 1;  // need to account for the null terminator when aligning to 32 bits
        return (length + 3) & ~3;            // align to 4 bytes
    }

    /// <summary>
    /// Find the address string in the OSC packet and store it in the MessageValues property.
    /// </summary>
    /// <param name="offset">The starting offsetfor parsing the address, defaults to 0</param>
    public void FindAddress(int offset = 0)
    {
        
        int addressLength =  FindUnalignedAddressLength(offset);
        byte[] addressBytes = new byte[addressLength];
        Buffer.BlockCopy(_buffer, offset, addressBytes, 0, addressLength);
        MessageValues.Address = System.Text.Encoding.ASCII.GetString(addressBytes);

    }

    /// <summary>Find the byte offsets for each element of the message</summary>
    /// <param name="offset">The byte index of the first value</param>
    public void FindOffsets(int offset)
    {
        var tags = MessageValues._tags;
        var offsets = MessageValues._offsets;
        for (int i = 0; i < MessageValues.ElementCount; i++)
        {
            offsets[i] = offset;
            switch (tags[i])
            {
                // false, true, nil, infinitum & array[] tags add 0 to the offset
                case TypeTag.Int32:
                case TypeTag.Float32:
                case TypeTag.Color32:
                case TypeTag.AsciiChar32:
                case TypeTag.MIDI:
                    offset += 4;
                    break;
                case TypeTag.Float64:
                case TypeTag.Int64:
                case TypeTag.TimeTag:
                    offset += 8;
                    break;
                case TypeTag.String:
                case TypeTag.AltTypeString:
                    offset += GetStringLength(offset);
                    break;
                case TypeTag.Blob:
                    // read the int that specifies the size of the blob
                    offset += (7 + ((_buffer[offset] << 24) | (_buffer[offset + 1] << 16) | (_buffer[offset + 2] << 8) |
                              _buffer[offset + 3])) & ~3; // ensure we interpret as big-endian and align to 4 bytes
                    break;
            }
        }
    }

    /// <summary>
    /// Test if '#bundle ' is present at a given index in the buffer 
    /// </summary>
    /// <param name="index">The index in the buffer to test</param>
    /// <returns>True if present, false otherwise</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsBundleTagAtIndex(int index)
    {
        return Unsafe.As<byte, long>(ref _buffer[index]) == Constant.BundlePrefixLong;
    }

    public static int FindArrayLength(byte[] bytes, int offset = 0)
    {
        if ((TypeTag)bytes[offset] != TypeTag.ArrayStart)
            return -1;

        var index = offset + 1;
        while (bytes[index] != (byte)TypeTag.ArrayEnd)
            index++;

        return index - offset;
    }
}

