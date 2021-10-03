using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Mordent.Core.Tests
{
    public class PageFileManagerTests
    {
        [Fact]
        public void TestPageFileBasics()
        {
            var fname = System.IO.Path.GetTempFileName();
            var f = new PageFileManagerFactory().Create(fname);
            Assert.Equal(0, f.PageCount);

            var p = new DbPage();
            Assert.Throws<ArgumentOutOfRangeException>("pageNo", () => f.ReadPage(0, ref p));

            f.AddPage();
            Assert.Equal(1, f.PageCount);
            p.InitAsHeap();
            p.RowData.AddSlot(100);
            var s = p.RowData.GetSlotSpan(0);
            s.Write(42);
            f.WritePage(0, ref p);
            var r = new DbPage();
            f.ReadPage(0, ref r);

            
            Assert.True(p.Equals(r));
        }
    }
}
