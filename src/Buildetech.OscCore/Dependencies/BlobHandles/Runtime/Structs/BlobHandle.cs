using System;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BlobHandles;

/// <summary>
/// Wraps an arbitrary chunk of bytes in memory, so it can be used as a hash key
/// and compared against other instances of the same set of bytes 
/// </summary>
public readonly unsafe struct BlobHandle : IEquatable<BlobHandle>
{
    /// <summary>A pointer to the start of the blob</summary>
    public byte* Pointer => (byte*)Unsafe.AsPointer(ref Reference);

    /// <summary>A reference to the start of the blob</summary>
    public ref byte Reference
    {
        get
        {
            if (_pointer != null)
            {
                return ref *_pointer;
            }
            return ref _bytes[_offset];
        }
    }

    private readonly byte* _pointer;
    private readonly byte[] _bytes;
    private readonly int _offset;

    /// <summary>The number of bytes in the blob</summary>
    public int Length { get; }

    private ref byte this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Unsafe.Add(ref Reference, (nint)(uint)index /* force zero-extension */);
        }
    }

    public BlobHandle(byte* pointer, int length)
    {
        _pointer = pointer;
        Length = length;
    }

    public BlobHandle(IntPtr pointer, int length)
    {
        _pointer = (byte*)pointer;
        Length = length;
    }

    /// <summary>
    /// Get a blob handle for a byte array. The byte array should have its address pinned to work safely!
    /// </summary>
    /// <param name="bytes">The bytes to get a handle to</param>
    public BlobHandle(byte[] bytes)
    {
        _bytes = bytes;
        Length = bytes.Length;
    }

    /// <summary>
    /// Get a blob handle for part of a byte array. The byte array should have its address pinned to work safely!
    /// </summary>
    /// <param name="bytes">The bytes to get a handle to</param>
    /// <param name="length">The number of bytes to include. Not bounds checked</param>
    public BlobHandle(byte[] bytes, int length)
    {
        _bytes = bytes;
        Length = length;
    }

    /// <summary>
    /// Get a blob handle for a slice of a byte array. The byte array should have its address pinned to work safely!
    /// </summary>
    /// <param name="bytes">The bytes to get a handle to</param>
    /// <param name="length">The number of bytes to include. Not bounds checked</param>
    /// <param name="offset">The byte array index to start the blob at</param>
    public BlobHandle(byte[] bytes, int length, int offset)
    {
        _bytes = bytes;
        Length = length;
        _offset = offset;
    }

    public override string ToString()
    {
        return $"{Length} bytes @ {new IntPtr(Pointer)}";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
    {
        unchecked
        {
            return Length * 397 ^ this[Length - 1];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(BlobHandle other)
    {
        return Length == other.Length &&
               MemoryCompare(ref Reference, ref other.Reference, Length) == 0;
    }

    public override bool Equals(object obj)
    {
        return obj is BlobHandle other && Equals(other);
    }

    public static bool operator ==(BlobHandle left, BlobHandle right)
    {
        return left.Length == right.Length &&
               MemoryCompare(ref left.Reference, ref right.Reference, left.Length) == 0;
    }

    public static bool operator !=(BlobHandle left, BlobHandle right)
    {
        return left.Length != right.Length ||
               MemoryCompare(ref left.Reference, ref right.Reference, left.Length) != 0;
    }

    private static int MemoryCompare(ref byte ptr1, ref byte ptr2, int count)
    {
        var p1 = (byte*)Unsafe.AsPointer(ref ptr1);
        var p2 = (byte*)Unsafe.AsPointer(ref ptr2);
        for (int i = 0; i < (uint)count; i++)
        {
            if (p1[i] != p2[i])
            {
                return 1;
            }
        }
        return 0;
    }
}
