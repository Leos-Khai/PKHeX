using System.Drawing;
using System.Runtime.CompilerServices;
using PKHeX.Core;
using PKHeX.Drawing.PokeSprite.Properties;

namespace PKHeX.WinForms.Controls;

public static class MoveDisplayState
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Bitmap? GetMoveImage(bool isIllegal, PKM pk, int index)
    {
        if (isIllegal)
            return Resources.warn;

        if (MoveInfo.IsDummiedMove(pk, index))
            return Resources.hint;

        return null;
    }

    /// <summary>
    /// Spoken counterpart of <see cref="GetMoveImage"/>, as the warning icon is otherwise the only indication.
    /// </summary>
    public static string GetMoveStatus(bool isIllegal, PKM pk, int index)
    {
        if (isIllegal)
            return "is illegal";

        if (MoveInfo.IsDummiedMove(pk, index))
            return "cannot be used in battle";

        return "is legal";
    }
}
