using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PKHeX.WinForms.Controls;

/// <summary>
/// Makes a set of cells (slots, tiles) a single tab stop that is navigated with the arrow keys.
/// </summary>
/// <remarks>Tabbing through every cell one at a time made the controls after a grid slow to reach from the keyboard.</remarks>
public static class GridNavigation
{
    /// <summary>
    /// Joins a cell, already added to <paramref name="cells"/>, to the grid's navigation.
    /// </summary>
    /// <remarks>
    /// The last focused cell is the grid's tab stop. Left/Right step through <paramref name="cells"/> in order;
    /// Up/Down move to the nearest cell in the adjacent row on screen, so rows may differ in length or live in separate panels.
    /// </remarks>
    public static void Join<T>(T cell, IReadOnlyList<T> cells) where T : Control
    {
        cell.TabStop = cells[0] == cell;
        cell.Enter += (_, _) => { foreach (var c in cells) c.TabStop = c == cell; };
        cell.PreviewKeyDown += (_, e) => e.IsInputKey |= e.KeyCode is Keys.Left or Keys.Right or Keys.Up or Keys.Down;
        cell.KeyDown += (_, e) =>
        {
            if (e.Modifiers != 0 || e.KeyCode is not (Keys.Left or Keys.Right or Keys.Up or Keys.Down))
                return;
            GetNext(cell, cells, e.KeyCode)?.Focus();
            e.Handled = true;
        };
    }

    private static T? GetNext<T>(T cell, IReadOnlyList<T> cells, Keys key) where T : Control
    {
        if (key is Keys.Left or Keys.Right)
        {
            var index = IndexOf(cell, cells) + (key == Keys.Right ? 1 : -1);
            return (uint)index < (uint)cells.Count && cells[index].CanFocus ? cells[index] : null;
        }

        var origin = cell.PointToScreen(Point.Empty);
        var sign = key == Keys.Down ? 1 : -1;
        return cells.Where(c => c.CanFocus && Offset(c).Y * sign > 0)
            .MinBy(c => (Offset(c).Y * sign, int.Abs(Offset(c).X)));

        Point Offset(T c) => c.PointToScreen(Point.Empty) - (Size)origin;
    }

    private static int IndexOf<T>(T cell, IReadOnlyList<T> cells) where T : Control
    {
        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i] == cell)
                return i;
        }
        return -1;
    }
}
