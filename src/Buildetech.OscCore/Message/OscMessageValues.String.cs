using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;

namespace Buildetech.OscCore;

public sealed unsafe partial class OscMessageValues
{
    /// <summary>
    /// Read a single string message element.
    /// Checks the element type before reading and throw <see cref="InvalidOperationException"/> if it's not interpretable as a string.
    /// </summary>
    /// <param name="index">The element index</param>
    /// <returns>The value of the element</returns>
    public string ReadStringElement(int index)
    {
#if OSCCORE_SAFETY_CHECKS
        if (OutOfBounds(index)) return string.Empty;
#endif
        ConvertBuffer buffer = new();
        var offset = _offsets[index];
        switch (_tags[index])
        {
            case TypeTag.AltTypeString:
            case TypeTag.String:
                var length = 0;
                while (_sharedBuffer[offset + length] != byte.MinValue) length++;
                return Encoding.ASCII.GetString(_sharedBuffer, offset, length);

            case TypeTag.Float64:
                buffer.Bits64[7] = _sharedBuffer[offset];
                buffer.Bits64[6] = _sharedBuffer[offset + 1];
                buffer.Bits64[5] = _sharedBuffer[offset + 2];
                buffer.Bits64[4] = _sharedBuffer[offset + 3];
                buffer.Bits64[3] = _sharedBuffer[offset + 4];
                buffer.Bits64[2] = _sharedBuffer[offset + 5];
                buffer.Bits64[1] = _sharedBuffer[offset + 6];
                buffer.Bits64[0] = _sharedBuffer[offset + 7];
                return buffer.@double.ToString(CultureInfo.CurrentCulture);

            case TypeTag.Float32:
                buffer.Bits32[0] = _sharedBuffer[offset + 3];
                buffer.Bits32[1] = _sharedBuffer[offset + 2];
                buffer.Bits32[2] = _sharedBuffer[offset + 1];
                buffer.Bits32[3] = _sharedBuffer[offset];
                return buffer.@float.ToString(CultureInfo.CurrentCulture);

            case TypeTag.Int64:
                var i64 = IPAddress.NetworkToHostOrder(Unsafe.As<byte, long>(ref _sharedBuffer[offset]));
                return i64.ToString(CultureInfo.CurrentCulture);

            case TypeTag.Int32:
                int i32 = _sharedBuffer[offset] << 24 |
                          _sharedBuffer[offset + 1] << 16 |
                          _sharedBuffer[offset + 2] << 8 |
                          _sharedBuffer[offset + 3];
                return i32.ToString(CultureInfo.CurrentCulture);

            case TypeTag.False:
                return "False";

            case TypeTag.True:
                return "True";

            case TypeTag.Nil:
                return "Nil";

            case TypeTag.Infinitum:
                return "Infinitum";

            case TypeTag.Color32:
                buffer.Bits32[0] = _sharedBuffer[offset + 3];
                buffer.Bits32[1] = _sharedBuffer[offset + 2];
                buffer.Bits32[2] = _sharedBuffer[offset + 1];
                buffer.Bits32[3] = _sharedBuffer[offset];
                return buffer.Color32.ToString();

            case TypeTag.MIDI:
                return Unsafe.As<byte, MidiMessage>(ref _sharedBuffer[offset]).ToString();
            case TypeTag.AsciiChar32:
                // ascii chars are encoded in the last byte of the 4-byte block
                return ((char)_sharedBuffer[offset + 3]).ToString();
            default:
                throw new InvalidOperationException();
        }
    }

    /// <summary>
    /// Read a single string message element as bytes.
    /// </summary>
    /// <param name="index">The element index</param>
    /// <param name="copyTo">The byte array to copy the string's bytes to</param>
    /// <returns>The byte length of the string</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadStringElementBytes(int index, byte[] copyTo)
    {
#if OSCCORE_SAFETY_CHECKS
        if (OutOfBounds(index)) return default;
#endif
        switch (_tags[index])
        {
            case TypeTag.AltTypeString:
            case TypeTag.String:
                int i;
                var offset = _offsets[index];
                for (i = offset; i < _sharedBuffer.Length; i++)
                {
                    byte b = _sharedBuffer[i];
                    if (b == byte.MinValue) break;
                    copyTo[i - offset] = b;
                }
                return i - offset;
            default:
                throw new InvalidOperationException();
        }
    }

    /// <summary>
    /// Read a single string message element as bytes.
    /// </summary>
    /// <param name="index">The element index</param>
    /// <param name="copyTo">The byte array to copy the string's bytes to</param>
    /// <param name="copyOffset">The index in the copyTo array to start copying at</param>
    /// <returns>The byte length of the string</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadStringElementBytes(int index, byte[] copyTo, int copyOffset)
    {
#if OSCCORE_SAFETY_CHECKS
        if (OutOfBounds(index)) return default;
#endif
        switch (_tags[index])
        {
            case TypeTag.AltTypeString:
            case TypeTag.String:
                int i;
                var offset = _offsets[index];
                // when this is subtracted from i, it's the same as i - offset + copyOffset
                var copyStartOffset = offset - copyOffset;
                for (i = offset; i < _sharedBuffer.Length; i++)
                {
                    byte b = _sharedBuffer[i];
                    if (b == byte.MinValue) break;
                    copyTo[i - copyStartOffset] = b;
                }

                return i - offset;
            default:
                throw new InvalidOperationException();
        }
    }
}
