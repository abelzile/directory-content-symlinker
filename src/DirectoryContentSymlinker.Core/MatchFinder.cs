using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.HashFunction;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DirectoryContentSymlinker.Core.Utils;


namespace DirectoryContentSymlinker.Core
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
            _targetFileHashDict.Clear();
            _destinationFileHashDict.Clear();
            _matches = new ConcurrentBag<FileMatch>();

            // ReSharper disable AccessToForEachVariableInClosure
            foreach (var destinationFilePair in _destination.Files)
            {
                Parallel.ForEach(
                    _target.Files,
                    targetFilePair =>
                    {
                        if (destinationFilePair.Value != targetFilePair.Value) return;
                            
                        byte[] destinationChunk = FirstChunk(destinationFilePair.Key, destinationFilePair.Value);
                        byte[] targetChunk = FirstChunk(targetFilePair.Key, targetFilePair.Value);

                        if (!ArrayUtil.ArraysEqual(destinationChunk, targetChunk)) return;

                        IHashFunction hash = null;

                        byte[] destinationHash;
                        if (!_destinationFileHashDict.TryGetValue(destinationFilePair.Key, out destinationHash))
                        {
                            hash = CreateMurmurHash3();

                            destinationHash = ComputeHash(destinationFilePair.Key, hash);
                            _destinationFileHashDict.TryAdd(destinationFilePair.Key, destinationHash);
                        }

                        byte[] targetHash;
                        if (!_targetFileHashDict.TryGetValue(targetFilePair.Key, out targetHash))
                        {
                            if (hash == null)
                                hash = CreateMurmurHash3();

                            targetHash = ComputeHash(targetFilePair.Key, hash);
                            _targetFileHashDict.TryAdd(targetFilePair.Key, targetHash);
                        }

                        if (!ArrayUtil.ArraysEqual(destinationHash, targetHash)) return;

                        _matches.Add(new FileMatch(targetFilePair.Key, destinationFilePair.Key));
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

        static MurmurHash3 CreateMurmurHash3()
        {
            return new MurmurHash3(128, (uint)ThreadSafeRandom.Next());
        }

        static byte[] ComputeHash(string destinationFilePath, IHashFunction hashFunction)
        {
            using (var fileStream = new BufferedStream(File.OpenRead(destinationFilePath), DefaultBufferChunkSize))
            {
                return hashFunction.ComputeHash(fileStream);
            }
        }
    }
}
