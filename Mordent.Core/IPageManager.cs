using System;
using System.Threading.Tasks;

namespace Mordent.Core
{
    public interface IPageManager<TPageId>
        where TPageId: struct, IEquatable<TPageId>
    {
        public ref DbPage this[TPageId pageId] { get; }
        public TPageId AllocatePage(TPageId nearTo) => this.AllocatePage();
        public TPageId AllocatePage();
        public void FreePage(TPageId pageId);
    }
}
