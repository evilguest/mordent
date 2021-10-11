using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Mordent.Core.Tests
{
    public class BuffersTest
    {
        [Theory]
        [InlineData(3)]
        public void TestBuffers(int capacity)
        {
            var logName = System.IO.Path.GetTempFileName();
            var dataName = System.IO.Path.GetTempFileName();
            using (var logFile = new LogFile(logName))
            using (var filesManager = new FilesManager<PageFileManager, PageFileManagerFactory>(dataName))
            {
                var b = new Buffers(logFile, filesManager, capacity);
                Assert.Equal(capacity, b.Available);
                var t = b.PinNew();
                Assert.Equal(capacity - 1, b.Available);
                b.GetPage(t).InitAsGamPage();
                b.GetPage(t).ExtentAlloc[42] = false;
                b.SetModified(t, new DbTranId(1), new Lsn(1)); // this page will be saved
                b.Unpin(t);
                Assert.Equal(capacity, b.Available);
                for (var i = 0; i < capacity; i++)  // these pages won't be saved
                {
                    var p = b.PinNew();
                    Assert.Equal(capacity - i - 1, b.Available);
                    b.GetPage(p).InitAsHeap();
                    b.GetPage(p).RowData.AddSlot(42);
                }
            }
            // check the contents after save
            using (var logFile = new LogFile(logName))
            using (var filesManager = new FilesManager<PageFileManager, PageFileManagerFactory>(dataName))
            {
                var b = new Buffers(logFile, filesManager, capacity);
                Assert.Equal(capacity, b.Available);
                var t = b.Pin(new DbPageId(0, 0));
                Assert.Equal(DbPageType.GlobalAllocationMap, b.GetPage(t).Header.Type);
                Assert.False(b.GetPage(t).ExtentAlloc[42]);
                b.Unpin(t);
                for (var i = 0; i < capacity; i++)  // these pages won't be saved
                {
                    var p = b.Pin(new DbPageId(0, i + 1));
                    Assert.Equal(DbPageType.None, b.GetPage(t).Header.Type);
                }
            }
        }
        [Theory]
        [InlineData(3)]
        public void TestBuffersOverflow(int capacity)
        {
            var logName = System.IO.Path.GetTempFileName();
            var dataName = System.IO.Path.GetTempFileName();
            using (var logFile = new LogFile(logName))
            using (var filesManager = new FilesManager<PageFileManager, PageFileManagerFactory>(dataName))
            {
                var b = new Buffers(logFile, filesManager, capacity);
                Assert.Equal(capacity, b.Available);
                for (var i = 0; i < capacity; i++)  // these pages won't be saved
                    b.PinNew();
                Assert.ThrowsAny<Exception>(()=>b.PinNew()); // not found;
            }
        }
    }
}