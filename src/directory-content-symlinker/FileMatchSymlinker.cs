using System.Diagnostics;
using System.IO;
using Microsoft.VisualBasic.FileIO;


namespace DirectoryContentSymlinker
{
    public class FileMatchSymlinker
    {
        const string MklinkCmdFormat = @"/c mklink ""{0}"" ""{1}""";
            
        readonly FileMatch _fileMatch;

        public FileMatchSymlinker(FileMatch fileMatch)
        {
            _fileMatch = fileMatch;
        }

        public void Create()
        {
            string tempLinkFileName = _fileMatch.LinkPath + "~";

            File.Move(_fileMatch.LinkPath, tempLinkFileName);

            string cmd = string.Format(MklinkCmdFormat, _fileMatch.LinkPath, _fileMatch.TargetPath);

            var process = new ProcessStartInfo("cmd.exe", cmd)
            {
                CreateNoWindow = true,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            var p = Process.Start(process);
            p.WaitForExit();
            int exitCode = p.ExitCode;
            
            FileSystem.DeleteFile(tempLinkFileName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
        }
    }
}