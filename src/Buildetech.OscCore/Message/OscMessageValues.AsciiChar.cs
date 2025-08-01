using System;
using System.Runtime.CompilerServices;

namespace Buildetech.OscCore;

public sealed partial class OscMessageValues
{
    /// <summary>
    /// Read a non-standard ascii char element.
    /// Checks the element type before reading and throw <see cref="InvalidOperationException"/> if it does not have the 'c' type tag
    /// </summary>
    /// <param name="index">The element index</param>
    /// <returns>The character value if the element has the right type tag, default otherwise</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public char ReadAsciiCharElement(int index)
    {
#if OSCCORE_SAFETY_CHECKS
        if (OutOfBounds(index)) return default;
#endif
        return _tags[index] switch
        {
            // the ascii byte is placed at the end of the 4 bytes given for an element
            TypeTag.AsciiChar32 => (char)_sharedBuffer[_offsets[index] + 3],
            _ => throw new InvalidOperationException(),
        };
    }
}
