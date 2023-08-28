﻿using System.Threading;
using System.Windows.Input;

namespace VisualGGPK2
{
    public partial class BackgroundDialog
    {
        public int progress = 0;
        public string ProgressText;
        public BackgroundDialog()
        {
            InitializeComponent();
        }

        public virtual void NextProgress()
        {
            Interlocked.Increment(ref progress);
            Dispatcher.BeginInvoke(() => { MessageTextBlock.Text = string.Format(ProgressText, progress); });
        }

        protected virtual void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
        }

        public new virtual void Close()
        {
            try {
                Closing -= OnClosing;
                Dispatcher.Invoke(base.Close);
            }
            catch
            {
                // ignored
            }
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }
    }
}