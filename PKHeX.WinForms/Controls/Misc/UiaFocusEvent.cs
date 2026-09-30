using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PKHeX.WinForms.Controls;

/// <summary>
/// Raises the UI Automation focus-changed event for a control that WinForms doesn't raise it for.
/// </summary>
/// <remarks>
/// Labels and picture boxes expose a native UIA provider, so NVDA reads them through UIA rather than MSAA,
/// but WinForms never raises the UIA focus event for them (it only does so for its own focusable controls).
/// Without the event, focusing one is silent, and the screen reader keeps treating the previously focused control as focused.
/// </remarks>
internal static class UiaFocusEvent
{
    private const int UIA_AutomationFocusChangedEventId = 20005;
    private static readonly Guid IID_IRawElementProviderSimple = new("d6dd68d1-86fd-4332-8666-9abedea2d24c");

    public static void Raise(Control control)
    {
        if (!control.IsHandleCreated || !UiaClientsAreListening())
            return;

        // Raise it on the control's own provider; UIA drops focus events raised on the window's host provider.
        var unknown = Marshal.GetIUnknownForObject(control.AccessibilityObject);
        try
        {
            if (Marshal.QueryInterface(unknown, in IID_IRawElementProviderSimple, out var provider) != 0)
                return;
            UiaRaiseAutomationEvent(provider, UIA_AutomationFocusChangedEventId);
            Marshal.Release(provider);
        }
        finally
        {
            Marshal.Release(unknown);
        }
    }

    #pragma warning disable SYSLIB1054
    [DllImport("UIAutomationCore.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UiaClientsAreListening();

    [DllImport("UIAutomationCore.dll")]
    private static extern int UiaRaiseAutomationEvent(nint provider, int id);
    #pragma warning restore SYSLIB1054
}
