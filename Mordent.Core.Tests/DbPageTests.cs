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
    }
}
