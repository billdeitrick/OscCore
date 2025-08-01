using System.Diagnostics;
using System.Linq;
using Buildetech.OscCore.UnityObjects;
using NUnit.Framework;

namespace Buildetech.OscCore.Tests;

[TestOf(typeof(OscMessageValues))]
public class MessageReadTests
{
    private const int Count = 4096;
    private static readonly Stopwatch _stopwatch = new();
    private readonly ConvertBuffer[] _buffers = new ConvertBuffer[Count];
    private readonly byte[] _midiSourceBytes = TestUtil.RandomMidiBytes(Count * 4);
    private readonly byte[] _timeSourceBytes = TestUtil.RandomTimestampBytes(Count * 4);

    [OneTimeSetUp]
    public unsafe void BeforeAll()
    {
        for (int i = 0; i < _buffers.Length; i++)
        {
            fixed (byte* bytes = _buffers[i].Bytes)
            {
                for (int j = 0; j < sizeof(ConvertBuffer); j++)
                {
                    bytes[j] = unchecked((byte)TestUtil.SharedRandom.Next());
                }
            }
        }
    }

    [SetUp]
    public void BeforeEach()
    {

    }

    private static OscMessageValues FromBytes(byte[] bytes, int count, TypeTag tag, int byteSize = 4)
    {
        var values = new OscMessageValues(bytes, count);
        for (int i = 0; i < count; i++)
        {
            values._offsets[i] = i * byteSize;
            values._tags[i] = tag;
        }

        values.ElementCount = count;
        return values;
    }

    private static OscMessageValues FromBytes(ConvertBuffer[] buffers, int count, TypeTag tag, int byteSize)
    {
        var values = new OscMessageValues(buffers.SelectMany(s => s.GetReversedBytes(byteSize)).ToArray(), count);
        for (int i = 0; i < count; i++)
        {
            values._offsets[i] = i * byteSize;
            values._tags[i] = tag;
        }

        values.ElementCount = count;
        return values;
    }

    [Test]
    public void ReadFloatElement_Checked()
    {
        const int count = 2048;
        var values = FromBytes(_buffers, count, TypeTag.Float32, sizeof(float));

        for (int i = 0; i < count; i++)
        {
            Assert.AreEqual(_buffers[i].@float, values.ReadFloatElement(i));
            Assert.AreEqual((double)_buffers[i].@float, values.ReadFloat64Element(i));
            Assert.AreEqual((int)_buffers[i].@float, values.ReadIntElement(i));
            Assert.AreEqual((long)_buffers[i].@float, values.ReadInt64Element(i));
            Assert.AreEqual(_buffers[i].@float.ToString(), values.ReadStringElement(i));
        }
    }

    [Test]
    public void ReadFloatElement_Unchecked()
    {
        const int count = 2048;
        var values = FromBytes(_buffers, count, TypeTag.Float32, sizeof(float));

        for (int i = 0; i < count; i++)
        {
            Assert.AreEqual(_buffers[i].@float, values.ReadFloatElementUnchecked(i));
        }
    }

    [Test]
    public void ReadFloat64Element_Checked()
    {
        const int count = 2048;
        var values = FromBytes(_buffers, count, TypeTag.Float64, sizeof(double));

        for (int i = 0; i < count; i++)
        {
            Assert.AreEqual(_buffers[i].@double, values.ReadFloat64Element(i));
            Assert.AreEqual((long)_buffers[i].@double, values.ReadInt64Element(i));
            Assert.AreEqual(_buffers[i].@double.ToString(), values.ReadStringElement(i));
        }
    }

    [Test]
    public void ReadFloat64Element_Unchecked()
    {
        const int count = 2048;
        var values = FromBytes(_buffers, count, TypeTag.Float64, sizeof(double));

        for (int i = 0; i < count; i++)
        {
            Assert.AreEqual(_buffers[i].@double, values.ReadFloat64ElementUnchecked(i));
        }
    }

