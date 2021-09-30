using System;

namespace Mordent.Core
{
    public class Table : Named<Table.Fixed>
    {
        public struct Fixed
        {
            public Guid Id;
            public DbPageId FirstPage;
            public DbPageId LastPage;
        }
        public Table(in Fixed value) => Value = value;
        public Table(Guid id, DbPageId firstPage, DbPageId lastPage) 
            => (Value.Id, Value.FirstPage, Value.LastPage) = (id, firstPage, lastPage);
        public ref Guid Id => ref Value.Id;
        public ref DbPageId FirstPage => ref Value.FirstPage;
        public ref DbPageId LastPage => ref Value.LastPage;
    }
}
