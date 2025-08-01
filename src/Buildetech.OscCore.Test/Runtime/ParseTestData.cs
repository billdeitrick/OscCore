using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MiniNtp;
using NUnit.Framework;

namespace Buildetech.OscCore.Tests;

internal static class MessageTestData
{
    public static IEnumerable Basic
    {
        get
        {
            var msg1 = SingleFloatMessage("/composition/layers/1/video/mixer/opacity", 0.69f);
            yield return new TestCaseData(msg1, msg1.Length);

            var msg2 = SingleFloatMessage("/composition/layers/1/clips/1/video/source/solidcolor/color/blue", 0.4f);
            yield return new TestCaseData(msg2, msg2.Length);
        }
    }

    private static byte[] SingleFloatMessage(string address, float value)
    {
        var addressBytes = Encoding.ASCII.GetBytes(address);
        var alignedByteCount = (addressBytes.Length + 3) & ~3;

        var bytes = new byte[alignedByteCount + 8];
        for (var i = 0; i < addressBytes.Length; i++)
            bytes[i] = addressBytes[i];

        bytes[alignedByteCount] = Constant.Comma;
        bytes[alignedByteCount + 1] = (byte)TypeTag.Float32;

        var floatBytes = BitConverter.GetBytes(value);
        Buffer.BlockCopy(floatBytes, 0, bytes, alignedByteCount + 4, 4);

        return bytes;
    }
}

public class StringLengthParseTestCase
{
    public readonly byte[] Bytes;
    public readonly int Start;
    public readonly int Expected;
    
    public StringLengthParseTestCase(byte[] bytes, int start, int expected)
    {
        Bytes = bytes;
        Start = start;
        Expected = expected;
    }
}

internal static class StringLengthTestData
{

    public static IEnumerable StringLengthTestCases
    {

        get
        {

            // Four byte string
            var bytes1 = new byte[]
            {
                (byte)'a', (byte)'b', (byte)'c', (byte)'d', (byte)0, (byte)0, (byte)0, (byte)0,
                (byte)0, (byte)0, (byte)0, (byte)0, (byte)0, (byte)0, (byte)0, (byte)0,
            };

            yield return new StringLengthParseTestCase(bytes1, 0, 8);
            
            // One byte string
            var bytes2 = new byte[]
            {
                (byte)'a', (byte)0, (byte)0, (byte)0, (byte)0, (byte)0, (byte)0, (byte)0,
                (byte)0, (byte)0, (byte)0, (byte)0, (byte)0, (byte)0, (byte)0, (byte)0,
            };
            
            yield return new StringLengthParseTestCase(bytes2, 0, 4);
            
            // Three byte string
            var bytes3 = new byte[]
            {
                (byte)'a', (byte)'b', (byte)'c', (byte)0, (byte)0, (byte)0, (byte)0, (byte)0,
                (byte)0, (byte)0, (byte)0, (byte)0, (byte)0, (byte)0, (byte)0, (byte)0,
            };
            
            yield return new StringLengthParseTestCase(bytes3, 0, 4);
            
            // Eight byte string
            var bytes4 = new byte[]
            {
                (byte)'a', (byte)'b', (byte)'c', (byte)'d', (byte)'e', (byte)'f', (byte)'g', (byte)'h',
                (byte)0, (byte)0, (byte)0, (byte)0, (byte)0, (byte)0, (byte)0, (byte)0,
            };
            
            yield return new StringLengthParseTestCase(bytes4, 0, 12);
            
            // Seven byte string
            var bytes5 = new byte[]
            {
                (byte)'a', (byte)'b', (byte)'c', (byte)'d', (byte)'e', (byte)'f', (byte)'g', (byte)0,
                (byte)0, (byte)0, (byte)0, (byte)0, (byte)0, (byte)0, (byte)0, (byte)0,
            };
            
            yield return new StringLengthParseTestCase(bytes5, 0, 8);

        }
        
    }
    
}

public class TypeTagParseTestCase
{
    public readonly byte[] Bytes;
    public readonly int Start;
    public readonly TypeTag[] Expected;

