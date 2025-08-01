using System;
using System.Runtime.CompilerServices;
using Buildetech.OscCore.UnityObjects;

namespace Buildetech.OscCore;

public sealed unsafe partial class OscMessageValues
{
    /// <summary>
    /// Read a single 32-bit RGBA color message element.
    /// Checks the element type before reading and throw <see cref="InvalidOperationException"/> if it's not interpretable as a color.
    /// </summary>
    /// <param name="index">The element index</param>
    /// <returns>The value of the element</returns>
    public Color32 ReadColor32Element(int index)
    {
#if OSCCORE_SAFETY_CHECKS
        if (OutOfBounds(index)) return default;
#endif
        var offset = _offsets[index];
        switch (_tags[index])
        {
            case TypeTag.Color32:
                ConvertBuffer buffer = new();
                buffer.Bits32[0] = _sharedBuffer[offset + 3];
                buffer.Bits32[1] = _sharedBuffer[offset + 2];
                buffer.Bits32[2] = _sharedBuffer[offset + 1];
                buffer.Bits32[3] = _sharedBuffer[offset];
                return buffer.Color32;
            default:
                throw new InvalidOperationException();
        }
    }

    /// <summary>
    /// Read a single 32-bit RGBA color message element, without checking the type tag of the element.
    /// Only call this if you are really sure that the element at the given index is a valid Color32,
    /// as the performance difference is small.
    /// </summary>
    /// <param name="index">The element index</param>
    /// <returns>The value of the element</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Color32 ReadColor32ElementUnchecked(int index)
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
        return buffer.Color32;
    }
}
