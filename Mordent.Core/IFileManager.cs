namespace Mordent.Core
{
    /// <summary>
    /// Used only in the buffer-based implementation of the IPageManager.
    /// </summary>
    public interface IFileManager
    {
        public void ReadPage(int pageNo, ref DbPage page);
        public void WritePage(int pageNo, ref DbPage page);
        public int AddPage();
        public int PageCount { get; }
    }


}
