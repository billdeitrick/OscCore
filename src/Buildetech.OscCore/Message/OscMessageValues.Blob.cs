using System;
using System.Runtime.CompilerServices;

namespace Buildetech.OscCore;

public sealed unsafe partial class OscMessageValues
{
    private const int ResizeByteHeadroom = 1024;

    /// <summary>
    /// Read a blob element.
    /// Checks the element type before reading, and throw <see cref="InvalidOperationException"/> if the element is not a blob.
    /// </summary>
    /// <param name="index">The element index</param>
    /// <returns>The array copied blob contents into</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[] ReadBlobElement(int index)
    {
#if OSCCORE_SAFETY_CHECKS
        if (OutOfBounds(index)) return default;
#endif
        switch (_tags[index])
        {
            case TypeTag.Blob:
                var offset = _offsets[index];
                var size = ReadIntIndex(offset);
                var dataStart = offset + 4;    // skip the size int
                var copyTo = new byte[size];
                Buffer.BlockCopy(_sharedBuffer, dataStart, copyTo, 0, size);
                return copyTo;
            default:
                throw new InvalidOperationException();
        }
    }

    /// <summary>
    /// Read a blob element.
    /// Checks the element type before reading, and throw <see cref="InvalidOperationException"/> if the element is not a blob.
    /// </summary>
    /// <param name="index">The element index</param>
    /// <param name="copyTo">
    /// The array to copy blob contents into.
    /// Will be resized if it lacks sufficient capacity
    /// </param>
    /// <param name="copyOffset">The index in the copyTo array to start copying at</param>
    /// <returns>The size of the blob</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadBlobElement(int index, ref byte[] copyTo, int copyOffset = 0)
    {
#if OSCCORE_SAFETY_CHECKS
        if (OutOfBounds(index)) return default;
#endif
        switch (_tags[index])
        {
            case TypeTag.Blob:
                var offset = _offsets[index];
                var size = ReadIntIndex(offset);
                var dataStart = offset + 4;    // skip the size int
                if (copyTo.Length - copyOffset <= size)
                    Array.Resize(ref copyTo, size + copyOffset + ResizeByteHeadroom);

                Buffer.BlockCopy(_sharedBuffer, dataStart, copyTo, copyOffset, size);
                return size;
            default:
                throw new InvalidOperationException();
        }
    }

}
