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
}
