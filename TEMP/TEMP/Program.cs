using System;
using System.Collections.Generic;
using System.IO;

namespace SearchTest
{
    public class Program
    {
        public static DirectoryInfo GetRootDirectory()
        {
            DirectoryInfo rootDirectory;
            while (true)
            {
                Console.Clear();
                Console.WriteLine("Please enter root directory:");
                var currentDirectory = Console.ReadLine();

                rootDirectory = TryGetDirectory(currentDirectory);
                if (rootDirectory == null)
                {
                    continue;
                }
                break;
            }

            return rootDirectory;
        }

        public static DirectoryInfo TryGetDirectory(string path)
        {
            try
            {
                var directory = new DirectoryInfo(path);
                return directory;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"*** ERROR: " + ex.Message);
                Console.ResetColor();
                Console.WriteLine(" * Press any key to continue...");
                Console.ReadKey();
                return null;
            }
        }

        public static void SearchText(DirectoryInfo rootDirectory)
        {
            while (true)
            {
                System.Console.WriteLine("Enter sarch text. (nothing to exit)");

                // 1. Get search text
                var searchText = System.Console.ReadLine();

                if (string.IsNullOrEmpty(searchText))
                {
                    break;
                }

                // 2. Get current folder
                //ar desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop); 

                // 3. Search text in folder
                var searchConfig = new SearchEngine.SearchConfiguration()
                {
                    IsSavePreview = true,
                    IsSearchInContext = true
                };
                var searchResult = SearchEngine.SearchTextInDirectory(rootDirectory, searchText, searchConfig);
                SearchEngine.PrintSearchResult(searchResult);

                System.Console.WriteLine(" * Press any key to continue...");
                System.Console.ReadKey();
                System.Console.Clear();
            }
        }

        public static void Main(string[] args)
        {
            DirectoryInfo rootDirectory = null;
            if (args.Length > 0)
            {
                var currentDirectory = args[0];
                rootDirectory = TryGetDirectory(currentDirectory);
            }

            rootDirectory = rootDirectory ?? GetRootDirectory();
            Console.WriteLine($" * Root directory: {rootDirectory}");

            while (true)
            {
                SearchText(rootDirectory);

                Console.WriteLine("Do you want change directory? (ESC, n, N) = no, else = yes");
                var key = Console.ReadKey();
                switch (key.Key)
                {
                    case ConsoleKey.Escape:
                    case ConsoleKey.N:
                    {
                        return;
                    }
                }

                rootDirectory = GetRootDirectory();
            }
        }
    }

}
