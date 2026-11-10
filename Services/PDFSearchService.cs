namespace PDFArchiveViewer.Services
{

    public class SearchResult
    {
        public string Category { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }

    }

    public class PDFSearchService
    {
        private readonly PDFIndexStore _store;

        public PDFSearchService(PDFIndexStore store)
        {
            _store = store;
        }

        public bool IsReady => _store.IsReady;

        // all category (folder) names, sorted
        public IReadOnlyList<string> GetCategories()
        {
            return _store.Index.Keys.Order().ToList();
        }

        public IReadOnlyList<KeyValuePair<DateOnly, string>> FindFiles(string category, DateOnly from, DateOnly to)
        {
            var index = _store.Index;

            if (!index.TryGetValue(category, out var files))
                return [];

            return files
                .Where(f => f.Key >= from && f.Key <= to)
                .OrderBy(f => f.Key)
                .ToList();
        }

    }
}
