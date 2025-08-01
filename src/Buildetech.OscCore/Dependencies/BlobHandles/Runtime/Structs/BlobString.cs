using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace BlobHandles;

/// <summary>
/// Represents a string as a fixed blob of bytes
/// </summary>
public readonly struct BlobString : IDisposable, IEquatable<BlobString>
{
    /// <summary>
    /// The encoding used to convert to and from strings.
    /// WARNING - Changing this after strings have been encoded will probably lead to errors!
    /// </summary>
    public static Encoding Encoding { get; set; } = Encoding.ASCII;

    public BlobHandle Handle { get; }

    public int Length => Handle.Length;

    public unsafe BlobString(string source)
    {
        // write encoded string bytes directly to unmanaged memory
        Handle = new BlobHandle(Encoding.GetBytes(source));
    }

    public unsafe BlobString(byte* sourcePtr, int length)
    {
        Handle = new BlobHandle(sourcePtr, length);
    }

    public override unsafe string ToString()
    {
        return Encoding.GetString((byte*)Unsafe.AsPointer(ref Handle.Reference), Handle.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
    {
        return Handle.GetHashCode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(BlobString other)
    {
        return Handle.Equals(other.Handle);
    }

    public override bool Equals(object obj)
    {
        return obj is BlobString other && Handle.Equals(other.Handle);
    }

    public static bool operator ==(BlobString l, BlobString r)
    {
        return l.Handle == r.Handle;
    }

    public static bool operator !=(BlobString l, BlobString r)
    {
        return l.Handle != r.Handle;
    }

    public void Dispose()
    {
    }
}
