using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Mordent.Core
{
    [StructLayout(LayoutKind.Sequential, Size = DbPage.Size)]
    public unsafe ref struct DbRowDataPage
    {
        private const int Capacity = (DbPage.Size - DbPageHeader.Size) / sizeof(short);
        DbPageHeader _header;
        private fixed short _rowOffsets[Capacity];
        public short GetRowOffset(short rowNo)
        {
            Debug.Assert(rowNo <= _header.DataCount, $"Row access mismatch. Getting row #{rowNo} of {_header.DataCount}");
            return _rowOffsets[Capacity - 1 - rowNo];
        }
    }
}
