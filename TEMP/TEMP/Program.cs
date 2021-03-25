using System.Collections.Generic;

namespace SearchTest
{
    public class Program
    {

        public static void Main(string[] args)
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
                var desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop); 
                System.IO.DirectoryInfo rootDirectory = new System.IO.DirectoryInfo(desktopPath);

                // 3. Search text in folder
                var searchConfig = new SearchEngine.SearchConfiguration()
                {
                    IsSavePreview = true,
                    IsSearchInContext = true
                };
                var searchResult = SearchEngine.SearchTextInDirectory(rootDirectory, searchText, searchConfig);
                SearchEngine.PrintSearchResult(searchResult);

                System.Console.WriteLine("Press any key to continue...");
                System.Console.ReadKey();
                System.Console.Clear();
            }
        }
    }

}
