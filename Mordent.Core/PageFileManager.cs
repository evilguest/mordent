using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Mordent.Core
{
    public class PageFileManager : IFileManager, IAsyncDisposable
    {
        private readonly int _pageSize = 1 << 14;
        private readonly object _sync = new();
        private readonly Stream _fileStream;
        private readonly string _filePath;
        private bool _disposed;

        public PageFileManager(string filePath)
        {
            _filePath = filePath;
            _fileStream = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
        }

        public int PageCount => (int)(_fileStream.Length / _pageSize);

        public int AddPage()
        {
            lock (_sync)
            {
                var pageNo = PageCount;
                _fileStream.SetLength((pageNo + 1) * _pageSize);
                return pageNo;
            }
        }

        public void ReadPage(int pageNo, ref DbPage page)
        {
            lock (_sync)
            {
                _fileStream.Position = pageNo * _pageSize;
                
                if (_fileStream.Read(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref page, 1))) < _pageSize)
                    throw new ArgumentOutOfRangeException(nameof(pageNo), pageNo, $"Failed to read the page {pageNo} from the file {_filePath}");
            }
        }

        public void WritePage(int pageNo, ref DbPage page)
        {
            lock (_sync)
            {
                _fileStream.Position = pageNo * _pageSize;
                _fileStream.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref page, 1)));
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            lock(_sync)
                if (!_disposed)
                {
                    if (disposing)
                        _fileStream.Dispose();

                    _disposed = true;
                }
        }

        void IDisposable.Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            await (_fileStream.DisposeAsync());
            GC.SuppressFinalize(this);
        }
    }
    public struct PageFileManagerFactory : IFileManagerFactory<PageFileManager>
    {
        public PageFileManager Create(string filePath) => new PageFileManager(filePath);
    }
}
