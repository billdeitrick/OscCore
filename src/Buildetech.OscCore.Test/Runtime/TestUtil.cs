using System;
using System.Threading;
using System.Threading.Tasks;

namespace Buildetech.OscCore.Tests;

public static class TestUtil
{
    public static readonly Random SharedRandom = new();
    private static readonly byte[] _swap32 = new byte[4];
    private static readonly byte[] _swap64 = new byte[8];

    public static int ReverseBytes(this int self)
    {
        _swap32[0] = (byte)(self >> 24);
        _swap32[1] = (byte)(self >> 16);
        _swap32[2] = (byte)(self >> 8);
        _swap32[3] = (byte)(self);
        return BitConverter.ToInt32(_swap32, 0);
    }

    public static float ReverseBytes(this float self)
    {
        var fBytes = BitConverter.GetBytes(self);
        _swap32[0] = fBytes[3];
        _swap32[1] = fBytes[2];
        _swap32[2] = fBytes[1];
        _swap32[3] = fBytes[0];
        return BitConverter.ToSingle(_swap32, 0);
    }

    public static double ReverseBytes(this double self)
    {
        var dBytes = BitConverter.GetBytes(self);
        _swap64[0] = dBytes[7];
        _swap64[1] = dBytes[6];
        _swap64[2] = dBytes[5];
        _swap64[3] = dBytes[4];
        _swap64[4] = dBytes[3];
        _swap64[5] = dBytes[2];
        _swap64[6] = dBytes[1];
        _swap64[7] = dBytes[0];
        return BitConverter.ToDouble(_swap64, 0);
    }

    public static byte[] ReversedCopy(byte[] source)
    {
        var copy = new byte[source.Length];
        Array.Copy(source, copy, source.Length);
        Array.Reverse(copy);
        return copy;
    }
    public static unsafe byte[] ReversedCopy(byte* source, int length)
    {
        var copy = new byte[length];
        fixed (byte* copyPtr = copy)
        {
            Buffer.MemoryCopy(source, copyPtr, length, length);
        }
        Array.Reverse(copy);
        return copy;
    }

    public static byte[] RandomFloatBytes(int byteCount = 2048)
    {
        var bytes = new byte[byteCount];
        for (int i = 0; i < bytes.Length; i += 4)
        {
            var f = SharedRandom.Next() * 2f - 1f;
            var fBytes = BitConverter.GetBytes(f);
            for (int j = 0; j < fBytes.Length; j++)
            {
                bytes[i + j] = fBytes[j];
            }
        }

        return bytes;
    }

    public static byte[] RandomIntBytes(int byteCount = 2048)
    {
        var bytes = new byte[byteCount];
        for (int i = 0; i < bytes.Length; i += 4)
        {
            var iValue = SharedRandom.Next(-1000, 1000);
            var iBytes = BitConverter.GetBytes(iValue);
            for (int j = 0; j < iBytes.Length; j++)
                bytes[i + j] = iBytes[j];
        }

        return bytes;
    }

    public static byte[] RandomColor32Bytes(int byteCount = 2048)
    {
        var bytes = new byte[byteCount];
        for (int i = 0; i < bytes.Length; i += 4)
        {
            var iValue = SharedRandom.Next(0, 255);
            var iBytes = BitConverter.GetBytes(iValue);
            for (int j = 0; j < iBytes.Length; j++)
                bytes[i + j] = iBytes[j];
        }

        return bytes;
    }

    public static byte[] RandomMidiBytes(int byteCount = 2048)
    {
        var bytes = new byte[byteCount];
        for (int i = 0; i < bytes.Length; i += 4)
        {
            var port = (byte)SharedRandom.Next(1, 16);
            var status = (byte)SharedRandom.Next(0, 127);
            var data1 = (byte)SharedRandom.Next(10, 255);
            var data2 = (byte)SharedRandom.Next(10, 255);
            bytes[i] = port;
            bytes[i + 1] = status;
            bytes[i + 2] = data1;
            bytes[i + 3] = data2;
        }

        return bytes;
    }

    public static byte[] RandomTimestampBytes(int count = 2048)
    {
        var bytes = new byte[count];
        for (int i = 0; i < bytes.Length; i += 8)
        {
            var seconds = SharedRandom.Next(0, 255);
            var sBytes = BitConverter.GetBytes(seconds);
            for (int j = 0; j < sBytes.Length; j++)
                bytes[i + j] = sBytes[j];

            var fractions = SharedRandom.Next(0, 10000000);
            var fBytes = BitConverter.GetBytes(fractions);

            var end = 4 + fBytes.Length;
            for (int j = 4; j < end; j++)
                bytes[i + j] = fBytes[j - 4];
        }

        return bytes;
    }

    public static async Task LoopWhile(Func<bool> conditions, TimeSpan timeout)
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        source.CancelAfter(timeout);
        await Task.Run(() => { while (conditions()) ; }, source.Token);
    }
}
