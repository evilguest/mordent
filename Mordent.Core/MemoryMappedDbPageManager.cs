using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Mordent.Core
{
    public static class PageAllocatorHelper
    {
        public static DbPageId AllocRowDataPage(this IDbPageManager allocator, DbPageId basePageId)
        {
            var newPage = allocator.AllocatePage(basePageId);
            allocator[basePageId].RowData.Header.NextPageId = newPage;
            allocator[newPage].InitAsHeap();
            allocator[newPage].RowData.Header.PrevPageId = basePageId;
            return newPage;
        }
    }
    public class MemoryMappedDbPageManager: IDbPageManager
    {
        public MemoryMappedDbPageManager(string mainFilePath, bool initNew)
        {
            Files.Add(new MemoryMappedFilePageManager(mainFilePath, initNew));
        }

        public ref DbPage this[DbPageId pageId] => ref Files[pageId.FileNo][pageId.PageNo];

        public long AvailablePages => Files.Sum(f => (long)f.AvailablePages);

        private List<IFilePageManager> Files { get; } = new();

        public static int PagesToExtents(int pages) => pages == 0 ? 0 : (pages - 1) / DbPage.ExtentAllocPayload.PagesPerExtent + 1;


        public DbPageId AllocatePage()
        {
            return AllocatePage(DbPageId.None);
        }

        public DbPageId AllocatePage(DbPageId basePageId)
        {
            //1. Figure out the page file
            var fileNo = basePageId.FileNo;
            if (Files[fileNo].AvailablePages > 0)
            {
                var newPageNo = Files[fileNo].AllocatePage(basePageId.PageNo);
                return new DbPageId(fileNo, newPageNo);
            }
            var leastUsedFile = (from file in Files orderby file.AvailablePages descending select file).FirstOrDefault();
            fileNo = (ushort)Files.IndexOf(leastUsedFile);
            if (Files[fileNo].AvailablePages > 0)
            {
                var newPageNo = Files[fileNo].AllocatePage(basePageId.PageNo);
                return new DbPageId(fileNo, newPageNo);
            }
            throw new Exception("File growth is not implemented yet");
        }

        public void AttachFile(short fileNo, string filePath)
        {
            throw new NotImplementedException();
        }

        public void CreateFile(short fileNo, string filePath)
        {
            throw new NotImplementedException();
        }

        public void FreePage(DbPageId pageId)
        {
            throw new NotImplementedException();
        }
    }
}
