using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace BuildSoft.OscCore.Tests;

public class ParsingTests
{
    private const int BufferSize = 4096;
    private readonly byte[] _buffer = new byte[BufferSize];
    private OscParser _parser = null!;

    [OneTimeSetUp]
    public void BeforeAll()
    {
        _parser = new OscParser();
    }

    [SetUp]
    public void BeforeEach()
    {
        _parser.MessageValues.ElementCount = 0;
        Array.Clear(_parser._buffer, 0, BufferSize); // clear the buffer before each test
    }

    [OneTimeTearDown]
    public void AfterAll()
    {

    }

    [TestCaseSource(typeof(StringLengthTestData), nameof(StringLengthTestData.StringLengthTestCases))]
    public void StringLengthParsing(StringLengthParseTestCase testCase)
    {

        // manually copy into the parser buffer
        for (var i = 0; i < testCase.Bytes.Length; i++)
        {
            _parser._buffer[i] = testCase.Bytes[i];
        }
        
        Assert.AreEqual(testCase.Expected, _parser.GetStringLength(testCase.Start));
        

    }

    [TestCaseSource(typeof(TagsTestData), nameof(TagsTestData.StandardTagParseCases))]
    public void SimpleTagParsing(TypeTagParseTestCase test)
    {
        var tagSize = _parser.ParseTags(test.Bytes, test.Start);
        var tagCount = tagSize - 1; // remove ','

        Assert.AreEqual(test.Expected.Length, tagCount);
        var tags = _parser.MessageValues._tags;
        for (var i = 0; i < tagCount; i++)
        {
            var tag = tags[i];
            Assert.AreEqual(test.Expected[i], tag);
        }
    }

    [Test]
    public void TagParsing_MustStartWithComma()
    {
        var commaAfterStart = new byte[] { 0, (byte)',', 1, 2 };
        Assert.Zero(_parser.ParseTags(commaAfterStart));
        Assert.Zero(_parser.MessageValues.ElementCount);

        var noCommaBeforeTags = new byte[] { (byte)'f', (byte)'i', 1, 2 };
        Assert.Zero(_parser.ParseTags(noCommaBeforeTags));
        Assert.Zero(_parser.MessageValues.ElementCount);
    }

    [TestCaseSource(typeof(MidiTestData), nameof(MidiTestData.Basic))]
    public void BasicMidiParsing(byte[] bytes, int offset, byte[] expected)
    {
        var midi = new MidiMessage(bytes, offset);

        Assert.AreEqual(expected[0], midi.PortId);
        Assert.AreEqual(expected[1], midi.Status);
        Assert.AreEqual(expected[2], midi.Data1);
        Assert.AreEqual(expected[3], midi.Data2);
    }
}
