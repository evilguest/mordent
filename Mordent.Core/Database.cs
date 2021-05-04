using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using DotNext.IO.MemoryMappedFiles;

namespace Mordent.Core
{
    public struct DatabaseFile
    {
        public DbFileHeaderPage FileHeader;
        public DbPageAllocPage FreeSpacePage1;
        public DbExtentAllocPage GamPage1;
        public DbExtentAllocPage SgamPage1;
        public DbPage BootPage; //TODO: fill in the boot page
    }

    public class Database: IDisposable
    {
        private const long MordentDataTag = 0x44_74_6E_65_64_72_6F_4D;
        private bool disposedValue;
        private MemoryMappedFile _file;
        private MemoryMappedDirectAccessor _acc;
        //public delegate void DbAction(Span<DbPage> pages);

        public Database(string filePath, bool initNew)
        {
            _file = initNew
                ? MemoryMappedFile.CreateFromFile(filePath, FileMode.CreateNew, null, DbPage.Size * 10)
                : MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, null);
            _acc = _file.CreateDirectAccessor();
            var h = MemoryMarshal.Cast<byte, DatabaseFile>(_acc.Bytes);
            if (initNew)
                InitDb(ref h[0]);
            else
                CheckDb(ref h[0]);
        }

        private void CheckDb(ref DatabaseFile file)
        {
            if (file.FileHeader.Tag != MordentDataTag)
                throw new InvalidOperationException("Invalid file format tag found");
        }

        private void InitDb(ref DatabaseFile file)
        {
            file.FileHeader.Tag = MordentDataTag; // Mordent
            file.FileHeader.Version = 0x0000_0001;
            file.FileHeader.Type = 42; // data file
            file.FileHeader.MaxPagesGrowth = int.MaxValue;
            file.FileHeader.RedoStartLSN = 8;

            file.FreeSpacePage1._header.Type = DbPageType.FreeSpace;
            file.FreeSpacePage1._header.PrevPageNo = -1;
            file.FreeSpacePage1._header.NextPageNo = -1;
            file.FreeSpacePage1._header.DataCount = 5;
            file.FreeSpacePage1[0] = PageAllocationStatus.PageAllocatedMask | PageAllocationStatus.PageMixedExtentMask | PageAllocationStatus.PageCompletelyFull;
            file.FreeSpacePage1[1] = PageAllocationStatus.PageAllocatedMask | PageAllocationStatus.PageMixedExtentMask | PageAllocationStatus.PageUpTo50PercentFull;
            file.FreeSpacePage1[2] = PageAllocationStatus.PageAllocatedMask | PageAllocationStatus.PageMixedExtentMask | PageAllocationStatus.PageUpTo50PercentFull;
            file.FreeSpacePage1[3] = PageAllocationStatus.PageAllocatedMask | PageAllocationStatus.PageMixedExtentMask | PageAllocationStatus.PageUpTo50PercentFull;
            file.FreeSpacePage1[4] = PageAllocationStatus.PageAllocatedMask | PageAllocationStatus.PageMixedExtentMask | PageAllocationStatus.PageCompletelyFull;

            file.GamPage1._header.Type = DbPageType.GlobalAllocationMap;
            file.GamPage1._header.PrevPageNo = -1;
            file.GamPage1._header.NextPageNo = -1;
            file.GamPage1[0] = true; // first extent occupied
            file.SgamPage1._header.Type = DbPageType.GlobalAllocationMap;
            file.SgamPage1._header.PrevPageNo = -1;
            file.SgamPage1._header.NextPageNo = -1;
            file.SgamPage1[0] = true; // first extent mixed

            _acc.Flush();
        }

        public Span<DbPage> GetSpan() => MemoryMarshal.Cast<byte, DbPage>(_acc.Bytes);

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _acc.Dispose();
                    _file.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Database()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
