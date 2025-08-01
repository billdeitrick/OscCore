using System;
using System.Net;
using System.Text;
using BlobHandles;
using Buildetech.OscCore;
using Buildetech.OscCore.UnityObjects;
using MiniNtp;
using NUnit.Framework;

namespace Buildetech.OscCore.Tests;

public class OscWriterTests
{
    private readonly OscWriter _writer = new();
    private int _writerLengthBefore;

    [SetUp]
    public void BeforeEach()
    {
        _writer.Reset();
        _writerLengthBefore = _writer.Length;
    }

    [TestCase(130)]
    [TestCase(144)]
    public void WriteInt32(int value)
    {
        _writer.Write(value);

        Assert.AreEqual(_writerLengthBefore + 4, _writer.Length);
        // this tests both that it wrote to the right place in the buffer as well as that the value is right
        var convertedBack = BitConverter.ToInt32(_writer.Buffer, _writerLengthBefore).ReverseBytes();

        Assert.AreEqual(value, convertedBack);
    }

    [TestCase(0.00001f)]
    [TestCase(0.867924529f)]
    [TestCase(144f)]
    public void WriteFloat32(float value)
    {
        _writer.Write(value);

        Assert.AreEqual(_writerLengthBefore + 4, _writer.Length);
        var convertedBack = BitConverter.ToSingle(_writer.Buffer, _writerLengthBefore).ReverseBytes();
        Assert.AreEqual(value, convertedBack);
    }

    [TestCase("/composition/tempo")]
    [TestCase("/layers/1/opacity")]
    [TestCase("/composition/layers/2/video/blend")]
    public void WriteString(string value)
    {
        _writer.Write(value);

        var asciiByteCount = Encoding.ASCII.GetByteCount(value);
        // strings align to 4 byte chunks like all other osc data types
        var alignedByteCount = (asciiByteCount + 3) & ~3;
        Assert.AreEqual(_writerLengthBefore + alignedByteCount, _writer.Length);

        var convertedBack = Encoding.ASCII.GetString(_writer.Buffer, _writerLengthBefore, asciiByteCount);
        Assert.AreEqual(value, convertedBack);
    }

    [TestCase("/composition/tempo")]
    [TestCase("/layers/1/opacity")]
    [TestCase("/composition/layers/2/video/blend")]
    [TestCase("/composition/layers/2/video/mode")]
    public void WriteBlobString(string value)
    {
        BlobString.Encoding = Encoding.ASCII;
        var blobStr = new BlobString(value);
        _writer.Write(blobStr);

        blobStr.Dispose();
        var asciiByteCount = Encoding.ASCII.GetByteCount(value);
        var alignedByteCount = (asciiByteCount + 3) & ~3;
        var expected = alignedByteCount == asciiByteCount ? asciiByteCount + 4 : alignedByteCount;
        Assert.AreEqual(expected, _writer.Length);

        var convertedBack = Encoding.ASCII.GetString(_writer.Buffer, _writerLengthBefore, asciiByteCount);
        Assert.AreEqual(value, convertedBack);
    }

    [TestCase(43)]
    [TestCase(62)]
    [TestCase(144)]
    public void WriteBlob(int size)
    {
        var bytes = RandomBytes(size);
        _writer.Write(bytes, size);

        var alignedByteCount = (size + 3) & ~3;
        var blobContentIndex = _writerLengthBefore + 4;
        var blobWriteEndIndex = blobContentIndex + size;
        // was the blob size written properly ?
        var writtenSize = BitConverter.ToInt32(_writer.Buffer, _writerLengthBefore).ReverseBytes();
        Assert.AreEqual(size, writtenSize);
        Assert.AreEqual(blobContentIndex + alignedByteCount, _writer.Length);

        // were the blob contents written the same as the source ?
        for (int i = blobContentIndex; i < blobWriteEndIndex; i++)
        {
            Assert.AreEqual(bytes[i - blobContentIndex], _writer.Buffer[i]);
        }

        // did we write the necessary trailing bytes to align to the next 4 byte interval ?
        var zeroEndIndex = blobContentIndex + alignedByteCount;
        for (int i = blobWriteEndIndex; i < zeroEndIndex; i++)
        {
            Assert.Zero(_writer.Buffer[i]);
        }
    }

