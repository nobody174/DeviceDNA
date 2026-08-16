//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

using System.Diagnostics;
using System.IO;
using System.Windows;
using DeviceDNA.Application;
using DeviceDNA.UI.Presentation;
using Microsoft.Win32;

namespace DeviceDNA.UI;

// Main command-deck window. Sets its DataContext to MainViewModel, which triggers a real
// hardware scan (via the Application layer) on construction.
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var viewModel = new MainViewModel();
        viewModel.ExportRequested += (_, _) => ExportDeviceJson(viewModel);
        viewModel.OpenVendorUrlRequested += (_, url) => OpenVendorUrl(url);
        viewModel.OpenBenchmarkUrlRequested += (_, url) => OpenVendorUrl(url);
        DataContext = viewModel;
    }

    // Opens a vendor product/support page in the user's default browser. This app never fetches
    // this URL itself — only the user's own click, via their own browser, ever reaches the vendor's
    // site (REQUIREMENTS.md section 10, clarified 2026-08-15).
    private void OpenVendorUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            // Real, expected failure mode: no default browser registered, or the URL is malformed.
            MessageBox.Show(this, $"Could not open {url}:\n{ex.Message}", "DeviceDNA", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Export is always an explicit user action (REQUIREMENTS.md section 10) — triggered only by
    // the "Export" button, never automatically, and the user picks the destination themselves.
    private void ExportDeviceJson(MainViewModel viewModel)
    {
        if (viewModel.Device == null)
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            FileName = $"DeviceDNA-{viewModel.Hostname}-{DateTime.Now:yyyyMMdd-HHmmss}.json",
        };

        if (dialog.ShowDialog(this) == true)
        {
            try
            {
                var json = DeviceExportService.ToJson(viewModel.Device);
                File.WriteAllText(dialog.FileName, json);
                MessageBox.Show(this, $"Exported to {dialog.FileName}", "DeviceDNA", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Real, expected failure modes for writing to a user-chosen path: disk full,
                // permission denied, file locked by another process, drive removed. Never let an
                // export failure take down the whole app — the user still has their scan on screen.
                MessageBox.Show(this, $"Could not export to {dialog.FileName}:\n{ex.Message}", "DeviceDNA", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

//*Built with assistance from __Claude Code__ by Anthropic.*
