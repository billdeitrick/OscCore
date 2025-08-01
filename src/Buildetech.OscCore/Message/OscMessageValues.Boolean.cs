using System.Runtime.CompilerServices;

namespace Buildetech.OscCore;

public sealed partial class OscMessageValues
{
    /// <summary>
    /// Checks the element type before reading and throw <see cref="InvalidOperationException"/> if it's not interpretable as a boolean.
    /// </summary>
    /// <param name="index">The element index</param>
    /// <returns>The value of the element</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadBooleanElement(int index)
    {
#if OSCCORE_SAFETY_CHECKS
        if (OutOfBounds(index)) return default;
#endif
        return _tags[index] switch
        {
            TypeTag.True => true,
            TypeTag.False => false,
            TypeTag.Int32 => ReadIntElementUnchecked(index) > 0,
            _ => throw new InvalidOperationException(),
        };
    }
}
