using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;
using DotNext.IO.MemoryMappedFiles;
using DotNext.Runtime.InteropServices;

namespace Mordent.Core
{
    public struct DatabaseFile
    {
        public DbFileHeaderPage FileHeader;
        public DbPageAllocPage FreeSpacePage1;
        public DbExtentAllocPage GamPage1;
        public DbExtentAllocPage SgamPage1;
        public DbRowDataPage BootPage; //TODO: fill in the boot page
    }

    public class Database : IDisposable
    {
        private const long MordentDataTag = 0x44_74_6E_65_64_72_6F_4D;
        private object __databaseLock = new object(); // used for locking

        private bool disposedValue;
        private string _filePath; 
        private MemoryMappedFile _fileMmf;
        private FileStream _fileStream;
        private MemoryMappedDirectAccessor _acc;
        //public delegate void DbAction(Span<DbPage> pages);

        public Database(string filePath, bool initNew)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException($"'{nameof(filePath)}' cannot be null or empty.", nameof(filePath));

            _filePath = filePath;
            _fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            if (_fileStream.Length == 0)
                PageCount = 6;
            else
            {
                _fileMmf = MemoryMappedFile.CreateFromFile(_fileStream, null, 0, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true);
                _acc = _fileMmf.CreateDirectAccessor();
            }

            var h = MemoryMarshal.Cast<byte, DatabaseFile>(_acc.Bytes);
            if (initNew)
                InitDb(ref h[0]);
            else
                CheckDb(ref h[0]);
        }

        /// <summary>
        /// Data size in bytes
        /// </summary>
        public long DataSize
        {
            get => PageCount << DbPage.SizeLog; // * DbPage.Size;
            set => PageCount = (value >> DbPage.SizeLog) + ((value & DbPage.SizeMask) != 0 ? 1 : 0);
        }
        public long PageCount 
        {
            get => ExtentsCount << 3; // * 8
            set => ExtentsCount = (value >> 3) + ((value & 0b111) != 0 ? 1 : 0);
        }

        public long ExtentsCount
        {
            get
            {
                lock (__databaseLock)
                    return _fileStream.Length >> (DbPage.SizeLog + 3);
            }
            set
            {
                lock (__databaseLock)
                {
                    if (value == ExtentsCount) // no change
                        return;
                    if (value < ExtentsCount)
                    {
                        // TODO: check that the pages being cut are not allocated

                    }
                    if (value < ExtentsCount)
                    {
                        // TODO: init GAM, SGAM, and PF pages
                    }
                    _fileStream.SetLength(value << (DbPage.SizeLog + 3));
                    _fileStream.Flush();
                    if (!_acc.IsEmpty)
                    {
                        _acc.Dispose();
                        if (_fileMmf != null)
                            _fileMmf.Dispose();
                    }
                    _fileMmf = MemoryMappedFile.CreateFromFile(_fileStream, null, 0, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true);
                    _acc = _fileMmf.CreateDirectAccessor();
                }
            }
        }
        public Pointer<DbPage> Pages { get => _acc.Pointer.As<DbPage>(); }

        //public Span<DbPage> Pages => MemoryMarshal.Cast<byte, DbPage>(_acc.Bytes);
        //public Span<DbRowDataPage> DataPages => MemoryMarshal.Cast<byte, DbRowDataPage>(_acc.Bytes);
        public unsafe DbRowDataPage* GetDataPages()
        {
            return (DbRowDataPage*)(byte*)_acc.Pointer;
        }

        private void CheckDb(ref DatabaseFile file)
        {
            if (file.FileHeader.Tag != MordentDataTag)
                throw new InvalidOperationException("Invalid file format tag found");
        }

