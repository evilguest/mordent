using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Mordent.Core.Tests
{
    public class LogTest
    {
        [Theory]
        [InlineData(25, 1000)]
        public void TestLog(int recCount, int recSize)
        {

            string filePath = System.IO.Path.GetTempFileName();
            using (var logFile = new LogFile(filePath))
            {
                var rec = new byte[recSize];
                for (var i = 0; i < recCount; i++)
                {
                    var r = "Record #" + i;
                    var s = rec.AsSpan();
                    s.Write(Encoding.UTF8.GetByteCount(r));
                    Encoding.UTF8.GetEncoder().Convert(r, s, true, out _, out _, out _);
                    logFile.Append(rec);
                }

                logFile.Flush(new Lsn(recCount / 2));
                Assert.Equal(recCount, logFile.Records.Count());
                logFile.Flush(new Lsn(recCount));
                Assert.Equal(recCount, logFile.Records.Count());
            }
            using (var logFile = new LogFile(filePath))
                Assert.Equal(recCount, logFile.Records.Count());
        }
    }
}
