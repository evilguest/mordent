using System;

namespace Mordent.Core
{
    public class Table : Named<Table.Fixed>
    {
        public struct Fixed
        {
            public Guid Id;
            public int FirstPage;
            public int LastPage;
        }
        public Table(Guid id, int firstPage, int lastPage) 
            => (Value.Id, Value.FirstPage, Value.LastPage) = (id, firstPage, lastPage);
        public ref Guid Id => ref Value.Id;
        public ref int FirstPage => ref Value.FirstPage;
        public ref int LastPage => ref Value.LastPage;
    }
}
