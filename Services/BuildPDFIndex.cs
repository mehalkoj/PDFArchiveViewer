using Microsoft.Extensions.Options;
using System.Collections.Frozen;


namespace PDFArchiveViewer.Services
{

    public class BuildPDFIndex(IOptionsMonitor<AppSettings> settings, PDFIndexStore store) : BackgroundService
    {
        private AppSettings Settings => settings.CurrentValue;



        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                bool ok = await Task.Run(Build, stoppingToken);
                await Task.Delay(ok ? TimeSpan.FromHours(24) : TimeSpan.FromMinutes(5), stoppingToken); // if works, scan again 24 hours, if not try to find it again in 5 minutes
            }
        }


        // Scans all the pdfs and puts them into memory
        public bool Build()
        {
            string path = Settings.GetRootPath();
            string format = Settings.Format;


            if (!Directory.Exists(path))
            {
                Console.WriteLine("Archive path is unreachable or access was denied");
                return false;
            }
            try
            {
                var directories = Directory.EnumerateDirectories(path);

                var PDFIndex = new Dictionary<string, FrozenDictionary<DateOnly, string>>(StringComparer.OrdinalIgnoreCase);

                // index from the last successful scan, used as a fallback if a folder can't be read
                var previousIndex = store.Index;


                foreach (string dir in directories)
                {
                    string category = Path.GetFileName(dir);

                    try
                    {
                        var PDFFiles = new Dictionary<DateOnly, string>();

                        // looping through pdf files
                        string[] files = Directory.GetFiles(dir, "*.pdf");
                        foreach (string filePath in files)
                        {
                            // converts string based on format into a date
                            if (DateOnly.TryParseExact(Path.GetFileNameWithoutExtension(filePath), format, out var date))
                            {
                                PDFFiles[date] = filePath;
                                Console.WriteLine($"Added {date}: {filePath}");
                            }
                            else
                            {
                                Console.WriteLine($"Skipped (bad filename): {filePath}");
                            }
                        }

                        PDFIndex[category] = PDFFiles.ToFrozenDictionary();
                        Console.WriteLine($"Appended {category} files to PDFIndex!");
                    }
                    // Only access/IO problems are handled here.
                    catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
                    {
                        // Keep last scan's files for this folder if we have them, so a
                        // temporary network hiccup doesn't make a category disappear
                        if (previousIndex.TryGetValue(category, out var oldFiles))
                        {
                            PDFIndex[category] = oldFiles;
                            Console.WriteLine($"Could not read '{category}' ({ex.Message}). Kept files from the previous scan.");
                        }
                        else
                        {
                            Console.WriteLine($"Skipped unreadable folder '{category}': {ex.Message}");
                        }
                    }
                }

                // last thing: hand the finished index to the shared store
                store.Replace(PDFIndex.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase));


                return true;
            }
            catch (UnauthorizedAccessException) { Console.WriteLine("Error: You do not have permission to access this network path."); return false; }
            catch (IOException ex) { Console.WriteLine($"Network or I/O error occurred: {ex.Message}"); return false; }
            catch (Exception ex) { Console.WriteLine($"Error: {ex.Message}"); return false; }
        }




    }
}