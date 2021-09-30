namespace Mordent.Core
{
    public interface IFilePageManager : IPageManager<int>
    {
        //public void InitAllocationPages(int oldPagesCount);
        public long DataSize
        {
            get => PageCount << DbPage.SizeLog; // * DbPage.Size;
            set => PageCount = (int)(value >> DbPage.SizeLog) + ((value & DbPage.SizeMask) != 0 ? 1 : 0);
        }
        public int PageCount
        {
            get => ExtentsCount * DbPage.ExtentAllocPayload.PagesPerExtent;
            set => ExtentsCount = MemoryMappedDbPageManager.PagesToExtents(value);
        }

        public int ExtentsCount { get; set; }
        public int AvailablePages { get; }
    }


}
