using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Mordent.Core
{
    public class TableField : Named<TableField.Fixed>
    {
        public struct Fixed
        {
            public Guid Id;
            public Guid TableId;
        }
        public ref Guid Id => ref Value.Id;
        public ref Guid TableId => ref Value.TableId;
        public string Type { get; set; }
        public override IEnumerable<short> DataItems => base.DataItems.Append((short)2);
        public override int TotalDataSize => base.TotalDataSize + StringSize(Type);

        public TableField(Guid tableId, string name, string type)
        {
            (Value.Id, Value.TableId) = (Guid.NewGuid(), tableId);
            Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace.", nameof(name)) : name;
            Type = string.IsNullOrWhiteSpace(type) ? throw new ArgumentException($"'{nameof(type)}' cannot be null or whitespace.", nameof(type)) : type;
        }

        public override short Write(Span<byte> space, short dataItem, IDbPageManager allocator)
        {
            switch (dataItem)
            {
                case 2:
                    return allocator.WriteString(space, Type);
                default: return base.Write(space, dataItem, allocator);
            }
        }
    }
}
