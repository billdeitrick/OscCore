using System.Diagnostics;
using System.Runtime.InteropServices;
using Buildetech.OscCore.UnityObjects;

namespace Buildetech.OscCore.Tests;

[StructLayout(LayoutKind.Explicit)]
public unsafe struct ConvertBuffer
{
    [FieldOffset(0)]
    public fixed byte Bytes[8];
    [FieldOffset(0)]
    public readonly int @int;
    [FieldOffset(0)]
    public readonly long @long;
    [FieldOffset(0)]
    public readonly float @float;
    [FieldOffset(0)]
    public readonly double @double;
    [FieldOffset(0)]
    public readonly Color32 Color32;

    public bool @bool => @int >= 0;

    public byte[] GetReversedBytes(int size)
    {
        Debug.Assert(size > 0 && size <= sizeof(ConvertBuffer));
        fixed (byte* p = Bytes)
        {
            return TestUtil.ReversedCopy(p, size);
        }
    }
}
