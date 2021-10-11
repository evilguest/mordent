using System.Linq;
using System.Collections.Generic;

namespace Mordent.Core
{
    public class FilesManager<TFM, TFMF> : IFilesManager
        where TFM : IFileManager
        where TFMF : IFileManagerFactory<TFM>, new()
    {
        private static TFMF _fileManagerFactory = new TFMF();
        private List<TFM> _files;
        private bool _disposed;

        public FilesManager(string mainFilePath)
        {
            _files = new List<TFM>() { _fileManagerFactory.Create(mainFilePath) };
        }

        public DbPageId AddPage()
        {
            var smallestFile = (from f in _files orderby f.PageCount select f).First();
            return new DbPageId((ushort)_files.IndexOf(smallestFile), smallestFile.AddPage());
        }

        public int AttachFile(string filePath)
        {
            lock (_files)
            {
                _files.Add(_fileManagerFactory.Create(filePath));
                return _files.Count - 1;
            }
        }


        public void ReadPage(DbPageId pageId, ref DbPage page) => _files[pageId.FileNo].ReadPage(pageId.PageNo, ref page);

        public void WritePage(DbPageId pageId, ref DbPage page) => _files[pageId.FileNo].WritePage(pageId.PageNo, ref page);

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    foreach(var f in _files)
                        f.Dispose();
                }

                _disposed = true;
            }
        }


        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            System.GC.SuppressFinalize(this);
        }
    }
}
