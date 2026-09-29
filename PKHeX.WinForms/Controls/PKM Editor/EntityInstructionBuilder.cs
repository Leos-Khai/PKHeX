using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using PKHeX.Core;
using static PKHeX.Core.MessageStrings;

namespace PKHeX.WinForms.Controls;

public partial class EntityInstructionBuilder : UserControl
{
    private readonly Func<PKM> Getter;
    private PKM Entity => Getter();

    private static ReadOnlySpan<char> Prefixes => StringInstruction.Prefixes;

    // The buttons and menu items only show operator symbols, which are read out unhelpfully. Order matches Prefixes.
    private static readonly string[] PrefixNames =
    [
        "Set value",
        "Require equal to", "Require not equal to", "Require greater than", "Require greater than or equal to", "Require less than", "Require less than or equal to",
        "Add", "Subtract", "Multiply", "Divide", "Modulo",
        "Bitwise and", "Bitwise or", "Bitwise xor", "Shift right", "Shift left",
    ];

    private int currentFormat = -1;
    private int requirementIndex;
    private bool readOnlyMode;
    private readonly ToolStripMenuItem[] requireMenuItems = new ToolStripMenuItem[Prefixes.Length];

    public EntityInstructionBuilder(Func<PKM> pk)
    {
        Getter = pk;
        InitializeComponent();
        for (int i = 0; i < Prefixes.Length; i++)
        {
            var prefix = Prefixes[i];
            var name = (i == 0 ? "Set" : prefix.ToString());
            var text = i == 0 ? "&Set" : (prefix is '&' ? "&&" : prefix.ToString()); // activator key sanitization
            var color = StringInstruction.IsMutationInstruction(prefix) ? WinFormsUtil.ColorWarn : SystemColors.ControlText;
            var item = new ToolStripMenuItem(text)
            {
                Name = $"mnu_{name}",
                Tag = i,
                ForeColor = color,
                AccessibleName = i < PrefixNames.Length ? PrefixNames[i] : text,
            };
            item.Click += RequireItem_Click;
            requireMenu.Items.Add(item);
            requireMenuItems[i] = item;
        }

        // Allow translation of the menu item.
        WinFormsTranslator.TranslateControls("BatchEdit", requireMenuItems, Main.CurrentLanguage);

        B_Require.ContextMenuStrip = requireMenu;

        CB_Format.Items.Clear();
        CB_Format.Items.Add(MsgAny);
        foreach (Type t in EntityBatchEditor.Instance.Types)
            CB_Format.Items.Add(t.Name.ToLowerInvariant());
        CB_Format.Items.Add(MsgAll);
        CB_Property.KeyDown += (_, _) => CB_Property.DroppedDown = false;

        CB_Format.SelectedIndex = 0;
        SetRequirementIndex(0);
        UpdateRequireMenuVisibility();
        toolTip1.SetToolTip(CB_Property, MsgBEToolTipPropName);
        toolTip2.SetToolTip(L_PropType, MsgBEToolTipPropType);
        toolTip3.SetToolTip(L_PropValue, MsgBEToolTipPropValue);
    }

    private void CB_Format_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (currentFormat == CB_Format.SelectedIndex)
            return;

        byte format = (byte)CB_Format.SelectedIndex;
        CB_Property.Items.Clear();
        CB_Property.Items.AddRange(EntityBatchEditor.Instance.Properties[format]);
        CB_Property.SelectedIndex = 0;
        currentFormat = format;
    }

    private void CB_Property_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (!EntityBatchEditor.Instance.TryGetPropertyType(CB_Property.Text, out var type, CB_Format.SelectedIndex))
            type = "Unknown";
        L_PropType.Text = type;

        if (EntityBatchEditor.Instance.TryGetHasProperty(Entity, CB_Property.Text, out var pi))
        {
            L_PropType.ResetForeColor();
            L_PropType.AccessibleDescription = null;

            bool hasValue = GetPropertyDisplayText(pi, Entity, out var display);
            L_PropValue.Text = display;
            if (hasValue)
            {
                L_PropValue.ResetForeColor();
                L_PropValue.AccessibleDescription = null;
            }
            else
            {
                L_PropValue.ForeColor = WinFormsUtil.ColorWarn;
                L_PropValue.AccessibleDescription = "Value cannot be displayed";
            }
        }
        else // no property, flag
        {
            L_PropValue.Text = string.Empty;
            L_PropType.ForeColor = WinFormsUtil.ColorWarn;
            L_PropType.AccessibleDescription = "Property does not exist on the current entity";
        }
    }

    private void B_Require_Click(object? sender, EventArgs e) => requireMenu.Show(B_Require, 0, B_Require.Height);

    private void RequireItem_Click(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem { Tag: int index })
            return;

        SetRequirementIndex(index);
    }

    private void SetRequirementIndex(int index)
    {
        if ((uint)index >= Prefixes.Length)
            return;

        requirementIndex = index;
        B_Require.Text = requireMenuItems[index].Text;
        B_Require.AccessibleName = $"Instruction Type: {requireMenuItems[index].AccessibleName}";

        for (int i = 0; i < requireMenuItems.Length; i++)
            requireMenuItems[i].Checked = i == index;
    }

    private void UpdateRequireMenuVisibility()
    {
        requireMenuItems[0].Visible = !readOnlyMode;

        if (readOnlyMode && requirementIndex == 0)
            SetRequirementIndex(1);
    }

    private static bool GetPropertyDisplayText(PropertyInfo pi, PKM pk, out string display)
    {
        var type = pi.PropertyType;
        if (type.IsGenericType)
        {
            if (type.GetGenericTypeDefinition().IsByRefLike) // Span, ReadOnlySpan
            {
                display = pi.PropertyType.ToString();
                return false;
            }
        }

        var value = pi.GetValue(pk);
        if (value?.ToString() is not { } x)
        {
            display = "null";
            return false;
        }

        display = x;
        return true;
    }

    public string Create()
    {
        if (CB_Property.SelectedIndex < 0)
            return string.Empty;

        var property = CB_Property.Items[CB_Property.SelectedIndex];
        var prefix = Prefixes[requirementIndex];
        const char equals = StringInstruction.SplitInstruction;
        return $"{prefix}{property}{equals}";
    }

    public bool ReadOnly
    {
        set
        {
            readOnlyMode = value;
            UpdateRequireMenuVisibility();
        }
    }
}
