using System;
using System.Windows.Forms;

namespace PKHeX.WinForms.Controls;

/// <summary>
/// Label that doubles as a click target, so it can be tabbed to and activated with Enter/Space like a button.
/// </summary>
/// <remarks>Plain labels can't take focus, which left their click shortcuts unreachable for keyboard and screen reader users.</remarks>
public class SelectableLabel : Label
{
    public SelectableLabel()
    {
        SetStyle(ControlStyles.Selectable, true);
        TabStop = true;
        AccessibleRole = AccessibleRole.PushButton;
    }

    // Claim Enter so a form's AcceptButton doesn't swallow it before the label sees the keypress.
    protected override bool IsInputKey(Keys keyData) => (keyData & Keys.KeyCode) == Keys.Enter || base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Enter or Keys.Space)
        {
            OnClick(EventArgs.Empty);
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        Invalidate();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        Invalidate();
        base.OnLostFocus(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Focused && ShowFocusCues)
            ControlPaint.DrawFocusRectangle(e.Graphics, ClientRectangle);
    }
}
