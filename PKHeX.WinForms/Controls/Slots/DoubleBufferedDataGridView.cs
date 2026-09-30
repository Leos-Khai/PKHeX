using System.Drawing;
using System.Windows.Forms;

namespace PKHeX.WinForms.Controls;

/// <summary>
/// Provides a DataGridView with double buffering enabled to reduce flicker.
/// </summary>
internal class DoubleBufferedDataGridView : DataGridView
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DoubleBufferedDataGridView"/> class.
    /// </summary>
    public DoubleBufferedDataGridView()
    {
        if (Application.IsDarkModeEnabled) // NET10
        {
            EnableHeadersVisualStyles = false;
            BorderStyle = BorderStyle.None;
            RowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Drawing.ColorUtil.Blend(SystemColors.ControlLight, SystemColors.ControlLightLight, 0.45) };
        }

        AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = SystemColors.ControlLight };
    }

    /// <summary>
    /// Up on the first visible row moves to the last, and Down on the last moves to the first.
    /// </summary>
    /// <remarks>Long lists are otherwise slow to reach the end of from the keyboard.</remarks>
    public bool WrapRows { get; init; }

    protected override bool ProcessDataGridViewKey(KeyEventArgs e)
    {
        if (WrapRows && e.Modifiers == 0 && e.KeyCode is (Keys.Up or Keys.Down) && CurrentCell is { } cell)
        {
            const DataGridViewElementStates visible = DataGridViewElementStates.Visible;
            var first = Rows.GetFirstRow(visible);
            var last = Rows.GetLastRow(visible);
            var (edge, target) = e.KeyCode == Keys.Up ? (first, last) : (last, first);
            if (cell.RowIndex == edge && target != edge)
            {
                CurrentCell = this[cell.ColumnIndex, target];
                return true;
            }
        }
        return base.ProcessDataGridViewKey(e);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            return base.CreateParams;
        }
    }
}
