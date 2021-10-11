using System;

namespace Mordent.Core
{
    /// <summary>
    /// Used only in the buffer-based implementation of the IPageManager.
    /// </summary>
    public interface IFileManager: IDisposable //, IAsyncDisposable
    {
        public void ReadPage(int pageNo, ref DbPage page);
        public void WritePage(int pageNo, ref DbPage page);
        public int AddPage();
        public int PageCount { get; }
    }

    public interface IFilesManager: IDisposable
    {
        int AttachFile(string filePath);
        public void ReadPage(DbPageId pageId, ref DbPage page);
        public void WritePage(DbPageId pageId, ref DbPage page);
        public DbPageId AddPage();
        //public int PageCount { get; }
    }
    public interface IFileManagerFactory<TFM>
        where TFM: IFileManager
    {
        public TFM Create(string filePath);
    }
}
