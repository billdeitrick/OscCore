using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Buildetech.OscCore.Tests;
using NUnit.Framework;

namespace Buildetech.OscCore.Test.Runtime;

[TestOf(typeof(OscServer))]
public class OscServerTest
{
    private OscServer _server = null!;
    private OscClient _client = null!;

    [StructLayout(LayoutKind.Explicit)]
    private struct FloatInt
    {
        [FieldOffset(0)]
        public int Setter;
        [FieldOffset(0)]
        public float Value;

        public FloatInt(float value)
        {
            Setter = 0;
            Value = value;
        }
        public FloatInt(int setter)
        {
            Value = 0;
            Setter = setter;
        }
    }

    private static IEnumerable<float> AllFloatSource
    {
        get
        {
            var floatInt = new FloatInt();
            for (long i = int.MinValue; i < int.MaxValue; i += int.MaxValue / 100)
            {
                floatInt.Setter = (int)i;
                yield return floatInt.Value;
            }
            yield return new FloatInt(int.MaxValue).Value;
        }
    }
    private static IEnumerable<int> AllIntSource
    {
        get
        {
            for (long i = int.MinValue; i < int.MaxValue; i += int.MaxValue / 100)
            {
                yield return (int)i;
            }
            yield return int.MaxValue;
        }
    }

    [OneTimeSetUp]
    public void Setup()
    {
        _server = new OscServer(7000);
        _client = new OscClient("127.0.0.1", 7000);
    }
    [OneTimeTearDown]
    public void TearDown()
    {
        _server.Dispose();
        _client.Dispose();
    }

    [Test]
    public void CallbackTest()
    {
        MonitorCallback callback1 = (_, _) => { };
        MonitorCallback callback2 = (_, _) => { };

        _server.AddMonitorCallback(callback1);
        _server.AddMonitorCallback(callback1);

        Assert.IsTrue(_server.RemoveMonitorCallback(callback1));
        Assert.IsFalse(_server.RemoveMonitorCallback(callback2));
        Assert.IsTrue(_server.RemoveMonitorCallback(callback1));
        Assert.IsFalse(_server.RemoveMonitorCallback(callback1));
    }


    [Test]
    [TestCaseSource(nameof(AllFloatSource))]
    public void CallbackTest2(float value)
    {
        OscMessageValues? values = null;
        MonitorCallback callback1 = (_, v) => values = v;

        _server.AddMonitorCallback(callback1);

        _client.Send("/address/test", value);
        TestUtil.LoopWhile(() => values == null, TimeSpan.FromMilliseconds(1000)).Wait();
        Assert.AreEqual(value, values!.ReadFloatElement(0));
        values = null;

        Assert.IsTrue(_server.RemoveMonitorCallback(callback1));
    }

    [Test]
    public void RemoveAddress_RemovesListenerCorrectly()
    {
        string testAddress = "/address/remove";
        bool wasCalled = false;
        void Listener(OscMessageValues values) => wasCalled = true;

        // Add listener for address
        _server.TryAddMethod(testAddress, Listener);

        // Send packet, should trigger listener
        _client.Send(testAddress, 123.45f);
        TestUtil.LoopWhile(() => !wasCalled, TimeSpan.FromMilliseconds(1000)).Wait();
        Assert.IsTrue(wasCalled, "Listener should be called after first send.");

        // Remove listener
        Assert.IsTrue(_server.RemoveAddress(testAddress), "Failed to receive indication the listener was removed.");
        wasCalled = false;

        // Send packet again, should NOT trigger listener
        _client.Send(testAddress, 678.90f);
        TestUtil.LoopWhile(() => !wasCalled, TimeSpan.FromMilliseconds(1000)).Wait();
        Assert.IsFalse(wasCalled, "Listener should not be called after removal.");
    }

    [Test]
    public void RemoveMethod_WithRegex_RemovesListenersCorrectly()
    {
        string testAddress1 = "/address/regex/1";

        bool wasCalled = false;

        void Listener(OscMessageValues values) => wasCalled = true;

        // Add regex listener
        _server.TryAddMethod("/address/regex/.*", Listener);

        // Send packets, should trigger listener
        _client.Send(testAddress1, 1.0f);

        TestUtil.LoopWhile(() => !wasCalled, TimeSpan.FromMilliseconds(1000)).Wait();
        Assert.IsTrue(wasCalled, "Listener should be called after first send.");

        // Remove regex pattern listener
        Assert.IsTrue(_server.RemoveMethod("/address/regex/.*", Listener), "Failed to remove listeners with regex pattern.");
        wasCalled = false;

        // Send packets again, should NOT trigger listeners
        _client.Send(testAddress1, 3.0f);
        TestUtil.LoopWhile(() => !wasCalled, TimeSpan.FromMilliseconds(1000)).Wait();
        Assert.IsFalse(wasCalled, "Listener1 should not be called after removal.");
    }
}
