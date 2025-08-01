using System.Runtime.CompilerServices;

namespace Buildetech.OscCore;

public sealed unsafe partial class OscMessageValues
{
    /// <summary>
    /// Read a single 32-bit float message element.
    /// Checks the element type before reading and throw <see cref="InvalidOperationException"/> if it's not interpretable as a float.
    /// </summary>
    /// <param name="index">The element index</param>
    /// <returns>The value of the element</returns>
    public float ReadFloatElement(int index)
    {
#if OSCCORE_SAFETY_CHECKS
        if (OutOfBounds(index)) return default;
#endif
        var offset = _offsets[index];
        switch (_tags[index])
        {
            case TypeTag.Float32:
                ConvertBuffer buffer = new();
                buffer.Bits32[0] = _sharedBuffer[offset + 3];
                buffer.Bits32[1] = _sharedBuffer[offset + 2];
                buffer.Bits32[2] = _sharedBuffer[offset + 1];
                buffer.Bits32[3] = _sharedBuffer[offset];
                return buffer.@float;
            case TypeTag.Int32:
                return _sharedBuffer[offset] << 24 |
                       _sharedBuffer[offset + 1] << 16 |
                       _sharedBuffer[offset + 2] << 8 |
                       _sharedBuffer[offset + 3];
            default:
                throw new InvalidOperationException();
        }
    }

    /// <summary>
    /// Read a single 32-bit float message element, without checking the type tag of the element.
    /// Only call this if you are really sure that the element at the given index is a valid float,
    /// as the performance difference is small.
    /// </summary>
    /// <param name="index">The element index</param>
    /// <returns>The value of the element</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadFloatElementUnchecked(int index)
    {
#if OSCCORE_SAFETY_CHECKS
        if (OutOfBounds(index)) return default;
#endif
        var offset = _offsets[index];
        ConvertBuffer buffer = new();
        buffer.Bits32[0] = _sharedBuffer[offset + 3];
        buffer.Bits32[1] = _sharedBuffer[offset + 2];
        buffer.Bits32[2] = _sharedBuffer[offset + 1];
        buffer.Bits32[3] = _sharedBuffer[offset];
        return buffer.@float;
    }
}
