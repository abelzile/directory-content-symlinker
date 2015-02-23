using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Mono.Options;


namespace DirectoryContentSymlinker
{
    class Program
    {
        static void Main(string[] args)
        {
            bool showHelp = false;
            string targetPath = "";
            string symlinkPath = "";
            string searchPattern = "";

            var p = new OptionSet {
                {
                    "h|help",  
                    "Show this message and exit.", 
                    v => showHelp = v != null
                },
                {
                    "t|target=", 
                    "The {PATH} to the files symlinks will refer to.", 
                    v => targetPath = v 
                },
                {
                    "d|destination=", 
                    "The {PATH} to the files symlinks will be created for.", 
                    v => symlinkPath = v 
                },
                {
                    "s|searchPattern=", 
                    "Pipe (|) delimited list of search patterns used to find files. Each pattern is passed directly to Directory.GetFiles so see docs for that.", 
                    v => searchPattern = v
                }
            };

            List<string> extra;
            try
            {
                extra = p.Parse(args);

                if (showHelp)
                {
                    p.WriteOptionDescriptions(Console.Out);
                    return;
                }

                ValidatePaths(targetPath, symlinkPath);

                if (string.IsNullOrWhiteSpace(searchPattern))
                {
                    searchPattern = "";
                }

                Console.WriteLine("Starting...");
                Console.WriteLine("Finding files in target directory...");

                var targetFinder = new FileFinder(targetPath, searchPattern);
                targetFinder.Find();

                Console.WriteLine("{0} files found in target directory.", targetFinder.Files.Count);
                Console.WriteLine("Finding files in destination directory...");

                var destinationFinder = new FileFinder(symlinkPath, searchPattern);
                destinationFinder.Find();

                Console.WriteLine("{0} files found in destination directory.", destinationFinder.Files.Count);
                Console.WriteLine("Finding matching files. This can take some time...");

                var matchFinder = new MatchFinder(targetFinder, destinationFinder);
                matchFinder.Find();

                int matchCount = matchFinder.Matches.Count;

                Console.WriteLine("{0} matching files found.", matchCount);

                if (matchCount > 0)
                {
                    Console.WriteLine("Making symbolic links...");

                    Parallel.ForEach(
                        matchFinder.Matches,
                        match =>
                        {
                            Console.WriteLine("Linking " + ShortenPath(match.TargetPath));

                            var linker = new FileMatchSymlinker(match);
                            linker.Create();
                        });
                }

                Console.WriteLine("Done. Exiting.");
            }
            catch (OptionException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `--help' for more information.");
            }

            if (Debugger.IsAttached)
            {
                Console.WriteLine("Click any key to exit.");
                Console.ReadKey();
            }
        }

        static string ShortenPath(string path)
        {
            int first = path.IndexOf(@"\", StringComparison.OrdinalIgnoreCase);
            int last = path.LastIndexOf(@"\", StringComparison.OrdinalIgnoreCase);

            if (first == -1 || last == -1 || first == last) return path;

            string part1 = path.Substring(0, first + 1);
            string part2 = path.Substring(last + 1);

            return part1 + @"...\" + part2;
        }

        static void ValidatePaths(string targetPath, string symlinkPath)
        {
            if (string.IsNullOrWhiteSpace(targetPath)) 
                throw new OptionException("Missing target path.", "-t");

            if (!Directory.Exists(targetPath)) 
                throw new OptionException("Target path does not exist or is invalid.", "-t");

            if (string.IsNullOrWhiteSpace(symlinkPath)) 
                throw new OptionException("Missing symlink destination path.", "-d");

            if (!Directory.Exists(symlinkPath)) 
                throw new OptionException("Symlink destination path does not exist or is invalid.", "-d");

            if (string.Compare(targetPath, symlinkPath, StringComparison.OrdinalIgnoreCase) == 0) 
                throw new OptionException("Target path and destination path cannot be the same.", "-d");
        }
    }
}
