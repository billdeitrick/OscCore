using System.Net;
using System.Runtime.CompilerServices;

namespace Buildetech.OscCore;

public sealed unsafe partial class OscMessageValues
{
    /// <summary>
    /// Read a single 64-bit float (double) message element.
    /// Checks the element type before reading and throw <see cref="InvalidOperationException"/> if it's not interpretable as a double.
    /// </summary>
    /// <param name="index">The element index</param>
    /// <returns>The value of the element</returns>
    public double ReadFloat64Element(int index)
    {
#if OSCCORE_SAFETY_CHECKS
        if (OutOfBounds(index)) return default;
#endif
        ConvertBuffer buffer = new();
        var offset = _offsets[index];
        switch (_tags[index])
        {
            case TypeTag.Float64:
                buffer.Bits64[7] = _sharedBuffer[offset];
                buffer.Bits64[6] = _sharedBuffer[offset + 1];
                buffer.Bits64[5] = _sharedBuffer[offset + 2];
                buffer.Bits64[4] = _sharedBuffer[offset + 3];
                buffer.Bits64[3] = _sharedBuffer[offset + 4];
                buffer.Bits64[2] = _sharedBuffer[offset + 5];
                buffer.Bits64[1] = _sharedBuffer[offset + 6];
                buffer.Bits64[0] = _sharedBuffer[offset + 7];
                return buffer.@double;
            case TypeTag.Float32:
                buffer.Bits32[0] = _sharedBuffer[offset + 3];
                buffer.Bits32[1] = _sharedBuffer[offset + 2];
                buffer.Bits32[2] = _sharedBuffer[offset + 1];
                buffer.Bits32[3] = _sharedBuffer[offset];
                return buffer.@float;
            case TypeTag.Int64:
                long bigEndian = Unsafe.As<byte, long>(ref _sharedBuffer[_offsets[index]]);
                return IPAddress.NetworkToHostOrder(bigEndian);
            case TypeTag.Int32:
                return _sharedBuffer[offset] << 24 |
                       _sharedBuffer[offset + 1] << 16 |
                       _sharedBuffer[offset + 2] << 8 |
                       _sharedBuffer[offset + 3];
        }

        throw new InvalidOperationException();
    }

    /// <summary>
    /// Read a single 64-bit float (double) message element, without checking the type tag of the element.
    /// Only call this if you are really sure that the element at the given index is a valid double,
    /// as the performance difference is small.
    /// </summary>
    /// <param name="index">The element index</param>
    /// <returns>The value of the element</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ReadFloat64ElementUnchecked(int index)
    {
#if OSCCORE_SAFETY_CHECKS
        if (OutOfBounds(index)) return default;
#endif
        var offset = _offsets[index];
        ConvertBuffer buffer = new();
        buffer.Bits64[7] = _sharedBuffer[offset];
        buffer.Bits64[6] = _sharedBuffer[offset + 1];
        buffer.Bits64[5] = _sharedBuffer[offset + 2];
        buffer.Bits64[4] = _sharedBuffer[offset + 3];
        buffer.Bits64[3] = _sharedBuffer[offset + 4];
        buffer.Bits64[2] = _sharedBuffer[offset + 5];
        buffer.Bits64[1] = _sharedBuffer[offset + 6];
        buffer.Bits64[0] = _sharedBuffer[offset + 7];
        return buffer.@double;
    }
}
