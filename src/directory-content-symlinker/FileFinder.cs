using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;


namespace DirectoryContentSymlinker
{
    public class FileFinder
    {
        readonly string _directory;
        readonly string[] _searchPatterns;
        Dictionary<string, long> _files;
        
        public FileFinder(string directory, string searchPattern)
        {
            _directory = directory;
            _searchPatterns = string.IsNullOrWhiteSpace(searchPattern)
                ? new string[] { }
                : searchPattern.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public ReadOnlyDictionary<string, long> Files
        {
            get { return new ReadOnlyDictionary<string, long>(_files); }
        }

        public void Find()
        {
            _files = new Dictionary<string, long>();

            var targetFiles = FindFiles();

            foreach (string targetFile in targetFiles)
            {
                var fileInfo = new FileInfo(targetFile);

                if (fileInfo.Attributes.HasFlag(FileAttributes.ReparsePoint)) continue;

                _files.Add(targetFile, fileInfo.Length);
            }
        }

        IEnumerable<string> FindFiles()
        {
            switch (_searchPatterns.Length)
            {
                case 0:
                    return Directory.EnumerateFiles(_directory, "*.*", SearchOption.AllDirectories);
                case 1:
                    return Directory.EnumerateFiles(_directory, _searchPatterns[0], SearchOption.AllDirectories);
                default:
                    return _searchPatterns.SelectMany(g => Directory.EnumerateFiles(_directory, g, SearchOption.AllDirectories));
            }
        }
    }
}