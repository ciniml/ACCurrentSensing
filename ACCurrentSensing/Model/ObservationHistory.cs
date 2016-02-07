using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Reactive.Bindings;

namespace ACCurrentSensing.Model
{
    public static class ObservationHistoryExtensions
    {
        public static ObservationHistory<T> ToObservationHistory<T>(this IObservable<T> observable, int maxHistoryCount)
        {
            return new ObservationHistory<T>(observable, maxHistoryCount);
        }
        public static ObservationHistory<T> ToObservationHistory<T>(this IObservable<T> observable, IObservable<Unit> removeOldObservable, Func<T, bool> isOldPredicate  )
        {
            return new ObservationHistory<T>(observable, removeOldObservable, isOldPredicate);
        }

        public static IObservable<CollectionChanged<T>> ObserveHistory<T>(this IObservable<T> observable, int maxHistoryCount)
        {
            int count = 0;
            return observable.SelectMany(value =>
            {
                if (count < maxHistoryCount)
                {
                    return new[]
                    {
                        CollectionChanged<T>.Add(count++, value)
                    };
                }
                else
                {
                    return new[]
                    {
                        CollectionChanged<T>.Remove(0, default(T)),
                        CollectionChanged<T>.Add(maxHistoryCount - 1, value),
                    };
                }
            });
        }


    }

    /// <summary>
    /// History of observed values.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ObservationHistory<T> : IDisposable
    {
        private IDisposable subscription;
        private ConcurrentQueue<T> queue;
        private readonly Subject<Unit> historyChangedSubject;

        
        public IObservable<Unit> HistoryChanged
        {
            get { return this.historyChangedSubject.AsObservable(); }
        }

        public ObservationHistory(IObservable<T> observable, int maxHistoryCount)
        {
            this.queue = new ConcurrentQueue<T>();
            this.historyChangedSubject = new Subject<Unit>();

            this.subscription = observable.Subscribe(value =>
            {
                if (this.queue.Count > maxHistoryCount)
                {
                    T dummy;
                    this.queue.TryDequeue(out dummy);
                }
                this.queue.Enqueue(value);
                this.historyChangedSubject.OnNext(Unit.Default);
            });
        }

        /// <summary>
        /// Construct an ObervationHistory which stores a new value when newItemObservable generates a new value and remove values when removeOldObservable raises.
        /// </summary>
        /// <param name="newItemObservable"></param>
        /// <param name="removeOldObservable"></param>
        /// <param name="isOldPredicate"></param>
        public ObservationHistory(IObservable<T> newItemObservable, IObservable<Unit> removeOldObservable, Func<T, bool> isOldPredicate )
        {
            this.queue = new ConcurrentQueue<T>();
            this.historyChangedSubject = new Subject<Unit>();

            this.subscription = Observable
                .Merge(new[]
                    {
                        newItemObservable.Select(item => new {Item=item, HasItem=true}),
                        removeOldObservable.Select(_ => new {Item=default(T), HasItem=false})
                    })
                .Subscribe(item =>
                    {
                        T oldElement;
                        while (this.queue.TryPeek(out oldElement) && isOldPredicate(oldElement))
                        {
                            this.queue.TryDequeue(out oldElement);
                        }

                        if (item.HasItem)
                        {
                            this.queue.Enqueue(item.Item);
                        }
                        this.historyChangedSubject.OnNext(Unit.Default);
                    });
        }

        /// <summary>
        /// Get values in this moment.
        /// </summary>
        /// <returns></returns>
        public T[] GetHistory()
        {
            return this.queue.ToArray();
        } 
        public void Dispose()
        {
            this.subscription.Dispose();
            this.subscription = null;

            while (!this.queue.IsEmpty)
            {
                T dummy;
                this.queue.TryDequeue(out dummy);
            }
            this.queue = null;
        }
    }
}
