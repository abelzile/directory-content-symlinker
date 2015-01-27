using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;


namespace DirectoryContentSymlinker
{
    public class MatchFinder
    {
        readonly FileFinder _target;
        readonly FileFinder _destination;
        readonly Dictionary<string, byte[]> _targetFileHashDict = new Dictionary<string, byte[]>();
        readonly Dictionary<string, byte[]> _destinationFileHashDict = new Dictionary<string, byte[]>();
        readonly List<FileMatch> _matches = new List<FileMatch>();

        public MatchFinder(FileFinder target, FileFinder destination)
        {
            _target = target;
            _destination = destination;
        }

        public IList<FileMatch> Matches
        {
            get { return _matches.AsReadOnly(); }
        }

        public void Find()
        {
            _matches.Clear();

            using (SHA512 shaM = SHA512Managed.Create())
            {
                foreach (var destinationFilePair in _destination.Files)
                {
                    foreach (var targetFilePair in _target.Files)
                    {
                        if (destinationFilePair.Value != targetFilePair.Value) continue;

                        byte[] destinationHash;
                        if (!_destinationFileHashDict.TryGetValue(destinationFilePair.Key, out destinationHash))
                        {
                            destinationHash = ComputeHash(destinationFilePair.Key, shaM);
                            _destinationFileHashDict.Add(destinationFilePair.Key, destinationHash);
                        }

                        byte[] targetHash;
                        if (!_targetFileHashDict.TryGetValue(targetFilePair.Key, out targetHash))
                        {
                            targetHash = ComputeHash(targetFilePair.Key, shaM);
                            _targetFileHashDict.Add(targetFilePair.Key, targetHash);
                        }

                        if (destinationHash.LongLength == targetHash.LongLength && destinationHash.SequenceEqual(targetHash))
                        {
                            var fileMatch = new FileMatch(targetFilePair.Key, destinationFilePair.Key);
                            _matches.Add(fileMatch);

                            break;
                        }
                    }
                }
            }
        }

        static byte[] ComputeHash(string destinationFilePath, HashAlgorithm shaM)
        {
            using (var fileStream = new BufferedStream(File.OpenRead(destinationFilePath), 1024 * 1024))
            {
                return shaM.ComputeHash(fileStream);
            }
        }
    }
}