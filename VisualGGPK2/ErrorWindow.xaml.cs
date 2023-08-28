using System;
using System.Diagnostics;
using System.Windows;
namespace VisualGGPK2
{
    public sealed partial class ErrorWindow
    {
        public ErrorWindow()
        {
            InitializeComponent();
            Title += MainWindow.Version;
        }

        public void ShowError(Exception e)
        {
            ErrorBox.Text = e.ToString();
            ButtonCopy.IsEnabled = true;
            ButtonResume.IsEnabled = true;
            ButtonStop.IsEnabled = true;
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
        }

        private void OnCopyClick(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(ErrorBox.Text);
        }

        private void OnGitHubClick(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/vmv/VisualGGPK2UI");
        }

        private void OnResumeClick(object sender, RoutedEventArgs e)
        {
            Closing -= OnClosing;
            DialogResult = true;
            Close(); // This line will never reached
        }

        private void OnStopClick(object sender, RoutedEventArgs e)
        {
            Closing -= OnClosing;
            DialogResult = false;
            Close(); // This line will never reached
        }
    }
}