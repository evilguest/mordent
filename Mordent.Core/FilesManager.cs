using System.Collections.Generic;

namespace Mordent.Core
{
    public class FilesManager<TFM, TFMF> : IFilesManager
        where TFM : IFileManager
        where TFMF : IFileManagerFactory<TFM>, new()
    {
        private static TFMF _fileManagerFactory = new TFMF();
        private List<TFM> _files;

        public FilesManager(string mainFilePath)
        {
            _files = new List<TFM>() { _fileManagerFactory.Create(mainFilePath) };
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
    }
}
