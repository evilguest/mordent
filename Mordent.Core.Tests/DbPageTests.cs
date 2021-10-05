using System;
using Xunit;

namespace Mordent.Core.Tests
{
    public class DbPageTests
    {
        [Fact]
        public void TestFileHeaderPayload()
        {
            var p = new DbPage();
            p.InitAsFileHeaderPage();
            Assert.Equal(DbPageType.FileHeader, p.Header.Type);
            Assert.Equal(DbPage.FileHeaderPayload.MordentDataTag, p.FileHeader.Tag);
            Assert.Equal(int.MaxValue, p.FileHeader.MaxPagesGrowth);
            Assert.Equal(new Lsn(0), p.FileHeader.RedoStartLSN);
            Assert.Equal((uint)42, p.FileHeader.Type);
            Assert.Equal((uint)1, p.FileHeader.Version);
        }
        [Fact]
        public void TestFreeSpacePayload()
        {
            var p = new DbPage();
            p.InitAsPfPage();
            Assert.Equal(DbPageType.FreeSpace, p.Header.Type);
            Assert.Equal(0, p.PageAlloc.FindFirstNonAllocatedPage(0));
            Assert.Equal(0, p.PageAlloc.FindFirstNonAllocatedPage(1));
            p.PageAlloc[0] |= PageAllocationStatus.PageAllocatedMask;
            p.PageAlloc[1] |= PageAllocationStatus.PageAllocatedMask;
            p.PageAlloc[2] |= PageAllocationStatus.PageAllocatedMask;
            p.PageAlloc[3] |= PageAllocationStatus.PageAllocatedMask;
            Assert.Equal(PageAllocationStatus.PageAllocatedMask, p.PageAlloc[0]);
            Assert.Equal(4, p.PageAlloc.FindFirstNonAllocatedPage(0));
            p.PageAlloc[4] |= PageAllocationStatus.PageAllocatedMask;
            p.PageAlloc[5] |= PageAllocationStatus.PageAllocatedMask;
            p.PageAlloc[6] |= PageAllocationStatus.PageAllocatedMask;
            p.PageAlloc[7] |= PageAllocationStatus.PageAllocatedMask;
            Assert.Equal(-1, p.PageAlloc.FindFirstNonAllocatedPage(0));
        }

        [Fact]
        public void TestGam()
        {
            var p = new DbPage();
            p.InitAsGamPage();
            Assert.Equal(DbPageType.GlobalAllocationMap, p.Header.Type);
            Assert.Equal(0, p.ExtentAlloc.FindFirstFreeExtent());
            p.ExtentAlloc[0] = false;
            Assert.Equal(1, p.ExtentAlloc.FindFirstFreeExtent());
            p.ExtentAlloc[2] = false;
            Assert.Equal(1, p.ExtentAlloc.FindFirstFreeExtent());
            p.ExtentAlloc[1] = false;
            Assert.Equal(3, p.ExtentAlloc.FindFirstFreeExtent());
            for (var e = 0; e < DbPage.ExtentAllocPayload.ExtentsCapacity; e++)
                p.ExtentAlloc[e] = false;
            Assert.Equal(-1, p.ExtentAlloc.FindFirstFreeExtent());
        }
        [Fact]
        public void TestSGam()
        {
            var p = new DbPage();
            p.InitAsSGamPage();
            Assert.Equal(DbPageType.SharedAllocationMap, p.Header.Type);
            Assert.Equal(-1, p.ExtentAlloc.FindFirstFreeExtent()); // by default, SGAM map is empty.
            p.ExtentAlloc[2] = true;
            Assert.Equal(2, p.ExtentAlloc.FindFirstFreeExtent());
            p.ExtentAlloc[0] = true;
            Assert.Equal(0, p.ExtentAlloc.FindFirstFreeExtent());
            p.ExtentAlloc[1] = true;
            Assert.Equal(0, p.ExtentAlloc.FindFirstFreeExtent());
        }

        [Fact]
        public void TestHeap()
        {
            var p = new DbPage();
            p.InitAsHeap();
            #region Testing the freshly initied page
            Assert.Equal(DbPageType.Heap, p.Header.Type);
            Assert.Equal(0, p.RowData.Header.DataCount);
            Assert.Equal(DbPageId.None, p.RowData.Header.NextPageId);
            Assert.Equal(DbPageId.None, p.RowData.Header.PrevPageId);
            Assert.Equal(DbPage.RowDataPayload.Capacity-2, p.RowData.FreeSpace);
            #endregion

            #region Testing behavior
            Assert.Throws<ArgumentOutOfRangeException>("slotNo", () => p.RowData.RemoveRow(1));
            Assert.Throws<ArgumentOutOfRangeException>("slotSize", ()=>p.RowData.AddSlot((ushort)(p.RowData.FreeSpace+1)));
            Assert.Equal(0, p.RowData.AddSlot((ushort)p.RowData.FreeSpace)); // testing that it fits
            Assert.Equal(DbPage.RowDataPayload.Capacity - 2, p.RowData.GetSlotSpan(0).Length);
            Assert.Throws<ArgumentOutOfRangeException>("slotSize", () => p.RowData.AddSlot(0));
            p.RowData.RemoveRow(0);
            Assert.Equal(0, p.RowData.Header.DataCount);
            Assert.Equal(DbPage.RowDataPayload.Capacity - 2, p.RowData.FreeSpace);
            ushort slotSize1 = 100;
            Assert.Equal(0, p.RowData.AddSlot(slotSize1));
            ushort slotSize2 = 200;
            Assert.Equal(1, p.RowData.AddSlot(slotSize2));
            Assert.Equal(DbPage.RowDataPayload.Capacity - slotSize1 - slotSize2 - 6, p.RowData.FreeSpace);
            Assert.Equal(slotSize1, p.RowData.GetSlotSpan(0).Length);
            Assert.Equal(slotSize2, p.RowData.GetSlotSpan(1).Length);
            p.RowData.RemoveRow(0);
            Assert.Equal(DbPage.RowDataPayload.Capacity - slotSize2 - 4, p.RowData.FreeSpace);
            #endregion
        }
    }
}
