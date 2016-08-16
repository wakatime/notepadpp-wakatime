using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WakaTime
{
    class WakaTimeNppPlugin : WakaTimeIdePlugin<LoggerNpp>
    {
        public WakaTimeNppPlugin(LoggerNpp editor) : base(editor)
        {
        }

        public override void BindEditorEvents()
        {
            throw new NotImplementedException();
        }

        public override void Dispose(bool disposing)
        {
            throw new NotImplementedException();
        }

        public override string GetActiveSolutionPath()
        {
            throw new NotImplementedException();
        }

        public override EditorInfo GetEditorInfo()
        {
            throw new NotImplementedException();
        }

        public override ILogService GetLogger()
        {
            throw new NotImplementedException();
        }

        public override IDownloadProgressReporter GetReporter()
        {
            throw new NotImplementedException();
        }

        public override void PromptApiKey()
        {
            throw new NotImplementedException();
        }

        public override void SettingsPopup()
        {
            throw new NotImplementedException();
        }
    }
}
