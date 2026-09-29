using System.Windows.Forms;

namespace PKHeX.WinForms.Controls;

/// <summary>
/// Image cell that narrates its <see cref="DataGridViewCell.Tag"/> text (or nothing) instead of the image's type name.
/// </summary>
/// <remarks>A plain <see cref="DataGridViewImageCell"/> is read as "System.Drawing.Bitmap", which interrupts every row for screen readers.</remarks>
public sealed class AccessibleImageCell : DataGridViewImageCell
{
    protected override AccessibleObject CreateAccessibilityInstance() => new ImageCellAccessibleObject(this);

    private sealed class ImageCellAccessibleObject(DataGridViewCell owner) : DataGridViewCellAccessibleObject(owner)
    {
        public override string Value => Owner?.Tag as string ?? string.Empty;
    }
}
