using System.Runtime.CompilerServices;

namespace Buildetech.OscCore;

/// <summary>
/// type tags from http://opensoundcontrol.org/spec-1_0 
/// </summary>
public enum TypeTag : byte
{
    False = (byte)'F',                    // F, non-standard
    Infinitum = (byte)'I',                // I, non-standard
    Nil = (byte)'N',                      // N, non-standard
    AltTypeString = (byte)'S',            // S, non-standard
    True = (byte)'T',                     // T, non-standard
    ArrayStart = (byte)'[',               // [, non-standard
    ArrayEnd = (byte)']',                 // ], non-standard
    Blob = (byte)'b',                     // b, STANDARD
    AsciiChar32 = (byte)'c',              // c, non-standard
    Float64 = (byte)'d',                  // d, non-standard
    Float32 = (byte)'f',                  // f, STANDARD
    Int64 = (byte)'h',                    // h, non-standard
    Int32 = (byte)'i',                    // i, STANDARD
    MIDI = (byte)'m',                     // m, non-standard
    Color32 = (byte)'r',                  // r, non-standard
    String = (byte)'s',                   // s, STANDARD
    TimeTag = (byte)'t'                   // t, non-standard
}

public static class TypeTagMethods
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSupported(this TypeTag tag)
    {
        return tag switch
        {
            TypeTag.False or
            TypeTag.Infinitum or
            TypeTag.Nil or
            TypeTag.AltTypeString or
            TypeTag.True or
            TypeTag.Blob or
            TypeTag.AsciiChar32 or
            TypeTag.Float64 or
            TypeTag.Float32 or
            TypeTag.Int64 or
            TypeTag.Int32 or
            TypeTag.MIDI or
            TypeTag.Color32 or
            TypeTag.String or
            TypeTag.TimeTag or
            TypeTag.ArrayStart or
            TypeTag.ArrayEnd => true,
            _ => false,
        };
    }
}

