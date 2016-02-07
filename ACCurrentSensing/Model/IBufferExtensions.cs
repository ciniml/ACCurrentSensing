using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace ACCurrentSensing.Model
{
    public static class BufferExtensions
    {
        public static int ToInt32(this IBuffer buffer)
        {
            return ToInt32(buffer, 0);
        }
        public static int ToInt32(this IBuffer buffer, int offset)
        {
            return BitConverter.ToInt32(buffer.ToArray(), offset);
        }
    }
}
