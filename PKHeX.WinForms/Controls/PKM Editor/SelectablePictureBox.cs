using System;
using System.Windows.Forms;
using System.Windows.Forms.Automation;

namespace PKHeX.WinForms.Controls;

/// <summary>
/// PictureBox control that can be focused and selected.
/// </summary>
/// <remarks>Draws a focus rectangle, and can be tabbed between, raising events for screen readers.</remarks>
public class SelectablePictureBox : PictureBox
{
    public SelectablePictureBox() => SetStyle(ControlStyles.Selectable, TabStop = true);

    public static int FocusBorderDeflate { get; set; }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        Focus();
        base.OnMouseDown(e);
    }
    // Claim Enter so a form's AcceptButton doesn't swallow it before KeyDown handlers see it.
    protected override bool IsInputKey(Keys keyData) => (keyData & Keys.KeyCode) == Keys.Enter || base.IsInputKey(keyData);

    protected override void OnEnter(EventArgs e)
    {
        Invalidate();
        base.OnEnter(e);
        AccessibilityObject.RaiseAutomationNotification(AutomationNotificationKind.Other,
            AutomationNotificationProcessing.All, GetNarration());
    }

    // Contents first, then the name (e.g. the slot position), so moving through slots hears what's in each one sooner.
    // Skip the name if the description already starts with it.
    private string GetNarration()
    {
        var name = AccessibleName;
        var description = AccessibleDescription;
        if (string.IsNullOrEmpty(description))
            return name ?? string.Empty;
        if (string.IsNullOrEmpty(name) || description.StartsWith(name, StringComparison.Ordinal))
            return description;
        return $"{description}{Environment.NewLine}{name}";
    }
    protected override void OnLeave(EventArgs e)
    {
        Invalidate();
        base.OnLeave(e);
    }
    protected override void OnPaint(PaintEventArgs pe)
    {
        base.OnPaint(pe);
        if (!Focused)
            return;
        var rc = ClientRectangle;
        rc.Inflate(-FocusBorderDeflate, -FocusBorderDeflate);
        ControlPaint.DrawFocusRectangle(pe.Graphics, rc);
    }
}
