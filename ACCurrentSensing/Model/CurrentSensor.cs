using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using Reactive.Bindings.Extensions;

namespace ACCurrentSensing.Model
{
    [Notify]
    public class CurrentSensor : INotifyPropertyChanged, IDisposable
    {
        public static async Task<CurrentSensor> FromSensorInformationAsync(SensorInformation sensorInformation, CancellationToken cancellationToken)
        {
            var device = await CurrentSensorDevice.FromSensorInformationAsync(sensorInformation, cancellationToken);
            return new CurrentSensor(sensorInformation, device);
        }

        private Task initializationTask;
        private CancellationTokenSource initializationCancel;
        private CompositeDisposable disposables = new CompositeDisposable();

        [NonNotify]
        public SensorInformation SensorInformation { get; }
        public CurrentSensorDevice Device { get { return device; } private set { SetProperty(ref device, value, devicePropertyChangedEventArgs); } }

        public float Current { get { return current; } private set { SetProperty(ref current, value, currentPropertyChangedEventArgs); } }

        private void SubscribeCurrent()
        {
            this.Device
                .ObserveProperty(self => self.Current)
                .Do(current => this.Current = current)
                .CatchIgnore()
                .Subscribe()
                .AddTo(this.disposables);
        }

        private CurrentSensor(SensorInformation sensorInformation, CurrentSensorDevice device)
        {
            this.SensorInformation = sensorInformation;
            this.Device = device;
            this.SubscribeCurrent();
        }

        public CurrentSensor(SensorInformation sensorInformation)
        {
            this.SensorInformation = sensorInformation;

            this.initializationCancel = new CancellationTokenSource();
            this.initializationTask = Task.Run(async () =>
            {
                try
                {
                    this.Device = await CurrentSensorDevice.FromSensorInformationAsync(sensorInformation, this.initializationCancel.Token);
                    this.disposables.Add(this.Device);
                }
                catch(Exception)
                {
                    // Maybe the sensor device was not found.
                }
                this.SubscribeCurrent();
            }, this.initializationCancel.Token);
        }

        public void Dispose()
        {
            this.initializationCancel?.Cancel();
            this.initializationTask?.Wait();
            this.initializationTask = null;
            this.initializationCancel?.Dispose();
            this.initializationCancel = null;

            this.disposables?.Dispose();
            this.disposables = null;
        }

        #region NotifyPropertyChangedGenerator

        public event PropertyChangedEventHandler PropertyChanged;

        private CurrentSensorDevice device;
        private static readonly PropertyChangedEventArgs devicePropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(Device));
        private float current;
        private static readonly PropertyChangedEventArgs currentPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(Current));

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
