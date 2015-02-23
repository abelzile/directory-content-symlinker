using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;


namespace DirectoryContentSymlinker
{
    public class MatchFinder
    {
        const int DefaultFirstChunkSize = 1024 * 8;
        const int DefaultBufferChunkSize = 1024 * 1024;

        readonly FileFinder _target;
        readonly FileFinder _destination;
        readonly ConcurrentDictionary<string, byte[]> _targetFileHashDict = new ConcurrentDictionary<string, byte[]>();
        readonly ConcurrentDictionary<string, byte[]> _destinationFileHashDict = new ConcurrentDictionary<string, byte[]>();
        ConcurrentBag<FileMatch> _matches;

        public MatchFinder(FileFinder target, FileFinder destination)
        {
            _target = target;
            _destination = destination;
        }

        public IList<FileMatch> Matches
        {
            get { return _matches.ToList().AsReadOnly(); }
        }

        public void Find()
        {
            // ReSharper disable AccessToForEachVariableInClosure
            _targetFileHashDict.Clear();
            _destinationFileHashDict.Clear();
            _matches = new ConcurrentBag<FileMatch>();

            foreach (var destinationFilePair in _destination.Files)
            {
                Parallel.ForEach(
                    _target.Files,
                    targetFilePair =>
                    {
                        if (destinationFilePair.Value != targetFilePair.Value) return;
                            
                        byte[] destinationChunk = FirstChunk(destinationFilePair.Key, destinationFilePair.Value);
                        byte[] targetChunk = FirstChunk(targetFilePair.Key, targetFilePair.Value);

                        if (!ArraysEqual(destinationChunk, targetChunk)) return;

                        using (SHA512 shaM = SHA512Managed.Create())
                        {
                            byte[] destinationHash;
                            if (!_destinationFileHashDict.TryGetValue(destinationFilePair.Key, out destinationHash))
                            {
                                destinationHash = ComputeHash(destinationFilePair.Key, shaM);
                                _destinationFileHashDict.TryAdd(destinationFilePair.Key, destinationHash);
                            }

                            byte[] targetHash;
                            if (!_targetFileHashDict.TryGetValue(targetFilePair.Key, out targetHash))
                            {
                                targetHash = ComputeHash(targetFilePair.Key, shaM);
                                _targetFileHashDict.TryAdd(targetFilePair.Key, targetHash);
                            }

                            if (!ArraysEqual(destinationHash, targetHash)) return;
                        }

                        var fileMatch = new FileMatch(targetFilePair.Key, destinationFilePair.Key);
                        _matches.Add(fileMatch);
                    });
            }
            // ReSharper restore AccessToForEachVariableInClosure
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