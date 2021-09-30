using System;
using System.Collections;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Unicode;
using DotNext.IO.MemoryMappedFiles;
using DotNext.Runtime.InteropServices;

namespace Mordent.Core
{

    public unsafe class Database : IDisposable
    {
        private object __databaseLock = new object(); // used for locking

        private bool disposedValue;
        private string _filePath;

        private IDbPageManager _pageManager;
        //public delegate void DbAction(Span<DbPage> pages);

        public Database(string filePath, bool initNew)
        {

            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException($"'{nameof(filePath)}' cannot be null or empty.", nameof(filePath));


            _filePath = filePath;

            _pageManager = new MemoryMappedDbPageManager(filePath, initNew);

            if (initNew)
                InitDb();
            else
                CheckDb();
        }


        private void CheckDb()
        {
            var pages = _pageManager;
            if (pages[0, 3].FileHeader.Tag != FileHeaderPayload.MordentDataTag)
                throw new InvalidOperationException("Invalid file format tag found");
            if (pages[0, 0].Header.Type != DbPageType.FreeSpace)
                throw new InvalidOperationException("Invalid first page format");
            if (pages[0, 1].Header.Type != DbPageType.GlobalAllocationMap)
                throw new InvalidOperationException("Invalid 2nd page format");
            if (pages[0, 2].Header.Type != DbPageType.SharedAllocationMap)
                throw new InvalidOperationException("Invalid 3rd page format");
            if (pages[0, 3].Header.Type != DbPageType.FileHeader)
                throw new InvalidOperationException("Invalid 4th page format");
            if (pages[0, 4].Header.Type != DbPageType.Heap)
                throw new InvalidOperationException("Invalid 5th page format");
            if (pages[0, 5].Header.Type != DbPageType.Heap)
                throw new InvalidOperationException("Invalid 5th page format");

        }

        private void InitDb()
        {
            // pages[0] == PF
            // pages[1] == GAM
            // pages[2] == SGAM
            // pages[3] == file header
            // pages[4] is the Tables table storage
            var pages = _pageManager;
            var tablesTablePageId = pages.AllocatePage();
            pages[tablesTablePageId].InitAsHeap();
            pages.AddHeapRow(tablesTablePageId, TableHelper.BuildTablesTable());

            var fieldsTablePageId = pages.AllocatePage();
            pages[fieldsTablePageId].InitAsHeap();
            pages.AddHeapRow(tablesTablePageId, new Table(FieldsTableId, fieldsTablePageId, fieldsTablePageId) { Name = "Fields" });

            //pages[6].InitAsHeap();
            //pages.MarkPageMixedExtent(6);

            pages.AddHeapRow(fieldsTablePageId, new TableField(TablesTableId, "Id", "System.Guid"));
            pages.AddHeapRow(fieldsTablePageId, new TableField(TablesTableId, "Name", "System.String"));
            pages.AddHeapRow(fieldsTablePageId, new TableField(TablesTableId, "FirstPage", "System.Int32"));
            pages.AddHeapRow(fieldsTablePageId, new TableField(TablesTableId, "LastPage", "System.Int32"));

            pages.AddHeapRow(fieldsTablePageId, new TableField(FieldsTableId, "Id", "System.Guid"));
            pages.AddHeapRow(fieldsTablePageId, new TableField(FieldsTableId, "TableId", "System.Guid"));
            pages.AddHeapRow(fieldsTablePageId, new TableField(FieldsTableId, "Name", "System.String"));
            pages.AddHeapRow("Fields", new TableField(FieldsTableId, "Type", "System.String"));

            //_acc.Flush();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    //_acc.Dispose();
                    //_fileMmf.Dispose();
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


        public static readonly Guid TablesTableId = new("D42F19E7-93F6-4D48-884C-7EB72685A6B2");
        private static readonly Guid FieldsTableId = new("7E72F3F2-089E-4CBE-816A-7A6579A8ABE0");
    }
}

