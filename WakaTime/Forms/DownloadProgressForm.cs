using System;
using System.ComponentModel;
using System.Net;
using System.Windows.Forms;

namespace WakaTime.Forms
{
    public partial class DownloadProgressForm : Form, IDownloadProgressReporter
    {
        private IWin32Window _owner;

        public DownloadProgressForm(IWin32Window owner)
        {
            _owner = owner;
            InitializeComponent();
        }

        private const int WS_SYSMENU = 0x80000;
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.Style &= ~WS_SYSMENU;
                return cp;
            }
        }

        protected override void OnActivated(EventArgs e)
        {
            if (this.Modal == false)
            {
                this.CenterToParent();
                Application.DoEvents();
            }
            base.OnActivated(e);
        }


        public void Close(AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message);
            }

            this.InvokeIfRequired(() => {
                Close();
            });
        }

        public void Report(DownloadProgressChangedEventArgs e)
        {
            Application.DoEvents();
            progressBar1.InvokeIfRequired(() => {
                progressBar1.Value = e.ProgressPercentage;
            });
        }

        public void Show(string message = "")
        {
            labText.InvokeIfRequired(() =>
            {
                labText.Text = message;
            });

            this.InvokeIfRequired(() =>
            {
                Show(_owner);
            });
        }
    }
}
