using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using PKHeX.Core;
using PKHeX.Drawing.PokeSprite;
using PKHeX.WinForms.Controls;

namespace PKHeX.WinForms;

public sealed partial class SAV_GroupViewer : Form
{
    private readonly SaveFile SAV;
    private readonly IPKMView View;
    private readonly IReadOnlyList<SlotGroup> Groups;
    private readonly SummaryPreviewer Preview = new();

    private PictureBox? Hover;

    public int CurrentGroup { get; set; } = -1;

    public SAV_GroupViewer(SaveFile sav, IPKMView view, IReadOnlyList<SlotGroup> groups)
    {
        SAV = sav;
        View = view;
        InitializeComponent();

        Groups = groups;
        int count = groups[0].Slots.Length;
        Regenerate(count);
        CenterToParent();

        MouseWheel += (_, e) =>
        {
            CurrentGroup = e.Delta > 1 ? MoveLeft() : MoveRight();
            if (Hover is { } pb)
                HoverSlot(pb);
        };

        var names = groups.Select(z => $"{z.GroupName}").ToArray();
        CB_BoxSelect.Items.AddRange(names);
        CB_BoxSelect.SelectedIndex = GetFirstTeamWithContent(groups);

        if (Application.IsDarkModeEnabled)
        {
            WinFormsUtil.InvertToolStripIcons(mnu.Items);
            WinFormsTranslator.ReformatDark(B_BoxLeft);
            WinFormsTranslator.ReformatDark(B_BoxRight);
            WinFormsTranslator.ReformatDark(CB_BoxSelect);
        }

        foreach (PictureBox pb in Box.Entries)
        {
            pb.Click += (_, args) => OmniClick(pb, args);
            // A plain click does nothing here (Ctrl+click views), so the keyboard action is view.
            pb.KeyDown += (_, args) =>
            {
                if (args.KeyCode is not (Keys.Enter or Keys.Space))
                    return;
                ClickView(pb, args);
                args.Handled = true;
            };
            pb.Enter += (_, _) => DescribeSlot(pb);
            pb.ContextMenuStrip = mnu;
            pb.MouseMove += (_, args) => Preview.UpdatePreviewPosition(args.Location);
            pb.MouseEnter += (_, _) => HoverSlot(pb);
            pb.MouseLeave += (_, _) =>
            {
                Preview.Clear();
                Hover = null;
            };
        }
        FormClosing += (_, _) => Preview.Clear();
    }

    private void HoverSlot(PictureBox pb)
    {
        var group = Groups[CurrentGroup];
        var index = Box.Entries.IndexOf(pb);
        var slot = group.Slots[index];
        Preview.Show(pb, slot, group.Type);
        Hover = pb;
    }

    private void DescribeSlot(PictureBox pb)
    {
        var pk = Groups[CurrentGroup].Slots[Box.Entries.IndexOf(pb)];
        if (pk.Species == 0)
        {
            pb.AccessibleDescription = null;
            return;
        }
        var programLanguage = Language.GetLanguageValue(Main.Settings.Startup.Language);
        var settings = SummaryPreviewer.WithFirstLine(Main.Settings.BattleTemplate.Hover.GetSettings(programLanguage, pk.Context));
        pb.AccessibleDescription = ShowdownParsing.GetLocalizedPreviewText(pk, settings);
    }

    private void OmniClick(object sender, EventArgs e)
    {
        switch (ModifierKeys)
        {
            case Keys.Control: ClickView(sender, e); break;
            default:
                return;
        }
    }

    private static int GetFirstTeamWithContent(IReadOnlyList<SlotGroup> groups)
    {
        for (int i = 0; i < groups.Count; i++)
        {
            if (groups[i].Slots.Any(z => z.Species != 0))
                return i;
        }
        return 0;
    }

    private void Regenerate(int count)
    {
        int deltaW = Width - Box.Width;
        int deltaH = Height - Box.Height;
        var height = count / 5;
        var width = count / height;
        bool changed = Box.InitializeGrid(width, height, SpriteUtil.Spriter);
        if (!changed)
            return;

        Width = Box.Width + deltaW + 2;
        Height = Box.Height + deltaH + 2;

        RecenterControls();
    }

    private void RecenterControls()
    {
        if (Width < Box.Width)
            Width = Box.Width;
        Box.HorizontallyCenter(this);
        Box.Location = Box.Location with { X = Box.Location.X - 8 }; // manual fudge
        int p1 = CB_BoxSelect.Location.X;
        CB_BoxSelect.HorizontallyCenter(this);
        int p2 = CB_BoxSelect.Location.X;

        var delta = p2 - p1;
        if (delta == 0)
            return;

        B_BoxLeft.SetBounds(B_BoxLeft.Location.X + delta, 0, 0, 0, BoundsSpecified.X);
        B_BoxRight.SetBounds(B_BoxRight.Location.X + delta, 0, 0, 0, BoundsSpecified.X);
    }

    private void LoadGroup(int index)
    {
        if (index == CurrentGroup)
            return;

        var (_, slots, type) = Groups[index];
        Regenerate(slots.Length);

        var sav = SAV;
        for (int i = 0; i < slots.Length; i++)
        {
            Box.Entries[i].Image = slots[i].Sprite(sav, visibility: GetFlags(slots[i]), storage: type);
            // The grid's generated name (row/column) is meaningless when narrated.
            Box.Entries[i].AccessibleName = $"{Groups[index].GroupName} Slot {i + 1}";
        }

        if (slotSelected != -1 && (uint)slotSelected < Box.Entries.Count)
            Box.Entries[slotSelected].BackgroundImage = groupSelected != index ? null : SpriteUtil.Spriter.View;

        CurrentGroup = index;
    }

    private SlotVisibilityType GetFlags(PKM pk, bool ignoreLegality = false)
    {
        var result = SlotVisibilityType.None;
        if (!ignoreLegality)
            result |= SlotVisibilityType.CheckLegalityIndicate;
        return result;
    }

    public int MoveLeft(bool max = false)
    {
        int newBox = max ? 0 : (CurrentGroup + Groups.Count - 1) % Groups.Count;
        LoadGroup(newBox);
        return newBox;
    }

    public int MoveRight(bool max = false)
    {
        int newBox = max ? Groups.Count - 1 : (CurrentGroup + 1) % Groups.Count;
        LoadGroup(newBox);
        return newBox;
    }

    private int groupSelected = -1;
    private int slotSelected = -1;

    private void ClickView(object sender, EventArgs e)
    {
        if (!WinFormsUtil.TryGetUnderlying<PictureBox>(sender, out var pb))
            return;
        int index = Box.Entries.IndexOf(pb);

        var group = Groups[CurrentGroup];
        View.PopulateFields(group.Slots[index], false);

        if (slotSelected != index && (uint)slotSelected < Box.Entries.Count)
            Box.Entries[slotSelected].BackgroundImage = null;

        groupSelected = CurrentGroup;
        slotSelected = index;
        Box.Entries[index].BackgroundImage = SpriteUtil.Spriter.View;
    }

    private void B_BoxRight_Click(object sender, EventArgs e) => CB_BoxSelect.SelectedIndex = MoveRight((ModifierKeys & Keys.Control) != 0);
    private void B_BoxLeft_Click(object sender, EventArgs e) => CB_BoxSelect.SelectedIndex = MoveLeft((ModifierKeys & Keys.Control) != 0);
    private void CB_BoxSelect_SelectedIndexChanged(object sender, EventArgs e) => LoadGroup(CB_BoxSelect.SelectedIndex);
}
