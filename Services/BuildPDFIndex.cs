using Microsoft.Extensions.Options;
using System.Collections.Frozen;


namespace PDFArchiveViewer.Services
{

    public class BuildPDFIndex(IOptionsMonitor<AppSettings> settings, PDFIndexStore store) : BackgroundService
    {
        private AppSettings Settings => settings.CurrentValue;


        //private FrozenDictionary<string, FrozenDictionary<DateOnly, string>> _index = FrozenDictionary<string, FrozenDictionary<DateOnly, string>>.Empty;

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


                    foreach (string dir in directories)
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

                        string category = Path.GetFileName(dir);
                        PDFIndex[category] = PDFFiles.ToFrozenDictionary();
                        Console.WriteLine($"Appended {category} files to PDFIndex!");

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
