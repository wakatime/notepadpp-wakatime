using Kbg.NppPluginNET.PluginInfrastructure;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using WakaTime.Forms;

namespace WakaTime
{
    class WakaTimeNppPlugin : WakaTimeIdePlugin<UnmanagedExports>, IWin32Window
    {
        private SettingsForm _settingsForm;
        private ApiKeyForm _apiKeyForm;
        private DownloadProgressForm _downloadProgressForm;

        public IntPtr Handle
        {
            get
            {
                return PluginBase.nppData._nppHandle;
            }
        }

        public WakaTimeNppPlugin(UnmanagedExports editor) : base(editor)
        {
        }

        public override void BindEditorEvents()
        {
            WakaTimePackage.FileChanged += OnFileChanged;
            WakaTimePackage.FileOpened += OnFileOpened;
        }

        private void OnFileOpened(object sender, FileOperationEventArgs args)
        {
            OnDocumentOpened(args.FilePath);
        }

        private void OnFileChanged(object sender, FileOperationEventArgs args)
        {
            OnDocumentChanged(args.FilePath);
        }

        public override void Dispose(bool disposing)
        {
            if (_settingsForm != null && !_settingsForm.IsDisposed)
                _settingsForm.Dispose();

            if (_apiKeyForm != null && !_apiKeyForm.IsDisposed)
                _apiKeyForm.Dispose();

            if (_downloadProgressForm != null && !_downloadProgressForm.IsDisposed)
                _downloadProgressForm.Dispose();
        }

        public override string GetActiveSolutionPath()
        {
            return null;
        }

        public override EditorInfo GetEditorInfo()
        {
            var ver = (int)Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_GETNPPVERSION, 0, 0);
            return new EditorInfo
            {
                Name = "notepadpp",
                Version = new Version(ver, 0),
                PluginKey = Constants.PluginKey,
                PluginName = Constants.PluginName,
                PluginVersion = Constants.PluginVersion
            };
        }

        public override ILogService GetLogger()
        {
            return new LoggerNpp();
        }

        public override IDownloadProgressReporter GetReporter()
        {
            return new DownloadProgressForm(this);
        }

        public override void PromptApiKey()
        {
            if (_apiKeyForm == null)
                _apiKeyForm = new ApiKeyForm();

            _apiKeyForm.ShowDialog();
        }

        public override void SettingsPopup()
        {
            if (_settingsForm == null)
                _settingsForm = new SettingsForm();

            _settingsForm.ShowDialog();
        }
    }
}
