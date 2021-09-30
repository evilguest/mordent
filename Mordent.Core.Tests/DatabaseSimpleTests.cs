using System;
using System.IO;
using Xunit;

namespace Mordent.Core.Tests
{
    public class DatabaseSimpleTests
    {
        [Fact]
        public void TestFileInit()
        {
            var fname = System.IO.Path.GetTempFileName();
//            File.Delete(fname);
            using (new Database(fname, true)); // create db
            using (new Database(fname, false)); // verify it reads back
        }
    }
}
