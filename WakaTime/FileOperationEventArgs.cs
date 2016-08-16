using System;

namespace WakaTime
{
    public class FileOperationEventArgs : EventArgs
    {
        public string FilePath { get; }
        public bool IsWrite { get; }

        public FileOperationEventArgs(string filePath, bool isWrite)
        {
            FilePath = filePath;
            IsWrite = isWrite;
        }
    }
}
