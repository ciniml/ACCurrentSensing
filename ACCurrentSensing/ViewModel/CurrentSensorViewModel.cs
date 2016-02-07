using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using ACCurrentSensing.Model;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Diagnostics;

namespace ACCurrentSensing.ViewModel
{
    public class CurrentSensorViewModel : IDisposable
    {
        private readonly CompositeDisposable disposables = new CompositeDisposable();
        public Model.CurrentSensor Sensor { get; }
        public ReadOnlyReactiveProperty<bool> IsConnected { get;  }
        public ReadOnlyReactiveProperty<float> Current { get; private set; }
        public ReadOnlyReactiveProperty<float> BatteryLevel { get; private set; }
        public ReadOnlyReactiveProperty<string> Name { get; private set; }

        private IObservable<T> ObserveRecentDeviceProperty<T>(System.Linq.Expressions.Expression<Func<CurrentSensorDevice, T>> propertySelector)
        {
            return this.Sensor.ObserveProperty(self => self.Device)
                .Where(device => device != null)
                .Select(device => device.ObserveProperty(propertySelector))
                .Switch()
                .Do(value => Debug.WriteLine(value));
        }

        public CurrentSensorViewModel(CurrentSensor sensor)
        {
            this.Sensor = sensor;

            this.Name = this.Sensor.SensorInformation.ObserveProperty(self => self.Name).ToReadOnlyReactiveProperty();
            this.IsConnected = this.ObserveRecentDeviceProperty(self => self.IsConnected).ToReadOnlyReactiveProperty().AddTo(this.disposables);
            this.Current = this.ObserveRecentDeviceProperty(self => self.Current).ToReadOnlyReactiveProperty().AddTo(this.disposables);
            this.BatteryLevel = this.ObserveRecentDeviceProperty(self => self.BatteryLevel).ToReadOnlyReactiveProperty().AddTo(this.disposables);
        }


        public void Dispose()
        {
            this.disposables.Dispose();
        }
    }
}
