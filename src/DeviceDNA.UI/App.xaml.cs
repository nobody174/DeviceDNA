//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

using System.Configuration;
using System.Data;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace DeviceDNA.UI;

// Interaction logic for App.xaml
public partial class App : System.Windows.Application
{
    public App()
    {
        // Use invariant number formatting (e.g. "16.5 GB" not "16,5 GB") regardless of the
        // user's OS locale, so DNA summaries and field displays are consistent and unambiguous.
        var culture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        // Last-resort safety net: an unhandled exception anywhere on the UI thread (e.g. a
        // malformed row surfacing from SQLite history, a binding-triggered command failure)
        // should show an error rather than silently terminate the whole app and lose the
        // user's current scan/session.
        DispatcherUnhandledException += OnDispatcherUnhandledException;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            $"DeviceDNA hit an unexpected error and may be unstable:\n\n{e.Exception.Message}\n\nYou may want to restart the app.",
            "DeviceDNA — Unexpected Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }
}

//*Built with assistance from __Claude Code__ by Anthropic.*
