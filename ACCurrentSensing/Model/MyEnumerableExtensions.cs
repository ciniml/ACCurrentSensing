using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reactive.Bindings.Extensions;

namespace ACCurrentSensing.Model
{
    static class MyEnumerableExtensions
    {
        public static IEnumerable<OldNewPair<T>> Pairwise<T>(this IEnumerable<T> enumerable)
        {
            using (var enumerator = enumerable.GetEnumerator())
            {
                if (!enumerator.MoveNext()) yield break;
                var oldValue = enumerator.Current;
                while (enumerator.MoveNext())
                {
                    var newValue = enumerator.Current;
                    yield return new OldNewPair<T>(oldValue, newValue);
                    oldValue = newValue;
                }
            }
        }
    }
}
