using System;
using System.Windows.Forms;
using System.Windows.Forms.Automation;
using PKHeX.Core;
using PKHeX.Drawing.PokeSprite;

namespace PKHeX.WinForms.Controls;

public partial class StatusConditionView : UserControl
{
    private PKM? pk;
    private bool Loading;
    private readonly ToolTip Hover = new()
    {
        AutoPopDelay = 5000,
        InitialDelay = 200,
        ReshowDelay = 500,
        ShowAlways = true,
    };

    public StatusConditionView()
    {
        InitializeComponent();
        PB_Status.MouseHover += (_, _) => PB_Status.Cursor = Cursors.Hand;
        PB_Status.PreviewKeyDown += (_, e) => e.IsInputKey |= e.KeyCode is Keys.Up or Keys.Down;
        PB_Status.KeyDown += (_, e) =>
        {
            if (e.KeyCode is Keys.Enter or Keys.Space)
                PB_Status_Click(PB_Status, e);
            else if (e.KeyCode is Keys.Up or Keys.Down)
                StepStatus(e.KeyCode == Keys.Down ? 1 : -1);
        };
    }

    // Arrow keys change the status in place, like a combo box, without opening the browser.
    // Same choices and order as the browser; Sleep picks 1 turn (the browser can set more).
    private void StepStatus(int delta)
    {
        if (pk is null)
            return;
        var generation = pk.Format;
        StatusCondition[] choices = generation is 3 or 4
            ? [StatusCondition.None, StatusCondition.Sleep1, StatusCondition.Poison, StatusCondition.Burn, StatusCondition.Paralysis, StatusCondition.Freeze, StatusCondition.PoisonBad]
            : [StatusCondition.None, StatusCondition.Sleep1, StatusCondition.Poison, StatusCondition.Burn, StatusCondition.Paralysis, StatusCondition.Freeze];
        var current = pk.Status_Condition & 0xFF;
        var index = Array.FindIndex(choices, c => generation <= 4
            ? c == (StatusCondition)current || (c == StatusCondition.Sleep1 && ((StatusCondition)current).GetStatusType() == StatusType.Sleep)
            : c.GetStatusType() == (StatusType)current);
        var next = Math.Clamp(index + delta, 0, choices.Length - 1);
        if (next == index)
            return;
        SetChoice(choices[next], generation);
        PB_Status.AccessibilityObject.RaiseAutomationNotification(AutomationNotificationKind.Other,
            AutomationNotificationProcessing.MostRecent, PB_Status.AccessibleName ?? string.Empty);
    }

    public void LoadPKM(PKM entity)
    {
        pk = entity;
        LoadStoredValues();
    }

    public void LoadStoredValues()
    {
        if (pk is null || Loading)
            return;
        Loading = true;
        if (!pk.PartyStatsPresent)
            ClearStatus();
        else if (pk.Stat_HPCurrent == 0)
            SetFaint();
        else
            SetStatus(pk.Status_Condition, pk.Format);
        Loading = false;
    }

    private void SetFaint()
    {
        PB_Status.Image = Drawing.PokeSprite.Properties.Resources.sickfaint;
        Hover.RemoveAll();
        SetAccessibleStatus("Fainted");
    }

    private void ClearStatus()
    {
        PB_Status.Image = null;
        Hover.RemoveAll();
        SetAccessibleStatus("None");
    }

    private void SetStatus(int value, int generation)
    {
        if (generation <= 4)
        {
            StatusCondition status = (StatusCondition)(value & 0xFF);
            PB_Status.Image = status.GetStatusSprite();

            var text = WinFormsTranslator.TranslateEnum(status, Main.CurrentLanguage);
            Hover.SetToolTip(PB_Status, $"Status Condition: {text}");
            SetAccessibleStatus(text);
        }
        else
        {
            StatusType status = (StatusType)(value & 0xFF);
            PB_Status.Image = status.GetStatusSprite();

            var text = WinFormsTranslator.TranslateEnum(status, Main.CurrentLanguage);
            Hover.SetToolTip(PB_Status, $"Status Condition: {text}");
            SetAccessibleStatus(text);
        }
    }

    private void SetAccessibleStatus(string text)
    {
        PB_Status.AccessibleName = $"Status Condition: {text}";
        PB_Status.AccessibleDescription = $"Status Condition: {text}. Use Up and Down to change, or press Enter to open the status browser.";
    }

    private void PB_Status_Click(object sender, EventArgs e)
    {
        ArgumentNullException.ThrowIfNull(pk);
        int generation = pk.Format;
        using var form = new StatusBrowser(generation);
        form.LoadList(pk);
        form.ShowDialog();
        if (!form.WasChosen)
            return;
        SetChoice(form.Choice, generation);
    }

    private void SetChoice(StatusCondition choice, int generation)
    {
        ArgumentNullException.ThrowIfNull(pk);
        var current = pk.Status_Condition;
        current &= ~0xFF;
        current |= generation <= 4 ? (int)choice : (int)choice.GetStatusType();
        pk.Status_Condition = current;
        LoadStoredValues();
    }
}
