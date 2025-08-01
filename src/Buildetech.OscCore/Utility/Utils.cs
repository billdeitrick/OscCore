using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using BlobHandles;

namespace Buildetech.OscCore;

internal static class Utils
{
    private static readonly List<char> _tempChars = new();
    private static readonly StringBuilder _builder = new();

    public static bool ValidateAddress(ref string address)
    {
        if (string.IsNullOrEmpty(address))
            address = "/";
        if (address[0] != '/')
            address = address.Insert(0, "/");
        if (address.EndsWith(" "))
            address = address.TrimEnd(' ');

        address = ReplaceInvalidAddressCharacters(address);
        return true;
    }

    internal static string ReplaceInvalidAddressCharacters(string address)
    {
        _tempChars.Clear();
        _tempChars.AddRange(address.Where(OscParser.CharacterIsValidInAddress));
        return new string(_tempChars.ToArray());
    }

    public static unsafe TPtr* PinPtr<TData, TPtr>(TData[] array, out GCHandle handle)
        where TData : unmanaged
        where TPtr : unmanaged
    {
        return (TPtr*)PinPtr(array, out handle);
    }
    public static unsafe IntPtr PinPtr<TData>(TData[] array, out GCHandle handle)
        where TData : unmanaged
    {
        handle = GCHandle.Alloc(array, GCHandleType.Pinned);
        return handle.AddrOfPinnedObject();
    }

    internal static string GetLocalIpAddress()
    {
        string localIP = "unknown";
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (IPAddress ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                localIP = ip.ToString();
                break;
            }
        }
        return localIP;
    }

    public static string MonitorMessageToString(BlobString address, OscMessageValues values)
    {
        _builder.Clear();
        _builder.Append(address.ToString());
        const string divider = "  ,";
        _builder.Append(divider);
        values.ForEachElement((i, type) => { _builder.Append((char)type); });
        _builder.Append("  ");

        var lastIndex = values.ElementCount - 1;
        values.ForEachElement((i, type) =>
        {
            var elementText = values.ReadStringElement(i);
            _builder.Append(elementText);
            if (i != lastIndex) _builder.Append(' ');
        });

        return _builder.ToString();
    }

}
