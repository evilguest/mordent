using System;
using System.Diagnostics;

namespace Mordent.Core
{
    /*
    public unsafe readonly ref struct Ref<T>
        where T : unmanaged
    {
        private readonly T* _ref;
        //private Ref(void* ptr) => _ref = (T*)ptr;
        public Ref(ref T reference) => _ref = (T*)Unsafe.AsPointer(ref reference);
        public ref T Value => ref *_ref;
        //public void Reset(ref T reference)=> _ref = (T*)Unsafe.AsPointer(ref reference);
    }
    public static class Ref
    {
        public static Ref<T> AsRef<T>(this ref T r) where T : unmanaged => new Ref<T>(ref r);
    }
    */
    public static class HeapHelper
    {
        public static DbRowId AddHeapRow(this IDbPageManager pages, DbPageId pageId, IDbSerializable rowData)
        {
            Debug.Assert(pages[pageId].RowData.FreeSpace >= rowData.FixedDataSize, "Not enough space to store requested data on this page");
            if (rowData.TotalDataSize > pages[pageId].RowData.FreeSpace)
            {
                // TODO: implement
                throw new NotImplementedException("The row overflow is not implemented yet");
            }
            var rowId = new DbRowId(pageId, pages[pageId].RowData.AddSlot((ushort)rowData.TotalDataSize));
            pages.WriteHeapRow(rowId, rowData);
            return rowId;
        }
        public static DbRowId AddHeapRow(this IDbPageManager pages, string tableName, IDbSerializable rowData)
        {
            var t = pages.FindTable(tableName);
            Debug.Assert(t != null, $"Couldn't find table {tableName}");
            var size = rowData.TotalDataSize;
            var pageId = t.FirstPage;
            while (pages[pageId].RowData.FreeSpace < size)
            {
                if (pages[pageId].RowData.Header.NextPageId == DbPageId.None)
                {
                    var newPageNo = pages.AllocRowDataPage(pageId);
                    pages[pageId].RowData.Header.NextPageId = newPageNo;
                    pageId = newPageNo;
                    break;
                }
                pageId = pages[pageId].RowData.Header.NextPageId;
            }
            return AddHeapRow(pages, pageId, rowData);
        }

        public static void WriteHeapRow(this IDbPageManager pages, DbRowId rowId, IDbSerializable rowData)
        {
            // Writing a row offers multiple options:
            // 1. There is a minimum size this particular instance of row can occupy.
            //    It is the combined size of all the fixed-data fields plus the shortest representation of the varlen fields.
            // 1.1. The shortest representation of the varlen fields is either the in-row data (if the total byte length <= the overflow segment header size == 4+4 extra bytes.), 
            //  or just the header. We can assume the varlen fields always take 8 bytes min, as it does not seem to make much sense to pack them tighter than that. 
            // 2. We can store some of the varlen fields in-row, and do it partially. What should be the allocation strategy?
            // 2.A: spread even. If we are short of space, we split the X bytes remaining across N varlen fields giving X/N to each
            // 2.B: prefer in-row. Give as much space as possible to the shortest player; then, if any space left, give it to the next, etc, etc.
            // 2.C: eager approach: the first item grabs as much space as it needs.

            // check if page does have enough size to store the fixed data
            var dataSpan = pages[rowId.PageId].RowData.GetSlotSpan(rowId.SlotNo);
            dataSpan = dataSpan.Slice(rowData.Write(dataSpan, 0, pages)); // fixed data
            foreach (var item in rowData.DataItems)
                dataSpan = dataSpan.Slice(rowData.Write(dataSpan, item, pages));

        }

        public static void RemoveHeapRow(Span<DbPage> pages, DbRowId rowId)
        {
            Debug.Assert(pages[rowId.PageNo].Header.Type == DbPageType.Heap);
            pages[rowId.PageNo].RowData.RemoveRow(rowId.SlotNo);
        }
    }
}

