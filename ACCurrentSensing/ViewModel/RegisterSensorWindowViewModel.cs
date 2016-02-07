using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ACCurrentSensing.ViewModel
{
    public class RegisterSensorWindowViewModel : IDisposable
    {
        private CompositeDisposable disposables = new CompositeDisposable();

        public ReadOnlyReactiveCollection<SensorInformationViewModel> RegisteredSensors { get; }
        public ReadOnlyReactiveCollection<SensorInformationViewModel> UnregisteredSensors { get; }

        public ReactiveProperty<bool> IsUnregisteredSensorsShown { get; }
        public ReactiveCommand ShowUnregisteredSensorsCommand { get; }
        
        public ReactiveCommand UpdateUnregisteredSensorsCommand { get; }

        private class TestCommand : ICommand
        {
            public event EventHandler CanExecuteChanged;

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public void Execute(object parameter)
            {
                
            }
        }
        public RegisterSensorWindowViewModel()
        {
            var sensorRegistry = ((App)App.Current).SensorRegistry;
            this.RegisteredSensors = sensorRegistry.Sensors.ToReadOnlyReactiveCollection(
                sensorInformation => new SensorInformationViewModel(
                    sensorInformation,
                    vm => { },
                    vm => { sensorRegistry.UnregisterSensor(sensorInformation); }));

            this.UpdateUnregisteredSensorsCommand = (new ReactiveCommand()).AddTo(this.disposables);

            var sensorEnumerator = new Model.SensorEnumerator();
            var unregisteredSensorsObservable = this.UpdateUnregisteredSensorsCommand
                .Select(_ => sensorEnumerator.Enumerate().SubscribeOnUIDispatcher())
                .Switch()
                .Where(sensor => !sensorRegistry.IsRegistered(sensor))
                .Select(sensor => new SensorInformationViewModel(
                    sensor, 
                    vm => { sensorRegistry.RegisterSensor(sensor); this.IsUnregisteredSensorsShown.Value = false; },
                    vm => { }));


            this.UnregisteredSensors = unregisteredSensorsObservable.ToReadOnlyReactiveCollection(this.UpdateUnregisteredSensorsCommand.ToUnit()).AddTo(this.disposables);

            (this.IsUnregisteredSensorsShown = new ReactiveProperty<bool>()).AddTo(this.disposables);
            this.ShowUnregisteredSensorsCommand = new ReactiveCommand();
            this.ShowUnregisteredSensorsCommand
                .Do(_ => { this.UpdateUnregisteredSensorsCommand.Execute(); this.IsUnregisteredSensorsShown.Value = true; })
                .CatchIgnore()
                .Subscribe()
                .AddTo(this.disposables);
        }

        public void Dispose()
        {
            try
            {
                ((App)App.Current).SaveSensorRegistry();
            }
            catch(Exception)
            {
            }

            this.disposables?.Dispose();
            this.disposables = null;
        }
    }
}
