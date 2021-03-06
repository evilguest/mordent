using System;
using System.Runtime.InteropServices;

namespace Mordent.Core
{
    public partial struct DbPage
    {
        public void InitAsLogPage()
        {
            Header.Type = DbPageType.Log;
            Log.Reset();
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct LogPayload
        {
            public const int Capacity = DbPage.Size - DbPageHeader.Size - sizeof(short);
            public short Boundary { get; private set; }
            private fixed byte _payload[Capacity];

            public Span<Byte> AvailableArea => MemoryMarshal.CreateSpan(ref _payload[0], Boundary);
            public ReadOnlySpan<Byte> AllData => MemoryMarshal.CreateReadOnlySpan(ref _payload[0], Capacity);
            public short AvailableBytes => (short)(Boundary - sizeof(short));

            internal void Reset()
            {
                Boundary = Capacity;
            }
            internal void Write(ReadOnlySpan<byte> data)
            {
                var l = (short)data.Length;
                var recPos = (short)(Boundary - l - sizeof(short));
                var a = AvailableArea.Slice(recPos);
                MemoryMarshal.Write(a, ref l);
                a = a.Slice(sizeof(short));
                data.CopyTo(a);
                Boundary = recPos;
            }

            internal byte[] Read(short currentPos)
            {
                var a = AllData.Slice(currentPos);
                var l = a.Read<short>(); 
                var r = new byte[l];
                a[..l].CopyTo(r);
                return r;
            }
        }
    }
}