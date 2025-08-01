using System.Globalization;

namespace Buildetech.OscCore.UnityObjects;

public struct Vector3
{
    public float x;
    public float y;
    public float z;

    public Vector3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public Vector3(float x, float y)
    {
        this.x = x;
        this.y = y;
        z = 0f;
    }

    public override bool Equals(object other) => other is Vector3 vector && Equals(vector);

    public bool Equals(Vector3 other) => x == other.x && y == other.y && z == other.z;

    public override int GetHashCode() => x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() >> 2);

    public override string ToString()
    {
        return ToString(null, CultureInfo.InvariantCulture.NumberFormat);
    }

    public string ToString(string? format)
    {
        return ToString(format, CultureInfo.InvariantCulture.NumberFormat);
    }

    public string ToString(string? format, IFormatProvider formatProvider)
    {
        if (string.IsNullOrEmpty(format))
            format = "F1";
        return string.Format(CultureInfo.InvariantCulture.NumberFormat, "({0}, {1}, {2})", x.ToString(format, formatProvider), y.ToString(format, formatProvider), z.ToString(format, formatProvider));
    }

    public static bool operator ==(Vector3 lhs, Vector3 rhs)
    {
        float num = lhs.x - rhs.x;
        float num2 = lhs.y - rhs.y;
        float num3 = lhs.z - rhs.z;
        float num4 = num * num + num2 * num2 + num3 * num3;
        return num4 < 9.99999944E-11f;
    }

    public static bool operator !=(Vector3 lhs, Vector3 rhs)
    {
        return !(lhs == rhs);
    }

}
