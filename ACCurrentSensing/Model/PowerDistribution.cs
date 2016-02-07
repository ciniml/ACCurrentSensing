using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Reactive.Bindings;
using System.Reactive.Disposables;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive;

namespace ACCurrentSensing.Model
{
    [Notify]
    public class PowerDistribution : INotifyPropertyChanged, IDisposable
    {
        public float Capacity { get { return capacity; } set { SetProperty(ref capacity, value, capacityPropertyChangedEventArgs); } }
        public float WarningAlertCurrent { get { return warningAlertCurrent; } set { SetProperty(ref warningAlertCurrent, value, warningAlertCurrentPropertyChangedEventArgs); } }

        [NonNotify]
        public ReadOnlyReactiveCollection<CurrentSensor> Sensors { get; }
        public float TotalCurrent { get { return totalCurrent; } set { SetProperty(ref totalCurrent, value, totalCurrentPropertyChangedEventArgs); } }

        public bool IsWarningCondition { get { return isWarningCondition; } private set { SetProperty(ref isWarningCondition, value, isWarningConditionPropertyChangedEventArgs); } }
        public bool IsCriticalCondition { get { return isCriticalCondition; } private set { SetProperty(ref isCriticalCondition, value, isCriticalConditionPropertyChangedEventArgs); } }

        private CompositeDisposable disposables = new CompositeDisposable();

        public PowerDistribution(SensorRegistry registry)
        {
            this.Capacity = 30.0f;
            this.WarningAlertCurrent = this.Capacity * 0.75f;

            this.Sensors = registry.CurrentSensors.ToReadOnlyReactiveCollection(sensor => new CurrentSensor(sensor))
                .AddTo(this.disposables);

            var updateTotalCurrentSubject = new Subject<Unit>();
            updateTotalCurrentSubject
                .Select(_ => Observable.CombineLatest(this.Sensors.Select(sensor => sensor.ObserveProperty(self => self.Current)), currents => currents.Sum()))
                .Switch()
                .CatchIgnore()
                .Do(totalCurrent => this.TotalCurrent = totalCurrent)
                .Subscribe()
                .AddTo(this.disposables);
            this.Sensors.CollectionChangedAsObservable().Do(_ => updateTotalCurrentSubject.OnNext(Unit.Default)).Subscribe().AddTo(this.disposables);

            Observable.CombineLatest(
                    this.ObserveProperty(self => self.TotalCurrent),
                    this.ObserveProperty(self => self.WarningAlertCurrent),
                    (totalCurrent, warningAlertCurrent) => totalCurrent >= warningAlertCurrent)
                .CatchIgnore()
                .Do(isWarningCondition => this.IsWarningCondition = isWarningCondition)
                .Subscribe()
                .AddTo(this.disposables);

            Observable.CombineLatest(
                    this.ObserveProperty(self => self.TotalCurrent),
                    this.ObserveProperty(self => self.Capacity),
                    (totalCurrent, capacity) => totalCurrent >= capacity)
                .CatchIgnore()
                .Do(isCriticalCondition => this.IsCriticalCondition = isCriticalCondition)
                .Subscribe()
                .AddTo(this.disposables);

            //
            updateTotalCurrentSubject.OnNext(Unit.Default);
        }

        public void Dispose()
        {
            this.disposables?.Dispose();
            this.disposables = null;
        }

        #region NotifyPropertyChangedGenerator

        public event PropertyChangedEventHandler PropertyChanged;

        private float capacity;
        private static readonly PropertyChangedEventArgs capacityPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(Capacity));
        private float warningAlertCurrent;
        private static readonly PropertyChangedEventArgs warningAlertCurrentPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(WarningAlertCurrent));
        private float totalCurrent;
        private static readonly PropertyChangedEventArgs totalCurrentPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(TotalCurrent));
        private bool isWarningCondition;
        private static readonly PropertyChangedEventArgs isWarningConditionPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(IsWarningCondition));
        private bool isCriticalCondition;
        private static readonly PropertyChangedEventArgs isCriticalConditionPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(IsCriticalCondition));

        private void SetProperty<T>(ref T field, T value, PropertyChangedEventArgs ev)
        {
            //if (!System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, ev);
            }
        }

        #endregion
    }
}
