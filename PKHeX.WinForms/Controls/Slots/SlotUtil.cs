using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using PKHeX.Core;
using PKHeX.Drawing.PokeSprite;

namespace PKHeX.WinForms.Controls;

/// <summary>
/// Utility logic for drawing individual Slot views that represent underlying <see cref="PKM"/> data.
/// </summary>
public static class SlotUtil
{
    /// <summary>
    /// Gets the background image for a slot based on the provided <see cref="type"/>.
    /// </summary>
    public static Bitmap? GetTouchTypeBackground(SlotTouchType type) => type switch
    {
        SlotTouchType.None => null,
        SlotTouchType.Get => SpriteUtil.Spriter.View,
        SlotTouchType.Set => SpriteUtil.Spriter.Set,
        SlotTouchType.Delete => SpriteUtil.Spriter.Delete,
        SlotTouchType.Swap => SpriteUtil.Spriter.Set,
        SlotTouchType.Undo => SpriteUtil.Spriter.Delete,
        SlotTouchType.Redo => SpriteUtil.Spriter.Set,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    /// <summary>
    /// Gets the type of action that should be performed for a Drag &amp; Drop request.
    /// </summary>
    public static DropModifier GetDropModifier() => Control.ModifierKeys switch
    {
        Keys.Shift => DropModifier.Clone,
        Keys.Alt => DropModifier.Overwrite,
        _ => DropModifier.None,
    };

    public static readonly Color GoodDataColor = Color.Transparent;
    public static Color BadDataColor => WinFormsUtil.ColorWarn;

    /// <summary>
    /// Refreshes a <see cref="PictureBox"/> with the appropriate display content.
    /// </summary>
    public static void UpdateSlot(PictureBox pb, ISlotInfo info, PKM pk, SaveFile sav, SlotVisibilityType flags, SlotTouchType t = SlotTouchType.None)
    {
        pb.BackgroundImage = GetTouchTypeBackground(t);
        if (pk.Species == 0) // Nothing in slot
        {
            pb.Image = null;
            pb.BackColor = GoodDataColor;
            pb.AccessibleDescription = AppendNotes($"{pb.AccessibleName}: Empty", GetSlotNotes(info, pk, sav, flags));
            return;
        }
        if (!pk.Valid) // Invalid
        {
            // Bad Egg present in slot.
            pb.Image = null;
            pb.BackColor = BadDataColor;
            pb.AccessibleDescription = AppendNotes($"{pb.AccessibleName}: Invalid data", GetSlotNotes(info, pk, sav, flags)); // otherwise only the background colour differs
            return;
        }

        pb.Image = GetImage(info, pk, sav, flags);
        pb.BackColor = GoodDataColor;

        // Get an accessible description for the slot (for screen readers)
        var x = Main.Settings;
        var programLanguage = Language.GetLanguageValue(x.Startup.Language);
        var cfg = x.BattleTemplate;
        var settings = SummaryPreviewer.WithFirstLine(cfg.Hover.GetSettings(programLanguage, pk.Context));
        pb.AccessibleDescription = AppendNotes(ShowdownParsing.GetLocalizedPreviewText(pk, settings), GetSlotNotes(info, pk, sav, flags));
    }

    /// <summary>
    /// Adds the notes after the first line, which is expected to name the slot's contents.
    /// </summary>
    private static string AppendNotes(string text, string notes)
    {
        if (notes.Length == 0)
            return text;
        var index = text.IndexOf('\n');
        return index < 0 ? $"{text}, {notes}" : $"{text[..index].TrimEnd()}, {notes}{text[index..]}";
    }

    /// <summary>
    /// Spells out the state that <see cref="GetImage"/> only shows as sprite overlays (legality, lock, party and team markers).
    /// </summary>
    private static string GetSlotNotes(ISlotInfo info, PKM pk, SaveFile sav, SlotVisibilityType flags)
    {
        var notes = new List<string>(3);
        if (pk.Species != 0 && pk.Valid && flags.HasFlag(SlotVisibilityType.CheckLegalityIndicate))
        {
            var la = pk.GetType() == sav.PKMType // same choice as the sprite overlay
                ? new LegalityAnalysis(pk, sav.Personal, info.Type)
                : new LegalityAnalysis(pk, pk.PersonalInfo, info.Type);
            if (!la.Valid)
                notes.Add("Illegal");
            else if (pk.Format >= 8 && MoveInfo.IsDummiedMoveAny(pk))
                notes.Add("Has unusable moves");
        }
        if (info is SlotInfoBox b)
        {
            var slot = sav.GetBoxSlotFlags(b.Box, b.Slot);
            var team = slot.IsBattleTeam();
            if (team >= 0)
                notes.Add($"Battle team {team + 1}");
            if (slot.HasFlag(StorageSlotSource.Locked))
                notes.Add("Locked");
            if (slot.IsParty() >= 0)
                notes.Add("In party");
            if (slot.HasFlag(StorageSlotSource.Starter))
                notes.Add("Starter");
        }
        return string.Join(", ", notes);
    }

    private static Bitmap GetImage(ISlotInfo info, PKM pk, SaveFile sav, SlotVisibilityType flags) => info switch
    {
        SlotInfoBox b => pk.Sprite(sav, b.Box, b.Slot, flags, b.Type),
        SlotInfoParty ps => pk.Sprite(sav, -1, ps.Slot, flags, ps.Type),
        _ => pk.Sprite(sav, -1, -1, flags, info.Type),
    };
}
