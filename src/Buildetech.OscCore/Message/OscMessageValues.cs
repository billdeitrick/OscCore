using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Buildetech.OscCore.UnityObjects;

namespace Buildetech.OscCore;

/// <summary>
/// Represents the tags and values associated with a received OSC message
/// </summary>
public sealed unsafe partial class OscMessageValues
{
    [StructLayout(LayoutKind.Explicit)]
    private struct ConvertBuffer
    {
        [FieldOffset(0)]
        public fixed byte Bits32[4];

        [FieldOffset(0)]
        public fixed byte Bits64[8];

        [FieldOffset(0)]
        public float @float;

        [FieldOffset(0)]
        public uint @uint;

        [FieldOffset(0)]
        public Color32 Color32;

        [FieldOffset(0)]
        public double @double;
    }

    // the buffer where we read messages from - usually provided + filled by a socket reader
    private readonly byte[] _sharedBuffer;

    /// <summary>
    /// All type tags in the message.
    /// All values past index >= ElementCount are junk data and should NEVER BE USED!
    /// </summary>
    internal readonly TypeTag[] _tags;

    /// <summary>
    /// Indexes into the shared buffer associated with each message element
    /// All values at index >= ElementCount are junk data and should NEVER BE USED!
    /// </summary>
    internal readonly int[] _offsets;

    /// <summary>The number of elements in the OSC Message</summary>
    public int ElementCount { get; internal set; }
    
    /// <summary>The OSC message address.</summary>
    public string Address { get; internal set; }

    internal OscMessageValues(byte[] buffer, int elementCapacity = 8)
    {
        ElementCount = 0;
        Address = string.Empty;
        _tags = new TypeTag[elementCapacity];
        _offsets = new int[elementCapacity];
        _sharedBuffer = buffer;
    }

    /// <summary>Execute a method for every element in the message</summary>
    /// <param name="elementAction">A method that takes in the index and type tag for an element</param>
    public void ForEachElement(Action<int, TypeTag> elementAction)
    {
        for (int i = 0; i < ElementCount; i++)
            elementAction(i, _tags[i]);
    }

    /// <summary>
    /// Get a <see cref="TypeTag"/> corresponding to <paramref name="index"/>.
    /// </summary>
    /// <param name="index">Index of <see cref="TypeTag"/> you want to get</param>
    /// <returns></returns>
    public TypeTag GetTypeTag(int index)
    {
#if OSCCORE_SAFETY_CHECKS
        if (OutOfBounds(index)) return default;
#endif
        return _tags[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool OutOfBounds(int index)
    {
        if (index >= ElementCount)
        {
            Debug.Fail($"Tried to read message element index {index}, but there are only {ElementCount} elements");
            return true;
        }

        return false;
    }
}