    [Test]
    public void ReadInt32Element_Checked()
    {
        const int count = 2048;
        var values = FromBytes(_buffers, count, TypeTag.Int32, sizeof(int));

        for (int i = 0; i < count; i++)
        {
            Assert.AreEqual(_buffers[i].@int > 0, values.ReadBooleanElement(i));
            Assert.AreEqual(_buffers[i].@int, values.ReadIntElement(i));
            Assert.AreEqual((long)_buffers[i].@int, values.ReadInt64Element(i));
            Assert.AreEqual((float)_buffers[i].@int, values.ReadFloatElement(i));
            Assert.AreEqual((double)_buffers[i].@int, values.ReadFloat64Element(i));
            Assert.AreEqual(_buffers[i].@int.ToString(), values.ReadStringElement(i));
        }
    }

    [Test]
    public void ReadInt32Element_Unchecked()
    {
        const int count = 2048;
        var values = FromBytes(_buffers, count, TypeTag.Int32, sizeof(int));

        for (int i = 0; i < count; i++)
        {
            Assert.AreEqual(_buffers[i].@int, values.ReadIntElementUnchecked(i));
        }
    }

    [Test]
    public void ReadInt64Element_Checked()
    {
        const int count = 2048;
        var values = FromBytes(_buffers, count, TypeTag.Int64, sizeof(double));

        for (int i = 0; i < count; i++)
        {
            Assert.AreEqual(_buffers[i].@long, values.ReadInt64Element(i));
            Assert.AreEqual((double)_buffers[i].@long, values.ReadFloat64Element(i));
            Assert.AreEqual(_buffers[i].@long.ToString(), values.ReadStringElement(i));
        }
    }

    [Test]
    public void ReadInt64Element_Unchecked()
    {
        const int count = 2048;
        var values = FromBytes(_buffers, count, TypeTag.Int64, sizeof(double));

        for (int i = 0; i < count; i++)
        {
            Assert.AreEqual(_buffers[i].@long, values.ReadInt64ElementUnchecked(i));
        }
    }


    [Test]
    public void ReadBooleanElement()
    {
        const int count = 2048;
        var values = new OscMessageValues(new byte[0], count);
        for (int i = 0; i < count; i++)
        {
            values._offsets[i] = 0;
            values._tags[i] = _buffers[i].@bool ? TypeTag.True : TypeTag.False;
        }

        for (int i = 0; i < count; i++)
        {
            Assert.AreEqual(_buffers[i].@bool, values.ReadBooleanElement(i));
        }
    }

    [Test]
    public void ReadNilElement()
    {
        const int count = 2048;
        var values = new OscMessageValues(new byte[0], count);
        for (int i = 0; i < count; i++)
        {
            values._offsets[i] = 0;
            values._tags[i] = TypeTag.Nil;
        }

        for (int i = 0; i < count; i++)
        {
            Assert.AreEqual(true, values.ReadNilOrInfinitumElement(i));
            Assert.AreEqual("Nil", values.ReadStringElement(i));
        }
    }

    [Test]
    public void ReadInfinitumElement()
    {
        const int count = 2048;
        var values = new OscMessageValues(new byte[0], count);
        for (int i = 0; i < count; i++)
        {
            values._offsets[i] = 0;
            values._tags[i] = TypeTag.Infinitum;
        }

        for (int i = 0; i < count; i++)
        {
            Assert.AreEqual(true, values.ReadNilOrInfinitumElement(i));
            Assert.AreEqual("Infinitum", values.ReadStringElement(i));
        }
    }

    [Test]
    public unsafe void ReadColor32Element_Checked()
    {
        const int count = 2048;
        var values = FromBytes(_buffers, count, TypeTag.Color32, sizeof(Color32));

        for (int i = 0; i < count; i++)
        {
            Assert.AreEqual(_buffers[i].Color32, values.ReadColor32Element(i));
            Assert.AreEqual(_buffers[i].Color32.ToString(), values.ReadStringElement(i));
        }
    }

    [Test]
    public unsafe void ReadColor32Element_Unchecked()
    {
        const int count = 2048;
        var values = FromBytes(_buffers, count, TypeTag.Color32, sizeof(Color32));

        for (int i = 0; i < count; i++)
        {
            Assert.AreEqual(_buffers[i].Color32, values.ReadColor32ElementUnchecked(i));
        }
    }
}
