using System;
using System.Windows.Forms;
using PKHeX.Core;
using PKHeX.Drawing.Misc;

namespace PKHeX.WinForms.Controls;

public partial class MoveChoice : UserControl
{
    private EntityContext Context;
    private string Slot = "Move";

    public MoveChoice()
    {
        InitializeComponent();
        CB_Move.InitializeBinding();
    }

    public ushort SelectedMove { get => (ushort)WinFormsUtil.GetIndex(CB_Move); set => CB_Move.SelectedValue = (int)value; }
    public int PP { get => SelectedMove == 0 ? 0 : Util.ToInt32(TB_PP.Text); set => TB_PP.Text = value.ToString(); }
    public int PPUps { get => SelectedMove == 0 ? 0 : CB_PPUps.SelectedIndex; set => LoadClamp(CB_PPUps, value); }
    public bool HideLegality { private get; set; }
    public void SetContext(EntityContext context) => Context = context;

    /// <summary>
    /// Names the inner fields after the move slot, as all four slots are otherwise indistinguishable to screen readers.
    /// </summary>
    public void SetAccessibleSlot(string slot)
    {
        Slot = slot;
        CB_Move.AccessibleName = slot;
        TB_PP.AccessibleName = $"{slot} PP";
        CB_PPUps.AccessibleName = $"{slot} PP Ups";
    }

    private void UpdateTypeSprite(int value)
    {
        if (value <= 0)
        {
            PB_Type.Image = null;
            PB_Type.AccessibleName = $"{Slot} type: None";
            return;
        }

        var type = MoveInfo.GetType((ushort)value, Context);
        PB_Type.Image = TypeSpriteUtil.GetTypeSpriteIconSmall(type);
        PB_Type.AccessibleName = $"{Slot} type: {GameInfo.Strings.Types[type]}";
    }

    private static void LoadClamp(ComboBox cb, int value)
    {
        var max = cb.Items.Count - 1;
        if (value > max)
            value = max;
        else if (value < -1)
            value = 0;
        cb.SelectedIndex = value;
    }

    public void UpdateLegality(MoveResult move, PKM entity, int i)
    {
        if (HideLegality)
        {
            PB_Triangle.Visible = false;
            return;
        }
        PB_Triangle.Visible = true;
        PB_Triangle.Image = MoveDisplayState.GetMoveImage(!move.Valid, entity, i);
        PB_Triangle.AccessibleName = $"{Slot} {MoveDisplayState.GetMoveStatus(!move.Valid, entity, i)}";
    }

    public void HealPP(PKM pk)
    {
        var move = SelectedMove;
        var up = PPUps;
        if (move == 0)
            PPUps = up = 0;
        PP = pk.GetMovePP(move, up);
    }

    private void CB_Move_SelectedIndexChanged(object sender, EventArgs e)
    {
        var value = WinFormsUtil.GetIndex(CB_Move);
        UpdateTypeSprite(value);
    }
}