    [TestCase(50000000)]
    [TestCase(144 * 100000)]
    public void WriteInt64(long value)
    {
        _writer.Write(value);

        Assert.AreEqual(_writerLengthBefore + 8, _writer.Length);
        var bigEndian = BitConverter.ToInt64(_writer.Buffer, _writerLengthBefore);
        var convertedBack = IPAddress.NetworkToHostOrder(bigEndian);
        Assert.AreEqual(value, convertedBack);
    }

    [TestCase(0.00000001d)]
    [TestCase(0.8279245299754d)]
    [TestCase(144.1d * 1000d)]
    public void WriteFloat64(double value)
    {
        _writer.Write(value);

        Assert.AreEqual(_writerLengthBefore + 8, _writer.Length);
        var convertedBack = BitConverter.ToDouble(_writer.Buffer, _writerLengthBefore).ReverseBytes();
        Assert.AreEqual(value, convertedBack);
    }

    [TestCase(50, 100, 0, 255)]
    [TestCase(120, 80, 255, 100)]
    [TestCase(255, 150, 50, 255)]
    public void WriteColor32(byte r, byte g, byte b, byte a)
    {
        var value = new Color32(r, g, b, a);
        _writer.Write(value);

        Assert.AreEqual(_writerLengthBefore + 4, _writer.Length);
        var bR = _writer.Buffer[_writerLengthBefore + 3];
        var bG = _writer.Buffer[_writerLengthBefore + 2];
        var bB = _writer.Buffer[_writerLengthBefore + 1];
        var bA = _writer.Buffer[_writerLengthBefore];
        var convertedBack = new Color32(bR, bG, bB, bA);
        Assert.AreEqual(value, convertedBack);
    }

    [Test]
    public void WriteMidi()
    {
        var value = new MidiMessage(1, 4, 16, 80);
        _writer.Write(value);

        Assert.AreEqual(_writerLengthBefore + 4, _writer.Length);
        var convertedBack = new MidiMessage(_writer.Buffer, _writerLengthBefore);
        Assert.True(value == convertedBack);
    }

    [TestCase('S')]
    [TestCase('m')]
    [TestCase('C')]
    public void WriteAsciiChar(char chr)
    {
        _writer.Write(chr);

        Assert.AreEqual(_writerLengthBefore + 4, _writer.Length);
        var convertedBack = (char)_writer.Buffer[_writerLengthBefore + 3];
        Assert.True(chr == convertedBack);
    }

    [Test]
    public void WriteTimestamp()
    {
        var stamp = new NtpTimestamp(DateTime.Now);
        _writer.Write(stamp);

        Assert.AreEqual(_writerLengthBefore + 8, _writer.Length);
        var convertedBack = NtpTimestamp.FromBigEndianBytes(_writer.Buffer, _writerLengthBefore);
        Assert.True(stamp == convertedBack);
    }

    [Test]
    public void WriteVector2()
    {
        var data = new Vector2(2.5f, 1.01f);
        _writer.Write(data);

        Assert.AreEqual(_writerLengthBefore + 8, _writer.Length);
        var readX = BitConverter.ToSingle(_writer.Buffer, _writerLengthBefore).ReverseBytes();
        var readY = BitConverter.ToSingle(_writer.Buffer, _writerLengthBefore + 4).ReverseBytes();
        Assert.True(data == new Vector2(readX, readY));
    }

    [Test]
    public void WriteVector3()
    {
        var data = new Vector3(0.15f, -4.2f, 1f);
        _writer.Write(data);

        Assert.AreEqual(_writerLengthBefore + 12, _writer.Length);
        var readX = BitConverter.ToSingle(_writer.Buffer, _writerLengthBefore).ReverseBytes();
        var readY = BitConverter.ToSingle(_writer.Buffer, _writerLengthBefore + 4).ReverseBytes();
        var readZ = BitConverter.ToSingle(_writer.Buffer, _writerLengthBefore + 8).ReverseBytes();
        Assert.True(data == new Vector3(readX, readY, readZ));
    }

    private static byte[] RandomBytes(int count)
    {
        var bytes = new byte[count];
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = (byte)TestUtil.SharedRandom.Next(0, 255);

        return bytes;
    }
}
