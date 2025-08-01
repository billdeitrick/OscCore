using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace Buildetech.OscCore.UnityObjects;

[StructLayout(LayoutKind.Explicit)]

public struct Color32
{
    [FieldOffset(0)]
#pragma warning disable IDE1006 // Naming style
    private readonly int rgba;
#pragma warning restore IDE1006 // Naming style

    [FieldOffset(0)]
    public byte r;
    [FieldOffset(1)]
    public byte g;
    [FieldOffset(2)]
    public byte b;
    [FieldOffset(3)]
    public byte a;

    public byte this[int index]
    {
        get
        {
            return index switch
            {
                0 => r,
                1 => g,
                2 => b,
                3 => a,
                _ => throw new IndexOutOfRangeException("Invalid Color32 index(" + index + ")!"),
            };
        }
        set
        {
            switch (index)
            {
                case 0:
                    r = value;
                    break;
                case 1:
                    g = value;
                    break;
                case 2:
                    b = value;
                    break;
                case 3:
                    a = value;
                    break;
                default:
                    throw new IndexOutOfRangeException("Invalid Color32 index(" + index + ")!");
            }
        }
    }

    public Color32(byte r, byte g, byte b, byte a)
    {
        rgba = 0;
        (this.r, this.g, this.b, this.a) = (r, g, b, a);
    }

    /// <summary>
    /// Returns a nicely formatted string of this color.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return string.Format(CultureInfo.InvariantCulture.NumberFormat, "RGBA({0}, {1}, {2}, {3})", r, g, b, a);
    }

    /// <summary>
    /// Returns a nicely formatted string of this color.
    /// </summary>
    /// <param name="format"></param>
    /// <returns></returns>
    public string ToString(string format)
    {
        return string.Format(CultureInfo.InvariantCulture.NumberFormat, "RGBA({0}, {1}, {2}, {3})", r.ToString(format), g.ToString(format), b.ToString(format), a.ToString(format));
    }
}
