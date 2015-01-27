using System;
using System.Collections.Generic;
using System.IO;
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

            var p = new OptionSet() {
                {
                    "h|help",  
                    "Show this message and exit.", 
                    v => showHelp = v != null
                },
                {
                    "t|target=", 
                    "The {PATH} to the files symlinks will refer to.", 
                    v =>
                    {
                        if (string.IsNullOrWhiteSpace(v))
                        {
                            throw new OptionException("Missing target path.", "-t");
                        }
                        targetPath = v;
                    } 
                },
                {
                    "d|destination=", 
                    "The {PATH} to the files symlinks will be created for.", 
                    v =>
                    {
                        if (string.IsNullOrWhiteSpace(v))
                        {
                            throw new OptionException("Missing symlink creation path.", "-d");
                        }
                        symlinkPath = v;
                    } 
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

                var matchFinder = new MatchFinder(targetFinder, destinationFinder);
                matchFinder.Find();

                int matchCount = matchFinder.Matches.Count;

                Console.WriteLine("{0} matching files found.", matchCount);

                if (matchCount > 0)
                {
                    Console.WriteLine("Making symbolic links...");

                    foreach (var fileMatch in matchFinder.Matches)
                    {
                        var linker = new FileMatchSymlinker(fileMatch);
                        linker.Create();
                    }
                }

                Console.WriteLine("Done. Exiting.");
            }
            catch (OptionException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `--help' for more information.");
                Console.ReadKey();
            }
        }

        static void ValidatePaths(string targetPath, string symlinkPath)
        {
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                throw new OptionException("Missing target path.", "-t");
            }

            if (!Directory.Exists(targetPath))
            {
                throw new OptionException("Target path does not exist or is invalid.", "-t");
            }

            if (string.IsNullOrWhiteSpace(symlinkPath))
            {
                throw new OptionException("Missing symlink destination path.", "-d");
            }

            if (!Directory.Exists(symlinkPath))
            {
                throw new OptionException("Symlink destination path does not exist or is invalid.", "-d");
            }

            if (string.Compare(targetPath, symlinkPath, StringComparison.OrdinalIgnoreCase) == 0)
            {
                throw new OptionException("Target path and destination path cannot be the same.", "-d");
            }
        }
    }
}
