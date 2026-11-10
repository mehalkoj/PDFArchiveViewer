using System.Collections.Frozen;

namespace PDFArchiveViewer.Services
{
    public class PDFIndexStore
    {
        private FrozenDictionary<string, FrozenDictionary<DateOnly, string>> _index =
            FrozenDictionary<string, FrozenDictionary<DateOnly, string>>.Empty;

        private volatile bool _isReady;

        public FrozenDictionary<string, FrozenDictionary<DateOnly, string>> Index => Volatile.Read(ref _index);

        // False until the first build finishes, so the front end can show "still indexing..."
        public bool IsReady => _isReady;

        // Called by BuildPDFIndex when a new index is finished
        public void Replace(FrozenDictionary<string, FrozenDictionary<DateOnly, string>> newIndex)
        {
            Volatile.Write(ref _index, newIndex);
            _isReady = true;
        }
    }
}
