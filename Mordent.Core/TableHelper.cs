using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Mordent.Core
{
    public static class TableHelper
    {
        public static IEnumerable<T> TableScan<T>(this IDbPageManager pages, string tableName, Scanner<T> selector, Predicate predicate)
        {
            var t = FindTable(pages, tableName);

            for (var pageId = t.FirstPage; pageId != DbPageId.None; pageId = pages[pageId].RowData.Header.NextPageId)
            {
                for (short rowNo = 0; rowNo < pages[pageId].RowData.Header.DataCount; rowNo++)
                {
                    var r = pages[pageId].RowData.GetSlotSpan(rowNo);
                    if (predicate(pages, r))
                        yield return selector(pages, r);
                }
            }
        }
        public static Table BuildTablesTable() => new Table(Database.TablesTableId, new DbPageId(0, 4), new DbPageId(0, 4)) { Name = "Tables" };

        public static Table FindTable(this IDbPageManager pages, string tableName)
        {
            if (tableName == "Tables")
                return BuildTablesTable();
            else
            {
                return pages.TableScan("Tables",
                    (pages, span) => new Table(span.Read<Table.Fixed>()) { Name = pages.ReadString(span) },
                    (pages, span) => pages.ReadString(span.Slice(Marshal.SizeOf<Table.Fixed>())) == tableName
                ).First();
            }
        }
        public delegate T Scanner<T>(IDbPageManager pages, ReadOnlySpan<byte> recordSpan);
        public delegate bool Predicate(IDbPageManager pages, ReadOnlySpan<byte> recordSpan);

    }
}

