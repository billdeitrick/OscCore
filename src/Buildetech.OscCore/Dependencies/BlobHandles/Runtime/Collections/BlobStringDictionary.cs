using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.IL2CPP.CompilerServices.BlobHandles;

namespace BlobHandles;

/// <summary>
/// Designed to allow efficient matching of strings received as bytes (such as from network or disk) to values.
/// </summary>
/// <typeparam name="T">The type to associate a string key with</typeparam>
public sealed unsafe class BlobStringDictionary<T> : IDisposable
{
    private const int DefaultSize = 16;

    public readonly Dictionary<BlobHandle, T> HandleToValue;
    private readonly Dictionary<string, BlobString> _sourceToBlob;

    public BlobStringDictionary(int initialCapacity = DefaultSize)
    {
        HandleToValue = new Dictionary<BlobHandle, T>(initialCapacity);
        _sourceToBlob = new Dictionary<string, BlobString>(initialCapacity);
    }

    /// <summary>Converts a string into a BlobString and adds it and the value to the dictionary</summary>
    /// <param name="str">The string to add</param>
    /// <param name="value">The value to associate with the key</param>
    [Il2CppSetOption(Option.NullChecks, false)]
    public void Add(string str, T value)
    {
        if (str == null || _sourceToBlob.ContainsKey(str))
            return;

        var blobStr = new BlobString(str);
        HandleToValue.Add(blobStr.Handle, value);
        _sourceToBlob.Add(str, blobStr);
    }

    /// <summary>Adds a BlobString and its associated value to the dictionary</summary>
    /// <param name="blobStr">The blob string to add</param>
    /// <param name="value">The value to associate with the key</param>
    [Il2CppSetOption(Option.NullChecks, false)]
    public void Add(BlobString blobStr, T value)
    {
        HandleToValue.Add(blobStr.Handle, value);
    }

    /// <summary>Removes the value with the specified key</summary>
    /// <param name="str">The string to remove</param>
    /// <returns>true if the string was found and removed, false otherwise</returns>
    [Il2CppSetOption(Option.NullChecks, false)]
    public bool Remove(string str)
    {
        if (!_sourceToBlob.TryGetValue(str, out var blobStr))
            return false;

        _sourceToBlob.Remove(str);
        var removed = HandleToValue.Remove(blobStr.Handle);
        blobStr.Dispose();
        return removed;
    }

    /// <summary>Removes the value with the specified key</summary>
    /// <param name="blobStr">The blob string to remove</param>
    /// <returns>true if the string was found and removed, false otherwise</returns>
    [Il2CppSetOption(Option.NullChecks, false)]
    public bool Remove(BlobString blobStr)
    {
        return HandleToValue.Remove(blobStr.Handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Il2CppSetOption(Option.NullChecks, false)]
    public bool TryGetValueFromBytes(byte* ptr, int byteCount, out T value)
    {
        return HandleToValue.TryGetValue(new BlobHandle(ptr, byteCount), out value);
    }

    public void Clear()
    {
        HandleToValue.Clear();
        _sourceToBlob.Clear();
    }

    public void Dispose()
    {
        foreach (var kvp in _sourceToBlob)
            kvp.Value.Dispose();
    }
}
