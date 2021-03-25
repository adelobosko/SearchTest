using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SearchTest
{
    public class SearchEngine
    {
        public static SearchConfiguration DefaultConfiguration = new SearchConfiguration();

        private static void FindAllFilesByNameInCurrentDirectory(System.IO.DirectoryInfo currentDirectory, SearchResult searchResult, SearchConfiguration config)
        {
            Console.SetCursorPosition(0, 0);
            Console.WriteLine(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, 0);
            Console.WriteLine($"FindAllFilesByName: {currentDirectory.Name}");

            var files = currentDirectory.GetFiles();
            for (var i = 0; i < files.Length; i++)
            {
                var file = files[i];
                if (searchResult.FoundCounter >= config.Take)
                {
                    break;
                }

                if (config.IsRegisterSensitive && file.Name.Contains(searchResult.SearchText)
                    || file.Name.ToLower().Contains(searchResult.SearchText.ToLower()))
                {
                    searchResult.InDirectoryByNames.AddFile(file);
                }

                Console.SetCursorPosition(0, 1);
                Console.WriteLine(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, 1);
                Console.Write($"\t{(i + 1) * 100.0 / files.Length:00.00}%");
            }
        }

        private static void FindAllDirectoriesByNameInCurrentDirectory(System.IO.DirectoryInfo currentDirectory, SearchResult searchResult, SearchConfiguration config)
        {
            Console.SetCursorPosition(0, 0);
            Console.WriteLine(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, 0);
            Console.WriteLine($"FindAllDirectoriesByName: {currentDirectory.Name}");

            var directories = currentDirectory.GetDirectories();
            for (var i = 0; i < directories.Length; i++)
            {
                var directory = directories[i];
                if (searchResult.FoundCounter >= config.Take)
                {
                    break;
                }

                if (config.IsRegisterSensitive && directory.Name.Contains(searchResult.SearchText)
                    || directory.Name.ToLower().Contains(searchResult.SearchText.ToLower()))
                {
                    searchResult.InDirectoryByNames.AddDirectory(directory);
                }

                Console.SetCursorPosition(0, 1);
                Console.WriteLine(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, 1);
                Console.Write($"\t{(i + 1) * 100.0 / directories.Length:00.00}%");
            }
        }


        private static void FindAllInCurrentFile(System.IO.FileInfo currentFile, in string searchText, SearchResult searchResult, SearchConfiguration config)
        {
            var lineNumber = 0;
            using (var streamReader = new System.IO.StreamReader(currentFile.FullName))
            {
                Stream baseStream = streamReader.BaseStream;
                long length = baseStream.Length;

                while (true)
                {
                    if (searchResult.FoundCounter >= config.Take)
                    {
                        break;
                    }

                    var line = streamReader.ReadLine();
                    if (line == null)
                    {
                        break;
                    }


                    var startIndex = 0;
                    while (true)
                    {
                        var index = config.IsRegisterSensitive
                            ? line.IndexOf(searchText, startIndex, StringComparison.Ordinal)
                            : line.ToLower().IndexOf(searchText.ToLower(), startIndex, StringComparison.Ordinal);

                        if (index < 0 || searchResult.FoundCounter >= config.Take)
                        {
                            break;
                        }

                        startIndex = index + searchText.Length;

                        var previewText = config.IsSavePreview
                            ? new string(line.Skip(index - config.PreviewSymbolsCount).Take(searchText.Length + config.PreviewSymbolsCount * 2).ToArray())
                            : string.Empty;

                        searchResult.AddLineInFile(currentFile, lineNumber, previewText);
                    }


                    var percent = baseStream.Position * 100.0 / length;
                    if (lineNumber % 1000 == 0)
                    {
                        Console.SetCursorPosition(0, 1);
                        Console.WriteLine(new string(' ', Console.WindowWidth));
                        Console.SetCursorPosition(0, 1);
                        Console.WriteLine($"FindAllInCurrentFile: {currentFile.Name} {percent:00.00}%");
                    }

                    lineNumber++;
                }
            }
        }


        private static void FindAllInFilesInCurrentDirectory(System.IO.DirectoryInfo currentDirectory, SearchResult searchResult, SearchConfiguration config)
        {
            Console.SetCursorPosition(0, 0);
            Console.WriteLine(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, 0);
            Console.WriteLine($"FindAllInFilesInCurrentDirectory: {currentDirectory.Name}");

            var files = currentDirectory.GetFiles();
            for (var i = 0; i < files.Length; i++)
            {
                var file = files[i];
                if (searchResult.FoundCounter >= config.Take)
                {
                    break;
                }

                FindAllInCurrentFile(file, searchResult.SearchText, searchResult, config);

                Console.SetCursorPosition(0, 0);
                Console.WriteLine(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, 0);
                Console.WriteLine($"FindAllInFilesInCurrentDirectory: {currentDirectory.Name} {(i + 1) * 100.0 / files.Length:00.00}%");
            }
        }


        public static SearchResult SearchTextInDirectory(System.IO.DirectoryInfo currentDirectory, in string searchText, SearchConfiguration config = null)
        {
            var task = SearchTextInDirectoryAsync(currentDirectory, new SearchResult(searchText), config);

            task.Wait();

            Console.Clear();

            return task.Result;
        }


        public static Task<SearchResult> SearchTextInDirectoryAsync(System.IO.DirectoryInfo currentDirectory, SearchResult searchResult, SearchConfiguration config = null)
        {
            var configuration = config ?? DefaultConfiguration;
            var mainTask = new Task<SearchResult>(() =>
            {
                FindAllDirectoriesByNameInCurrentDirectory(currentDirectory, searchResult, config);
                FindAllFilesByNameInCurrentDirectory(currentDirectory, searchResult, config);

                if (configuration.IsSearchInContext)
                {
                    FindAllInFilesInCurrentDirectory(currentDirectory, searchResult, config);
                }

                foreach (var directoryInfo in currentDirectory.GetDirectories())
                {
                    var task = SearchTextInDirectoryAsync(directoryInfo, searchResult, config);
                    task.Wait();

                }

                return searchResult;
            });

            mainTask.Start();

            return mainTask;
        }


        public class SearchResult
        {
            private System.Collections.Generic.List<InFileResult> _inFiles;

            public string SearchText { get; }
            public InDirectoryResult InDirectoryByNames { get; }
            public System.Collections.Generic.IReadOnlyList<InFileResult> InFiles => _inFiles.AsReadOnly();
            public int FoundCounter { get; set; }

            public SearchResult(string searchText)
            {
                SearchText = searchText;
                InDirectoryByNames = new InDirectoryResult(this);
                _inFiles = new System.Collections.Generic.List<InFileResult>();
            }

            public void AddLineInFile(FileInfo fileInfo, int lineNumber, string previewText)
            {
                var inFile = _inFiles.FirstOrDefault(x => x.FileInfo.FullName == fileInfo.FullName);

                if (inFile != null)
                {
                    inFile.AddLine(lineNumber, previewText);
                    return;
                }

                inFile = new InFileResult(this, fileInfo);
                inFile.AddLine(lineNumber, previewText);

                _inFiles.Add(inFile);
            }
        }

        public class SearchConfiguration
        {
            public bool IsRegisterSensitive { get; set; }
            public bool IsSearchInContext { get; set; }
            public int Take { get; set; }
            public int Skip { get; set; }
            public bool IsSavePreview { get; set; }
            public int PreviewSymbolsCount { get; }

            public SearchConfiguration()
            {
                Take = int.MaxValue;
                Skip = 0;
                IsSavePreview = false;
                IsSearchInContext = false;
                IsRegisterSensitive = false;

                PreviewSymbolsCount = 40;
            }
        }

        public class InFileResult
        {
            private System.Collections.Generic.List<InLineResult> _inLineResults;
            private SearchResult _searchResult;

            public System.IO.FileInfo FileInfo { get; }

            public System.Collections.Generic.IReadOnlyList<InLineResult> InLineResults => _inLineResults.AsReadOnly();

            public InFileResult(SearchResult searchResult, System.IO.FileInfo fileInfo)
            {
                _searchResult = searchResult;
                FileInfo = fileInfo;
                _inLineResults = new System.Collections.Generic.List<InLineResult>();
            }

            public void AddLine(int lineNumber, string previewText)
            {
                _searchResult.FoundCounter++;
                _inLineResults.Add(new InLineResult(lineNumber, previewText));
            }


            public class InLineResult
            {
                public int LineNumber { get; }
                public string PreviewText { get; }

                public InLineResult(int lineNumber, string previewText)
                {
                    LineNumber = lineNumber;
                    PreviewText = previewText;
                }
            }
        }

        public class InDirectoryResult
        {
            private System.Collections.Generic.List<System.IO.DirectoryInfo> _directories;
            private System.Collections.Generic.List<System.IO.FileInfo> _files;
            private SearchResult _searchResult;


            public System.Collections.Generic.IReadOnlyList<System.IO.DirectoryInfo> Directories => _directories.AsReadOnly();
            public System.Collections.Generic.IReadOnlyList<System.IO.FileInfo> Files => _files.AsReadOnly();

            public InDirectoryResult(SearchResult searchResult)
            {
                _searchResult = searchResult;
                _directories = new System.Collections.Generic.List<System.IO.DirectoryInfo>();
                _files = new System.Collections.Generic.List<System.IO.FileInfo>();
            }

            public void AddFile(System.IO.FileInfo fileInfo)
            {
                _searchResult.FoundCounter++;
                this._files.Add(fileInfo);
            }

            public void AddDirectory(System.IO.DirectoryInfo directoryInfo)
            {
                _searchResult.FoundCounter++;
                this._directories.Add(directoryInfo);
            }
        }

        public static void PrintSearchResult(SearchResult searchResult)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            foreach (var directoryInfo in searchResult.InDirectoryByNames.Directories)
            {
                Console.WriteLine($" * {directoryInfo.FullName}");
            }

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            foreach (var fileInfo in searchResult.InDirectoryByNames.Files)
            {
                Console.WriteLine($" * {fileInfo.FullName}");
            }

            for (var i = 0; i < searchResult.InFiles.Count; i++)
            {
                Console.ForegroundColor = i % 2 == 0 ? ConsoleColor.DarkMagenta : ConsoleColor.Cyan;

                var inFile = searchResult.InFiles[i];
                Console.WriteLine($" * {inFile.FileInfo.FullName} ({inFile.InLineResults.Count})");

                foreach (var inFileInLineResult in inFile.InLineResults)
                {
                    Console.WriteLine($"\tLine {inFileInLineResult.LineNumber}. {inFileInLineResult.PreviewText}");
                }
            }

            Console.ResetColor();
        }
    }
}