        private unsafe void InitDb(ref DatabaseFile file)
        {
            PageCount = 6;
            file.FileHeader.Tag = MordentDataTag; // Mordent
            file.FileHeader.Version = 0x0000_0001;
            file.FileHeader.Type = 42; // data file
            file.FileHeader.MaxPagesGrowth = int.MaxValue;
            file.FileHeader.RedoStartLSN = 8;

            file.FreeSpacePage1.Header.Type = DbPageType.FreeSpace;
            //file.FreeSpacePage1.Header.PrevPageNo = 0;
            //file.FreeSpacePage1.Header.NextPageNo = 0;
            file.FreeSpacePage1.Header.DataCount = 5;
            file.FreeSpacePage1[0] = PageAllocationStatus.PageAllocatedMask | PageAllocationStatus.PageMixedExtentMask | PageAllocationStatus.PageCompletelyFull;
            file.FreeSpacePage1[1] = PageAllocationStatus.PageAllocatedMask | PageAllocationStatus.PageMixedExtentMask | PageAllocationStatus.PageUpTo50PercentFull;
            file.FreeSpacePage1[2] = PageAllocationStatus.PageAllocatedMask | PageAllocationStatus.PageMixedExtentMask | PageAllocationStatus.PageUpTo50PercentFull;
            file.FreeSpacePage1[3] = PageAllocationStatus.PageAllocatedMask | PageAllocationStatus.PageMixedExtentMask | PageAllocationStatus.PageUpTo50PercentFull;
            file.FreeSpacePage1[4] = PageAllocationStatus.PageAllocatedMask | PageAllocationStatus.PageMixedExtentMask | PageAllocationStatus.PageCompletelyFull;

            file.GamPage1.Header.Type = DbPageType.GlobalAllocationMap;
            //file.GamPage1.Header.PrevPageNo = 0;
            //file.GamPage1.Header.NextPageNo = 0;
            file.GamPage1[0] = true; // first extent occupied
            file.SgamPage1.Header.Type = DbPageType.GlobalAllocationMap;
            //file.SgamPage1.Header.PrevPageNo = 0;
            //file.SgamPage1.Header.NextPageNo = 0;
            file.SgamPage1[0] = true; // first extent mixed

            file.BootPage.Header.Type = DbPageType.Heap;
            GetDataPages()[5].Header.Type = DbPageType.Heap;
            GetDataPages()[5].Header.PrevPageNo = 0;
            GetDataPages()[5].Header.NextPageNo = 0;
            AddRow(5, BuildTablesTable());

            GetDataPages()[6].Header.Type = DbPageType.Heap;
            GetDataPages()[6].Header.PrevPageNo = 0;
            GetDataPages()[6].Header.NextPageNo = 0;
            AddRow(6, new TableField(TablesTableId, "Id", "System.Guid"));
            AddRow(6, new TableField(TablesTableId, "Name", "System.String"));
            AddRow(6, new TableField(TablesTableId, "FirstPage", "System.Int32"));
            AddRow(6, new TableField(TablesTableId, "LastPage", "System.Int32"));

            AddRow(5, new Table(FieldsTableId, 6,6) { Name = "Fields" });
            AddRow(6, new TableField(FieldsTableId, "Id", "System.Guid"));
            AddRow(6, new TableField(FieldsTableId, "TableId", "System.Guid"));
            AddRow(6, new TableField(FieldsTableId, "Name", "System.String"));
            AddRow(6, new TableField(FieldsTableId, "Type", "System.String"));

            //file.
            _acc.Flush();
        }

        private static Table BuildTablesTable() => new Table(TablesTableId, 5, 5) { Name = "Tables" };

        public DbRowId AddRow(IDbSerializable rowData)
        {
            // 0. Detect the table.
            // 1. Find a suitable page
            var totalSize = rowData.TotalDataSize;
            // check if the table does contain enough space for the whole record
            // 3. Insert the data into the page
            // return AddRow(page, rowData)
            return DbRowId.None;

        }

        public T ReadRow<T>(DbRowId rowId) where T: IDbSerializable
        {
            return default;
        }

        public Table FindTable(string tableName)
        {
            if (tableName == "Tables")
                return BuildTablesTable();
            else
            {
                var row = new DbRowId(FindTable("Tables").FirstPage, 0);
                do
                {
                    var t = ReadRow<Table>(row);
                    if (t.Name == tableName)
                        return t;
                    row = GetNext(row);
                } while (row.PageNo != 0);
                return null;
            }
        }

        public unsafe DbExtentAllocPage* GetGamPageRefForPage(ref int extentNo)
        {
            int gamPageNo = extentNo / DbExtentAllocPage.ExtentsCount;
            extentNo = gamPageNo % DbExtentAllocPage.ExtentsCount;
            if (gamPageNo == 0)
                gamPageNo = 2;
            Debug.Assert(GetDataPages()[gamPageNo].Header.Type == DbPageType.GlobalAllocationMap, "Reading a non-GAM page as GAM!");
            return (DbExtentAllocPage*)GetDataPages() + gamPageNo;

        }
        public unsafe DbRowId AddRow(string tableName, IDbSerializable rowData)
        {
            var t = FindTable(tableName);
            Debug.Assert(t != null, $"Couldn't find table {tableName}");
            var size = rowData.TotalDataSize;
            var pageNo = t.FirstPage;
            var pagePtr = GetDataPagePtr(pageNo);
            while(pagePtr->FreeSpace < size)
            {
                if (pagePtr->Header.NextPageNo == 0)
                    break;
                pagePtr = GetDataPagePtr(pagePtr->Header.NextPageNo);
            }
            if (pagePtr->FreeSpace < size) // we couldn't find a matching page, create a new one
            {
               
            }
            return DbRowId.None; //TODO: fix
        }
        public unsafe DbRowId AddRow(int pageNo, IDbSerializable rowData)
        {
            var pagePtr = GetDataPagePtr(pageNo);
            Debug.Assert(pagePtr->FreeSpace >= rowData.FixedDataSize, "Not enough space to store requested data on this page");
            if (rowData.TotalDataSize > pagePtr->FreeSpace)
            {
                // TODO: implement
                throw new NotImplementedException("The row overflow is not implemented yet");
            }
            var slotNo = pagePtr->AddSlot((short)rowData.TotalDataSize);
            var dataSpan = new Span<byte>(pagePtr->GetSlotPtr(slotNo), rowData.TotalDataSize);
            dataSpan = dataSpan.Slice(rowData.Write(dataSpan, 0)); // fixed data
            foreach (var item in rowData.DataItems)
                dataSpan = dataSpan.Slice(rowData.Write(dataSpan, item));
            
            return new DbRowId(pageNo, slotNo);
        }


