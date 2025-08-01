using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Buildetech.OscCore;

public sealed class OscAddressSpace
{
    private const int DefaultPatternCapacity = 8;
    private const int DefaultCapacity = 16;

    internal readonly OscAddressMethods _addressToMethod;

    // Keep a list of registered address patterns and the methods they're associated with just like addresses
    internal int _patternCount;
    internal Regex[] _patterns = new Regex[DefaultPatternCapacity];
    internal OscActionPair[] _patternMethods = new OscActionPair[DefaultPatternCapacity];
    private readonly Queue<int> _freedPatternIndices = new();
    private readonly Dictionary<string, int> _patternStringToIndex = new();

    public int HandlerCount => _addressToMethod.HandleToValue.Count;

    public IEnumerable<string> Addresses => _addressToMethod._sourceToBlob.Keys;

    public OscAddressSpace(int startingCapacity = DefaultCapacity)
    {
        _addressToMethod = new OscAddressMethods(startingCapacity);
    }

    public bool TryAddMethod(string address, OscActionPair onReceived)
    {
        if (string.IsNullOrEmpty(address) || onReceived == null)
            return false;

        switch (OscParser.GetAddressType(address))
        {
            case AddressType.Address:
                _addressToMethod.Add(address, onReceived);
                return true;
            case AddressType.Pattern:
                int index;
                // if a method has already been registered for this pattern, add the new delegate
                if (_patternStringToIndex.TryGetValue(address, out index))
                {
                    _patternMethods[index] += onReceived;
                    return true;
                }

                if (_freedPatternIndices.Count > 0)
                {
                    index = _freedPatternIndices.Dequeue();
                }
                else
                {
                    index = _patternCount;
                    if (index >= _patterns.Length)
                    {
                        var newSize = _patterns.Length * 2;
                        Array.Resize(ref _patterns, newSize);
                        Array.Resize(ref _patternMethods, newSize);
                    }
                }

                _patterns[index] = new Regex(address);
                _patternMethods[index] = onReceived;
                _patternStringToIndex[address] = index;
                _patternCount++;
                return true;
            default:
                return false;
        }
    }

    public bool RemoveAddressMethod(string address)
    {
        if (string.IsNullOrEmpty(address))
            return false;

        return OscParser.GetAddressType(address) switch
        {
            AddressType.Address => _addressToMethod.RemoveAddress(address),
            _ => false,
        };
    }

    public bool RemoveMethod(string address, OscActionPair onReceived)
    {
        if (string.IsNullOrEmpty(address) || onReceived == null)
            return false;

        switch (OscParser.GetAddressType(address))
        {
            case AddressType.Address:
                return _addressToMethod.Remove(address, onReceived);
            case AddressType.Pattern:
                if (!_patternStringToIndex.TryGetValue(address, out var patternIndex))
                    return false;

                var method = _patternMethods[patternIndex].ValueRead;
                if (method.GetInvocationList().Length == 1)
                {
                    _patterns[patternIndex] = null!;
                    _patternMethods[patternIndex] = null!;
                }
                else
                {
                    _patternMethods[patternIndex] -= onReceived;
                }

                _patternCount--;
                _freedPatternIndices.Enqueue(patternIndex);
                return _patternStringToIndex.Remove(address);
            default:
                return false;
        }
    }

    /// <summary>
    /// Try to match an address against all known address patterns,
    /// and add a handler for the address if a pattern is matched
    /// </summary>
    /// <param name="address">The address to match</param>
    /// <param name="allMatchedMethods"></param>
    /// <returns>True if a match was found, false otherwise</returns>
    public bool TryMatchPatternHandler(string address, List<OscActionPair> allMatchedMethods)
    {
        if (!OscParser.AddressIsValid(address))
            return false;

        allMatchedMethods.Clear();

        bool any = false;
        for (var i = 0; i < _patternCount; i++)
        {
            if (_patterns[i].IsMatch(address))
            {
                var handler = _patternMethods[i];
                _addressToMethod.Add(address, handler);
                any = true;
            }
        }

        return any;
    }
}

