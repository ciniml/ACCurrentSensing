using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACCurrentSensing.ViewModel
{
    public class SensorInformationViewModel : IDisposable
    {
        private CompositeDisposable disposables = new CompositeDisposable();

        public ReactiveProperty<bool> IsSelected { get; }
        public ReactiveProperty<Model.SensorKind> Kind { get; }
        public ReactiveProperty<string> Name { get; }

        public ReactiveCommand AddCommand { get; }
        public ReactiveCommand RemoveCommand { get; }

        public SensorInformationViewModel(Model.SensorInformation sensorInformation, Action<SensorInformationViewModel> onAddCommand, Action<SensorInformationViewModel> onRemoveCommand)
        {
            this.IsSelected = new ReactiveProperty<bool>().AddTo(this.disposables);
            this.Kind = sensorInformation.ToReactivePropertyAsSynchronized(self => self.Kind).AddTo(this.disposables);
            this.Name = sensorInformation.ToReactivePropertyAsSynchronized(self => self.Name).AddTo(this.disposables);
            this.AddCommand = new ReactiveCommand().AddTo(this.disposables);
            this.AddCommand.Do(_ => onAddCommand(this)).CatchIgnore().Subscribe().AddTo(this.disposables);
            this.RemoveCommand = new ReactiveCommand().AddTo(this.disposables);
            this.RemoveCommand.Do(_ => onRemoveCommand(this)).CatchIgnore().Subscribe().AddTo(this.disposables);
        }

        public void Dispose()
        {
            this.disposables?.Dispose();
            this.disposables = null;
        }
    }
}
