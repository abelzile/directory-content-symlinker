namespace DirectoryContentSymlinker
{
    public class FileMatch
    {
        public string TargetPath { get; private set; }
        public string LinkPath { get; private set; }

        public FileMatch(string targetPath, string linkPath)
        {
            TargetPath = targetPath;
            LinkPath = linkPath;
        }
    }
}