namespace Mordent.Core
{
    public interface IDbPageManager : IPageManager<DbPageId>
    {
        public void AttachFile(short fileNo, string filePath);
        public void CreateFile(short fileNo, string filePath);
        public long AvailablePages { get; }
        public ref DbPage this[ushort fileNo, int pageNo] => ref this[new DbPageId(fileNo, pageNo)];
    }
}
