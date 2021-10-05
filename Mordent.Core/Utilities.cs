using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mordent.Core
{
    public static class Utilities
    {
        public static short Add(this short a, short b) => (short)(a + b);
        public static short Subtract(this short a, short b) => (short)(a - b);
        public static short DivBy(this short a, int b) => (short)(a / b);
        public static ushort Add(this ushort a, ushort b) => (ushort)(a + b);
        public static ushort Subtract(this ushort a, ushort b) => (ushort)(a - b);
        public static ushort DivBy(this ushort a, int b) => (ushort)(a / b);
    }
}
