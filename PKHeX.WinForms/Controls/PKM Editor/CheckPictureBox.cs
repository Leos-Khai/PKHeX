using System;
using System.Windows.Forms;

namespace PKHeX.WinForms.Controls;

/// <summary>
/// Picture that toggles a value when clicked, reported to screen readers as a check box.
/// </summary>
/// <remarks>The picture's opacity is the only visual cue for its state, so the state has to be exposed as well.</remarks>
public sealed class CheckPictureBox : SelectablePictureBox
{
    public CheckPictureBox() => AccessibleRole = AccessibleRole.CheckButton;

    public bool Checked { get; set; }

    protected override AccessibleObject CreateAccessibilityInstance() => new CheckPictureBoxAccessibleObject(this);

    private sealed class CheckPictureBoxAccessibleObject(CheckPictureBox owner) : SelectablePictureBoxAccessibleObject(owner)
    {
        public override AccessibleRole Role => AccessibleRole.CheckButton;
        public override AccessibleStates State => owner.Checked ? base.State | AccessibleStates.Checked : base.State;
        // NVDA reads a check box's state from UIA's toggle pattern, which WinForms doesn't let a custom control provide; read it as help text instead.
        public override string Help => owner.Checked ? "checked" : "not checked";
        public override string DefaultAction => owner.Checked ? "Uncheck" : "Check";
        public override void DoDefaultAction() => owner.OnClick(EventArgs.Empty);
    }
}
