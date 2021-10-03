using DotNext.IO.MemoryMappedFiles;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace Mordent.Core
{

    public class MemoryMappedFilePageManager : IFilePageManager
    {
        private object __fileLock = new(); // used for locking
        private string _filePath;
        private MemoryMappedFile _mmfFile;
        private FileStream _fileStream;
        private MemoryMappedDirectAccessor _acc;

        public MemoryMappedFilePageManager(string filePath, bool initNew)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException($"'{nameof(filePath)}' cannot be null or empty.", nameof(filePath));

            _filePath = filePath;
            _fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            if (_fileStream.Length != 0)
            {
                _mmfFile = MemoryMappedFile.CreateFromFile(_fileStream, null, 0, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true);
                _acc = _mmfFile.CreateDirectAccessor();
            }
            else
            {
                ExtentsCount = 1;
                var fileHeaderPageNo = AllocatePage();
                this[fileHeaderPageNo].InitAsFileHeaderPage();
            }
        }



        #region IFilePageManager implementation
        public ref DbPage this[int pageNo] => ref Pages[pageNo];

        public int ExtentsCount
        {
            get
            {
                lock (__fileLock)
                    return (int)(_fileStream.Length >> (DbPage.SizeLog + 3));
            }
            set
            {
                lock (__fileLock)
                {
                    var oldValue = ExtentsCount;
                    if (value == oldValue) // no change
                        return;
                    if (value < oldValue)
                    {
                        // TODO: check that the pages being cut are not allocated

                    }
                    _fileStream.SetLength(value << (DbPage.SizeLog + 3));
                    _fileStream.Flush();
                    if (!_acc.IsEmpty)
                    {
                        _acc.Dispose();
                        if (_mmfFile != null)
                            _mmfFile.Dispose();
                    }
                    _mmfFile = MemoryMappedFile.CreateFromFile(_fileStream, null, 0, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true);
                    _acc = _mmfFile.CreateDirectAccessor();
                    InitAllocationPages(oldValue);
                }
            }
        }

        public int AllocatePage() => AllocatePage(0);
        public int AllocatePage(int basePageNo)
        {
            // 1. Try to allocate a page in the same extent
            var (pfPageNo, pageSlotNo) = GetPFPageForPage(basePageNo);

            var newPage = this[pfPageNo].PageAlloc.FindFirstNonAllocatedPage(pageSlotNo / DbPage.ExtentAllocPayload.PagesPerExtent);
            if (newPage > 0) // Yeah! We've got the page in the same extent!
            {
                var firstExtentPage = pageSlotNo / DbPage.ExtentAllocPayload.PagesPerExtent * DbPage.ExtentAllocPayload.PagesPerExtent; // TODO: replace with bit fiddling
                newPage += firstExtentPage;
                MarkPageMixedExtent(newPage);
            }
            else
            {
                newPage = FindPageInEmptyExtent();
                // TODO: handle -1 - no more extents
                MarkPageAllocated(newPage);
            }

            return newPage;
        }
        private void MarkPageAllocated(int pageNo)
        {
            var (pfPageNo, pageSlotNo) = GetPFPageForPage(pageNo);
            this[pfPageNo].PageAlloc[pageSlotNo] |= PageAllocationStatus.PageAllocatedMask;
            // if this was a mixed extent, then we need to check whether it still has any free pages
            var (gamPageNo, extentNo) = GetGamPageNoForExtent(pageNo / DbPage.ExtentAllocPayload.PagesPerExtent);
            if (this[gamPageNo + 1].ExtentAlloc[extentNo])
            {
                if (this[pfPageNo].PageAlloc.FindFirstNonAllocatedPage(pageSlotNo / DbPage.ExtentAllocPayload.PagesPerExtent) == -1)
                    this[gamPageNo + 1].ExtentAlloc[extentNo] = false; // no more room
            }
        }
        private static (int, int) GetGamPageNoForExtent(int extentNo)
            => ((extentNo / DbPage.ExtentAllocPayload.ExtentsCapacity * DbPage.ExtentAllocPayload.ExtentsCapacity) + 1, extentNo % DbPage.ExtentAllocPayload.ExtentsCapacity);

        private int FindPageInEmptyExtent()
        {
            for (var gamPageNo = 1; gamPageNo < Pages.Length; gamPageNo += DbPage.ExtentAllocPayload.ExtentsCapacity)
            {
                var fe = Pages[gamPageNo].ExtentAlloc.FindFirstFreeExtent();
                if (fe < 0)
                    continue;
                var firstFreePage = gamPageNo - 1 + fe * DbPage.ExtentAllocPayload.PagesPerExtent;
                if (firstFreePage < Pages.Length)
                    return firstFreePage;
                else
                    return -1;

            }
            return -1;
        }

        public void MarkPageMixedExtent(int pageNo)
        {
            var (pfPageNo, pageSlotNo) = GetPFPageForPage(pageNo);
            this[pfPageNo].PageAlloc[pageSlotNo] |= PageAllocationStatus.PageAllocatedMask | PageAllocationStatus.PageMixedExtentMask;

            MarkExtentMixed(pageNo / DbPage.ExtentAllocPayload.PagesPerExtent);
        }

        private void MarkExtentMixed(int extentNo)
        {
            var (gamPageNo, extentSlotNo) = GetGamPageNoForExtent(extentNo);
            this[gamPageNo].ExtentAlloc[extentSlotNo] = false; // GAM: segment is no longer available
            this[gamPageNo + 1].ExtentAlloc[extentSlotNo] = true;  // SGAM: segment is mixed
        }



        private (int pfPageNo, ushort pageSlotNo) GetPFPageForPage(int pageNum)
        {
            Debug.Assert(this[pageNum / DbPage.AllocPagePayload.PagesCapacity].Header.Type == DbPageType.FreeSpace, "Invalid PF page type");

            return (pageNum / DbPage.AllocPagePayload.PagesCapacity, (ushort)(pageNum % DbPage.AllocPagePayload.PagesCapacity));
        }


        public void FreePage(int pageNo)
        {
            throw new NotImplementedException();
        }

        private Span<DbPage> Pages => MemoryMarshal.Cast<byte, DbPage>(_acc.Bytes);

        public int AvailablePages
        {
            get => this[3].FileHeader.AvailablePages; // File Header page!
            private set => this[3].FileHeader.AvailablePages = value;
        }

        public void InitAllocationPages(int oldExtentsCount)
        {
            var firstNewPF = oldExtentsCount == 0 ? 0 : (oldExtentsCount * DbPage.ExtentAllocPayload.PagesPerExtent) / DbPage.AllocPagePayload.PagesCapacity + 1;
            var lastNewPF = (ExtentsCount * DbPage.ExtentAllocPayload.PagesPerExtent) / DbPage.AllocPagePayload.PagesCapacity;
            var c = 0;
            for (var i = firstNewPF; i <= lastNewPF; i++) // this cycle would be empty if we aren't crossing the PF boundary
            {
                int newPFPageNum = i * DbPage.AllocPagePayload.PagesCapacity;
                this[newPFPageNum].InitAsPfPage();
                c++;
                if (i % (DbPage.ExtentAllocPayload.PagesPerExtent * sizeof(byte)) == 0) // time to add GAM/SGAM
                {
                    this[newPFPageNum + 1].InitAsGamPage();
                    this[newPFPageNum + 2].InitAsSGamPage();
                    MarkPageMixedExtent(newPFPageNum + 1);
                    MarkPageMixedExtent(newPFPageNum + 2);
                    c += 2;
                }
                MarkPageMixedExtent(newPFPageNum);
            }
            AvailablePages += (ExtentsCount - oldExtentsCount) * 8 - c;
        }
        #endregion
    }
}
