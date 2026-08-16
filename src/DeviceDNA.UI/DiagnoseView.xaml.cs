//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

using System.Windows.Controls;

namespace DeviceDNA.UI;

// The Diagnose page (REQUIREMENTS.md section 7, item 3), extracted from the former DiagnoseWindow
// into a UserControl swapped into MainWindow's content area in place of DashboardView. DataContext
// is MainViewModel (not DiagnoseViewModel directly) so this can bind both GoBackToDashboardCommand
// and MainViewModel.DiagnoseViewModel's own properties.
public partial class DiagnoseView : UserControl
{
    public DiagnoseView()
    {
        InitializeComponent();
    }
}

//*Built with assistance from __Claude Code__ by Anthropic.*
