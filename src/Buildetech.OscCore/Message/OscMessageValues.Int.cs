using System.Runtime.CompilerServices;

namespace Buildetech.OscCore;

public sealed unsafe partial class OscMessageValues
{
    /// <summary>
    /// Read a single 32-bit integer message element.
    /// Checks the element type before reading and throw <see cref="InvalidOperationException"/> if it's not interpretable as a integer.
    /// </summary>
    /// <param name="index">The element index</param>
    /// <returns>The value of the element</returns>
    public int ReadIntElement(int index)
    {
#if OSCCORE_SAFETY_CHECKS
        if (OutOfBounds(index)) return default;
#endif
        var offset = _offsets[index];
        switch (_tags[index])
        {
            case TypeTag.Int32:
                return _sharedBuffer[offset] << 24 |
                       _sharedBuffer[offset + 1] << 16 |
                       _sharedBuffer[offset + 2] << 8 |
                       _sharedBuffer[offset + 3];
            case TypeTag.Float32:
                ConvertBuffer buffer = new();
                buffer.Bits32[0] = _sharedBuffer[offset + 3];
                buffer.Bits32[1] = _sharedBuffer[offset + 2];
                buffer.Bits32[2] = _sharedBuffer[offset + 1];
                buffer.Bits32[3] = _sharedBuffer[offset];
                return (int)buffer.@float;
            default:
                throw new InvalidOperationException();
        }
    }

    /// <summary>
    /// Read a single 32-bit int message element, without checking the type tag of the element.
    /// Only call this if you are really sure that the element at the given index is a valid integer,
    /// as the performance difference is small.
    /// </summary>
    /// <param name="index">The element index</param>
    /// <returns>The value of the element</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadIntElementUnchecked(int index)
    {
#if OSCCORE_SAFETY_CHECKS
        if (OutOfBounds(index)) return default;
#endif
        var offset = _offsets[index];
        return _sharedBuffer[offset] << 24 |
               _sharedBuffer[offset + 1] << 16 |
               _sharedBuffer[offset + 2] << 8 |
               _sharedBuffer[offset + 3];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal uint ReadUIntIndex(int index)
    {
        ConvertBuffer buffer = new();
        buffer.Bits32[0] = _sharedBuffer[index + 3];
        buffer.Bits32[1] = _sharedBuffer[index + 2];
        buffer.Bits32[2] = _sharedBuffer[index + 1];
        buffer.Bits32[3] = _sharedBuffer[index];
        return buffer.@uint;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int ReadIntIndex(int index)
    {
        return _sharedBuffer[index] << 24 |
               _sharedBuffer[index + 1] << 16 |
               _sharedBuffer[index + 2] << 8 |
               _sharedBuffer[index + 3];
    }
}
