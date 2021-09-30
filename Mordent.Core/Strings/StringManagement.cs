using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Mordent.Core
{
    [StructLayout(LayoutKind.Sequential, Pack = 2, Size = 12)]
    public struct StringHeader
    {
        public short FirstSegLength;
        public StringSegmentHeader FirstSegmentHeader;

        internal void Init(int length, short firstSegLength)
        {
            FirstSegmentHeader.Init(firstSegLength, DbPageId.None);
        }
        internal void Init(int length) => Init(length, (short)length);
    }

    [StructLayout(LayoutKind.Explicit, Pack = 2, Size = 8)]
    public struct StringSegmentHeader//: IEquatable<StringSegmentHeader>
    {
        [FieldOffset(0)]
        private ulong allData;
        [FieldOffset(0)]
        public DbPageId PageId;
        [FieldOffset(6)]
        public short SegmentLen;
        public void Init(short segmentLength, DbPageId pageId)
            => (PageId, SegmentLen) = (pageId, segmentLength);


        public int TotalLen => Marshal.SizeOf<StringSegmentHeader>() + SegmentLen;
        public bool HasMore { get => PageId != DbPageId.None; }
        //public static readonly StringSegmentHeader Null = new();
        //public static bool operator ==(StringSegmentHeader left, StringSegmentHeader right) => left.allData == right.allData;
        //public static bool operator !=(StringSegmentHeader left, StringSegmentHeader right) => left.allData != right.allData;
        //public override bool Equals(object obj) => obj is StringSegmentHeader ssh && ssh == this;
        //public override int GetHashCode() => PageNo ^ SegmentLen ^ (FileNo << 16);

        //public bool Equals(StringSegmentHeader other) => allData == other.allData;

        public unsafe ReadOnlySpan<char> Segment { 
            get
            {
                fixed(void* pData = &this)
                    return new ReadOnlySpan<char>((byte*)pData + sizeof(StringSegmentHeader), SegmentLen);
            } 
        }
    }

    public static class SpanMarshalHelper
    {
        public static Span<byte> Read<T>(ref this Span<byte> span, out T value)
            where T: unmanaged
        {
            value = MemoryMarshal.Read<T>(span);
            return span.Slice(Marshal.SizeOf<T>());
        }
        public static void Read<T>(ref this ReadOnlySpan<byte> span, out T value)
            where T : unmanaged
        {
            value = MemoryMarshal.Read<T>(span);
            span = span.Slice(Marshal.SizeOf<T>());
        }
        public static T Read<T>(ref this ReadOnlySpan<byte> span)
            where T : unmanaged
        {
            var value = MemoryMarshal.Read<T>(span);
            span = span.Slice(Marshal.SizeOf<T>());
            return value;
        }
        public static void Write<T>(ref this Span<byte> span, T value)
            where T: unmanaged
        {
            MemoryMarshal.Write(span, ref value);
            span = span.Slice(Marshal.SizeOf<T>());
        }
    }
    /// <summary>
    /// This class is used to read and write all strings
    /// </summary>
    public static class StringHelper
    {

        /// <summary>
        /// Reads a string from database, starting with the first string segment
        /// </summary>
        /// <param name="pages"></param>
        /// <param name="head"></param>
        /// <returns></returns>
        public static string ReadString(this IDbPageManager pages, ReadOnlySpan<byte> head)
        {
            head.Read(out ushort length);
            switch (length & 0xC000)
            {
                case 0x0000: // short ASCII string
                    return Encoding.ASCII.GetString(head.Slice(0, length));
                case 0x8000: // short UTF8 string
                {
                    head.Read(out ushort bytes);
                    return Encoding.UTF8.GetString(head.Slice(0, bytes));
                }
                case 0x4000: // long string with ASCII first segment
                {
                    head.Read(out int fullLength);
                    head.Read(out DbPageId stringHeapRoot);
                    var sb = new StringBuilder(fullLength);
                    sb.Append(Encoding.ASCII.GetString(head.Slice(0, length & 0x3FFF)));
                    ReadString(pages, stringHeapRoot, sb);
                    return sb.ToString();
                }
                case 0xC000: // long string with UTF8 first segment
                {
                    head.Read(out int fullLength);
                    head.Read(out DbPageId stringHeapRoot);
                    head.Read(out ushort bytes);
                    var sb = new StringBuilder(fullLength);
                    sb.Append(Encoding.UTF8.GetString(head.Slice(0, bytes)));
                    ReadString(pages, stringHeapRoot, sb);
                    return sb.ToString();
                }
                default: throw new Exception("Unreachable code reached");
            }
        }

        private static void ReadString(IDbPageManager pages, DbPageId stringHeapRoot, StringBuilder sb)
        {
            throw new NotImplementedException();
        }

        public static short WriteString(this IDbPageManager pages, Span<byte> storageSpace, string text)
        {
            // check for Unicode
            if (text.Any(c => c > 127))
                throw new NotImplementedException("Unicode writing isn't supported yet");
            if (text.Length + 2 > storageSpace.Length)
                throw new NotImplementedException("Long strings support isn't implemented yet");
            storageSpace.Write((ushort)text.Length);
            Encoding.ASCII.GetEncoder().Convert(text, storageSpace, true, out var _, out _, out var __);
            return (short)text.Length;
/*            var value = text.AsSpan();
            var segHead = MemoryMarshal.AsRef<StringHeader>(storageSpace).FirstSegmentHeader.AsRef();
            var fittingChars = (short)((storageSpace.Length - Marshal.SizeOf<StringHeader>()) / sizeof(char));
            fittingChars = fittingChars > value.Length ? (short)value.Length : fittingChars;
            MemoryMarshal.AsRef<StringHeader>(storageSpace).Init(text.Length, (short)fittingChars);
            var charSpace = MemoryMarshal.Cast<byte, char>(storageSpace.Slice(Marshal.SizeOf<StringHeader>()));
            value.Slice(0, fittingChars).CopyTo(charSpace);
            value = value.Slice(fittingChars);
            DbPageId page = new();
            while (value.Length > 0)
            {
                page = pages.AllocRowDataPage(page); // todo: pass the original page no to keep the chunks near the head
                segHead.Value.PageNo = page; // passing the pointer further
                var storeSize = (short)(Marshal.SizeOf<StringSegmentHeader>() + sizeof(char) * value.Length);
                if (storeSize > pages[page].RowData.FreeSpace)
                    storeSize = pages[page].RowData.FreeSpace;

                pages[page].RowData.AddSlot(storeSize);
                segHead = pages[page].RowData.GetSlotAs<StringSegmentHeader>(0).AsRef();
                storageSpace = pages[page].RowData.GetSlotSpan(0);
                fittingChars = (short)((storageSpace.Length - Marshal.SizeOf<StringSegmentHeader>()) / sizeof(char));
                fittingChars = fittingChars > value.Length ? (short)value.Length : fittingChars;
                segHead.Value.Init(fittingChars, 0, 0);
                charSpace = MemoryMarshal.Cast<byte, char>(storageSpace.Slice(Marshal.SizeOf<StringSegmentHeader>()));
                value.Slice(0, fittingChars).CopyTo(charSpace);
                value = value.Slice(fittingChars);
            }*/
        }
    }
}
