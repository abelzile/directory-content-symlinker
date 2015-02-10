using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;


namespace DirectoryContentSymlinker
{
    public class MatchFinder
    {
        const int DefaultFirstChunkSize = 1024 * 8;
        const int DefaultBufferChunkSize = 1024 * 1024;

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

                        byte[] destinationChunk = FirstChunk(destinationFilePair.Key, destinationFilePair.Value);
                        byte[] targetChunk = FirstChunk(targetFilePair.Key, targetFilePair.Value);

                        if (!ArraysEqual(destinationChunk, targetChunk)) continue;

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

                        if (!ArraysEqual(destinationHash, targetHash)) continue;

                        var fileMatch = new FileMatch(targetFilePair.Key, destinationFilePair.Key);
                        _matches.Add(fileMatch);

                        break;
                    }
                }
            }
        }

        static byte[] FirstChunk(string destinationFilePath, long fileSize)
        {
            long firstChunkSize = Math.Min(DefaultFirstChunkSize, fileSize);

            byte[] firstChunk;
            using (var stream = File.OpenRead(destinationFilePath))
            {
                firstChunk = new byte[firstChunkSize];
                int read = stream.Read(firstChunk, 0, firstChunk.Length);
            }

            return firstChunk;
        }

        static byte[] ComputeHash(string destinationFilePath, HashAlgorithm hashAlgorithm)
        {
            using (var fileStream = new BufferedStream(File.OpenRead(destinationFilePath), DefaultBufferChunkSize))
            {
                return hashAlgorithm.ComputeHash(fileStream);
            }
        }

        static bool ArraysEqual(byte[] a1, byte[] a2)
        {
            if (a1.Length == a2.Length)
            {
                for (int i = 0; i < a1.Length; i++)
                {
                    if (a1[i] != a2[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

    }
}