    public TypeTagParseTestCase(byte[] bytes, int start, TypeTag[] expected)
    {
        Bytes = bytes;
        Start = start;
        Expected = expected;
    }
}

internal static class TagsTestData
{
    public static IEnumerable StandardTagParseCases
    {
        get
        {
            var expected1 = new[] { TypeTag.Float32, TypeTag.Float32, TypeTag.Int32, TypeTag.String };
            var bytes1 = new[]
            {
                (byte)',', (byte)TypeTag.Float32, (byte)TypeTag.Float32, (byte)TypeTag.Int32,
                (byte)TypeTag.String, (byte)0, (byte)0, (byte)0
            };

            yield return new TypeTagParseTestCase(bytes1, 0, expected1);

            var expected2 = new[]
            {
                TypeTag.Int32, TypeTag.Float32, TypeTag.String, TypeTag.String, TypeTag.Blob, TypeTag.Int32
            };
            var bytes2 = new[]
            {
                (byte)0, (byte)0, // offset of 2 bytes
                (byte)',',
                (byte)TypeTag.Int32, (byte)TypeTag.Float32, (byte)TypeTag.String,
                (byte)TypeTag.String, (byte)TypeTag.Blob, (byte)TypeTag.Int32,
                (byte)0, (byte)0 // trailing bytes
            };

            yield return new TypeTagParseTestCase(bytes2, 2, expected2);
        }
    }
}

internal static class MidiTestData
{
    public static IEnumerable Basic
    {
        get
        {
            var expected1 = new[]
            {
                (byte)1,                     // port id
                (byte)144,                   // status - ch1 note on
                (byte)60,                    // note # - 60 = middle c
                (byte)100                    // note velocity
            };
            var bytes1 = new[]
            {
                (byte)0, (byte)0, (byte)1, (byte)144,
                (byte)60, (byte)100, (byte)0, (byte)0,
            };

            yield return new TestCaseData(bytes1, 2, expected1);

            var expected2 = new[]
            {
                (byte)16,
                (byte)128,                  // status - ch1 note off
                (byte)72,                   // note C4
                (byte)42,
            };
            var bytes2 = new[]
            {
                (byte)16, (byte)128, (byte)72, (byte)42, (byte)0, (byte)0
            };

            yield return new TestCaseData(bytes2, 0, expected2);
        }
    }
}

internal static class BundleData
{
    public static byte[] GetRecursiveBundlesExample()
    {
        var writer = new OscWriter(512);

        var now = DateTime.Now;
        writer.WriteBundlePrefix();
        writer.Write(new NtpTimestamp(now));

        WriteFloatBundleElement(writer, "/composition/video/opacity", 0.5f);
        WriteFloatBundleElement(writer, "/composition/layers/2/video/opacity", 0.64f);

        writer.WriteBundlePrefix();
        writer.Write(new NtpTimestamp(now));

        WriteIntBundleElement(writer, "/composition/layers/1/video/mixer/blendmode", 24);
        WriteFloatBundleElement(writer, "/composition/layers/1/video/opacity", 0.72f);

        var bytes = new byte[writer.Length];
        writer.CopyBuffer(bytes, 0);
        return bytes;
    }

    private static void WriteIntBundleElement(OscWriter writer, string address, int value)
    {
        var typeTags = ",i";
        var firstAddressByteCount = Encoding.ASCII.GetByteCount(address).Align4();
        var firstTypeTagByteCount = Encoding.ASCII.GetByteCount(typeTags).Align4();
        var elementSize = firstAddressByteCount + firstTypeTagByteCount + 4;

        writer.Write(elementSize);
        writer.Write(address);
        writer.Write(typeTags);
        writer.Write(value);
    }

    private static void WriteFloatBundleElement(OscWriter writer, string address, float value)
    {
        var typeTags = ",f";
        var firstAddressByteCount = Encoding.ASCII.GetByteCount(address).Align4();
        var firstTypeTagByteCount = Encoding.ASCII.GetByteCount(typeTags).Align4();
        var elementSize = firstAddressByteCount + firstTypeTagByteCount + 4;

        writer.Write(elementSize);
        writer.Write(address);
        writer.Write(typeTags);
        writer.Write(value);
    }
}
