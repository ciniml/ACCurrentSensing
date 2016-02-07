using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ACCurrentSensing.Model
{
    class TraceObjectTracker<T>
    {
        private static long id = 1;

        public static long NewId() { return Interlocked.Increment(ref id); }
    }
}
