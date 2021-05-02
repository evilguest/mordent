using System.Runtime.InteropServices;

namespace Mordent.Core
{
    [StructLayout(LayoutKind.Sequential, Size = DbPage.Size)]
    public struct DbPage
    {
        public const int SizeLog = 14;
        public const int Size = 1 << SizeLog;
    }
}
