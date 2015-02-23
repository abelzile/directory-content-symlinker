using System;
using System.IO;
using Microsoft.VisualBasic.FileIO;


namespace DirectoryContentSymlinker.Core
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

            var commandRunner = new CommandRunner(string.Format(MklinkCmdFormat, _fileMatch.LinkPath, _fileMatch.TargetPath));
            commandRunner.Run();

            if (commandRunner.ExitCode != 0)
                throw new Exception(commandRunner.StandardError);
            
            FileSystem.DeleteFile(tempLinkFileName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
        }
    }
}