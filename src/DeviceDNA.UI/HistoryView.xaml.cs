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

// History & Changes page (REQUIREMENTS.md section 7 items 4-5), converted from a separate popup
// Window (formerly HistoryWindow) to a UserControl swapped into MainWindow's content area — same
// pattern as DiagnoseView. DataContext is MainViewModel (not HistoryWindowViewModel directly), so
// this binds through MainViewModel.HistoryViewModel's History/Changes sub-view-models.
public partial class HistoryView : UserControl
{
    public HistoryView()
    {
        InitializeComponent();
    }
}

//*Built with assistance from __Claude Code__ by Anthropic.*
