using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Mordent.Core
{
    public class LogFile : ILogFile
    {
        private readonly IFileManager _logStream;
        private readonly string _logFilePath;
        private int _currentPageNo;
        private Lsn _latestLSN;
        private Lsn _lastSavedLSN;
        private DbPage _logPage;
        private bool _disposed;

        public LogFile(string filePath)
        {
            _logFilePath = filePath;
            _logStream = new PageFileManager(filePath);
            if (_logStream.PageCount == 0)
            {
                _currentPageNo = AppendNewPage();
            }
            else
            {
                _currentPageNo = _logStream.PageCount - 1;
                _logStream.ReadPage(_currentPageNo, ref _logPage);
            }
        }

        public IEnumerable<byte[]> Records
        {
            get
            {
                Flush();
                return IterateRecords(_logStream, _currentPageNo);
            }
        }

        private IEnumerable<byte[]> IterateRecords(IFileManager logStream, int startPage)
        {
            var page = new DbPage();
            for (var currentPage = startPage; currentPage >= 0; currentPage--)
            {
                logStream.ReadPage(currentPage, ref page);
                var currentPos = page.Log.Boundary;
                while(currentPos < DbPage.LogPayload.Capacity)
                {
                    var d = page.Log.Read(currentPos);
                    yield return d;
                    currentPos += (short)(d.Length+sizeof(short));
                }
            }
        }

        public Lsn Append(ReadOnlySpan<byte> data)
        {
            if (_logPage.Log.AvailableBytes < data.Length)
            {
                Flush();
                _currentPageNo = AppendNewPage();
            }
            _logPage.Log.Write(data);
            return ++_latestLSN;
        }

        private int AppendNewPage()
        {
            var newPage = _logStream.AddPage();
            _logPage.InitAsLogPage();
            _logStream.WritePage(newPage, ref _logPage);
            return newPage;
        }

        public void Flush(Lsn upToLsn)
        {
            if (upToLsn >= _lastSavedLSN)
                Flush();
        }
        private void Flush()
        {
            _logStream.WritePage(_currentPageNo, ref _logPage);
            _lastSavedLSN = _latestLSN;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                    _logStream.Dispose();

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        //public async ValueTask DisposeAsync()
        //{
        //    await _logStream.DisposeAsync();
        //    GC.SuppressFinalize(this);
        //}
    }
}
