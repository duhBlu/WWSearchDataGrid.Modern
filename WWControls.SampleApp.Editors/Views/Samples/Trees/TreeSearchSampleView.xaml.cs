using System;
using System.Windows;
using System.Windows.Controls;

namespace WWControls.SampleApp.Editors.Views.Samples.Trees
{
    public partial class TreeSearchSampleView : UserControl
    {
        public TreeSearchSampleView() => InitializeComponent();

        private TreeSearchSampleViewModel ViewModel => (TreeSearchSampleViewModel)DataContext;

        // BringItemIntoView is an imperative method on the control (there is no command for it), so
        // revealing the picked node — expand its ancestors and scroll it into view, without selecting
        // it — is a single line of glue. Showing that call is the point of this handler.
        private void OnReveal(object sender, RoutedEventArgs e)
        {
            if (ViewModel.JumpTarget != null)
                Tree.BringItemIntoView(ViewModel.JumpTarget);
        }

        // SelectItems replaces the whole selection at once (multi modes). It is fed the same FilterText
        // the find bar drives, so "Select all matches" selects exactly the set being cycled.
        private void OnSelectMatches(object sender, RoutedEventArgs e)
        {
            Tree.SelectItems(ViewModel.MatchesFor(Tree.FilterText));
        }

        private void OnClearSelection(object sender, RoutedEventArgs e)
        {
            Tree.SelectItems(Array.Empty<object>());
        }
    }
}