        ////public Span<DbPage> GetSpan() => MemoryMarshal.Cast<byte, DbPage>(_acc.Bytes);
        //public void InsertRow<T>(T obj, SpanAction<byte, T> saveAction)
        //{
        //    // figure out the storage size
        //    // find the page chain
        //    // find an empty enough page
        //    // reserve a slot
        //    // call the saveAction with that slot.
        //}

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _acc.Dispose();
                    _fileMmf.Dispose();
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Span<byte> GetPageRowData(DbRowId rowId)
        {
            return _acc.Bytes.Slice(rowId.PageNo << DbPage.SizeLog + GetDataPagePtr(rowId)->GetSlotOffset(rowId.SlotNo));
        }

        public unsafe DbRowDataPage* GetDataPagePtr(int pageNo)
        {
            if (pageNo <= 0)
                throw new ArgumentOutOfRangeException(nameof(pageNo), "Negative pageNo detected");

            Debug.Assert(GetDataPages()[pageNo].Header.Type == DbPageType.Heap, "Reading a non-data page as data!");

            return GetDataPages() + pageNo;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe DbRowDataPage* GetDataPagePtr(DbRowId rowId)
        {
            Debug.Assert(rowId.FileNo == 0, "Broken RowID with a non-zero file ID. Only one file is supported yet");
            return GetDataPagePtr(rowId.PageNo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe DbRowId GetNext(DbRowId rowId)
        {
            var slotNo = rowId.SlotNo++;
            if (slotNo < GetDataPagePtr(rowId)->Header.DataCount)
                return new DbRowId(rowId.PageNo, slotNo);
            else
                // Note that if there are no more pages, the NextPageNo would contain 0; thus we're returning an empty DbRowID that is equal to DbRowId.None
                return new DbRowId(GetDataPagePtr(rowId)->Header.NextPageNo, 0);

        }
        public unsafe DbRowId GetPrev(DbRowId rowId)
        {
            var slotNo = rowId.SlotNo;
            if (slotNo > 0)
                return new DbRowId(rowId.PageNo, slotNo--);
            else
            {
                // Note that if there are no more pages, the PrevPageNo would contain 0
                int prevPageNo = GetDataPagePtr(rowId)->Header.PrevPageNo;
                if (prevPageNo == 0)
                    return DbRowId.None;
                else
                    return new DbRowId(prevPageNo, GetDataPagePtr(prevPageNo)->Header.DataCount--);
            }
        }

        /// <summary>
        /// Bootstrap method
        /// </summary>
        /// <returns>A row ID pointing to the first row of the Tables table</returns>
        public DbRowId GetTablesTableFirstRowId()
        {
            // tables table is always starting with the Page 5
            return new DbRowId(5, 0);
        }
        public unsafe string GetString(StringHeader* headerPtr)
        {
            var b = new StringBuilder(headerPtr->Length);
            var h = &headerPtr->FirstSegmentHeader;
            do
            {
                var pData = ((byte*)h + sizeof(StringHeader)); // chars are laid out right after the header
                b.Append(new ReadOnlySpan<char>(pData, h->SegmentLen));
                // now we need to jump to the page referenced by the segment header
                if (!h->HasMore)
                    break;

                h = (StringSegmentHeader*)GetDataPagePtr(h->PageNo)->GetSlotPtr(0); // continuations are always at slot 0
            } while (true);

            return b.ToString();
        }
        private static readonly Guid TablesTableId = new("D42F19E7-93F6-4D48-884C-7EB72685A6B2");
        private static readonly Guid FieldsTableId = new("7E72F3F2-089E-4CBE-816A-7A6579A8ABE0");

        
        //TODO:
        // 1. Allocate page:
        // 1.1. For an existing storage unit:
        // 1.1.1. Take the storage unit
        // 1.1.2. Find the last page
        // 1.1.3. Check whether its extent still has some space:
        // 1.1.3.1. Yes: Allocate from same extent
        // 1.1.3.1.1. Find the first non-allocated page within that extent
        // 1.1.3.2. No: find a new free extent
        // 1.1.3.2.1. Scan through the GAMs looking for a free extent
        // 1.1.3.2.1.1. Found some extent: Take first page from this extent
        // 1.1.3.2.1.2. No extents found: GROW file, go to 1.1.3.2.1.1
        // 1.1.4. Update the extent consumption maps
        // 1.1.5. Update the prev/next page references in the old and new pages
        // 1.2. For a new storage unit:
        // 1.2.1. Find an extent with a page
        // 1.2.1.1. Scan through the SGAMs looking for a free extent
        // 1.2.1.1.1. Found: scan through the pages of this extent finding a non-allocated one
        // 1.2.1.1.2. Not found: scan through GAMs
        // 1.2.1.1.2.1. Found some free extent
        // 1.2.1.1.2.1.1. Mark it as Shared
        // 1.2.1.1.2.1.2. Take the first page from this extent
        // 1.2.1.2. Find an 


    }
}
