using Reactive.Bindings.Extensions;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace ACCurrentSensing.Model
{
    /// <summary>
    /// A class to comunicate with current sensors.
    /// </summary>
    [Notify]
    public class CurrentSensorDevice : INotifyPropertyChanged, IDisposable
    {
        [EventSource(Guid = "F72D7C68-9FC8-410B-A4EC-A83ED6D224DB", Name ="CurrentSensorDevice")]
        private class DeviceEventSource : EventSource
        {
            [NonEvent]
            public void Created(CurrentSensorDevice device) { this.Created(device.traceId); }
            [Event(1, Level=EventLevel.Informational, Keywords = EventKeywords.None, Message ="TraceId={0}")]
            private void Created(long traceId)
            {
                base.WriteEvent(1, traceId);
            }
            [NonEvent]
            public void Initializing(CurrentSensorDevice device, DeviceInformation deviceInformation) { this.Initializing(device.traceId, deviceInformation.Id, deviceInformation.Name); }
            [Event(2, Level = EventLevel.Informational, Keywords = EventKeywords.None, Message = "TraceId={0},Device={2}({1})")]
            private void Initializing(long traceId, string id, string name)
            {
                base.WriteEvent(2, traceId, id, name);
            }

            [NonEvent]
            public void Initialized(CurrentSensorDevice device, DeviceInformation deviceInformation) { this.Initialized(device.traceId, deviceInformation.Id, deviceInformation.Name); }
            [Event(3, Level = EventLevel.Informational, Keywords = EventKeywords.None, Message = "TraceId={0},Device={2}({1})")]
            private void Initialized(long traceId, string id, string name)
            {
                base.WriteEvent(3, traceId, id, name);
            }

            [NonEvent]
            public void ConnectionStatusChanged(CurrentSensorDevice device, DeviceInformation deviceInformation, bool isConnected) { this.ConnectionStatusChanged(device.traceId, deviceInformation.Id, deviceInformation.Name, isConnected); }
            [Event(4, Level = EventLevel.Informational, Keywords = EventKeywords.None, Message = "TraceId={0},Connected={3},Device={2}({1})")]
            private void ConnectionStatusChanged(long traceId, string id, string name, bool isConnected)
            {
                base.WriteEvent(4, traceId, id, name, isConnected);
            }

            [NonEvent]
            public void CurrentValueChanged(CurrentSensorDevice device, DeviceInformation deviceInformation, int value) { this.CurrentValueChanged(device.traceId, deviceInformation.Id, deviceInformation.Name, value); }
            [Event(5, Level = EventLevel.Verbose, Keywords = EventKeywords.None, Message = "TraceId={0},Connected={3},Device={2}({1})")]
            private void CurrentValueChanged(long traceId, string id, string name, int value)
            {
                base.WriteEvent(5, traceId, id, name, value);
            }

            [NonEvent]
            public void Disposed(CurrentSensorDevice device) { this.Disposed(device.traceId, device.Device.Id, device.Device.Name); }
            [Event(6, Level = EventLevel.Verbose, Keywords = EventKeywords.None, Message = "TraceId={0},Device={2}({1})")]
            private void Disposed(long traceId, string id, string name)
            {
                base.WriteEvent(6, traceId, id, name);
            }
        }

        static readonly DeviceEventSource eventSource = new DeviceEventSource();

        /// <summary>
        /// Raw sensor value to A(rms) conversion coefficient.
        /// </summary>
        public struct Coefficient
        {
            public int Numerator { get; set; }
            public int Denominator { get; set; }
        }

        /// <summary>
        /// UUID of the current sensor service.
        /// </summary>
        private static readonly Guid CurrentSensorServiceGuid = new Guid("00000000-0001-0002-0003-0123456789ab");
        /// <summary>
        /// UUID of the current characterstic.
        /// </summary>
        private static readonly Guid CurrentCharacteristicGuid = new Guid("00010000-1000-2000-3012-3456789ab000");
        /// <summary>
        /// UUID of the coefficient characteristic.
        /// </summary>
        private static readonly Guid CoefficientCharacteristicGuid = new Guid("00010000-1000-2000-3012-3456789ab001");

        /// <summary>
        /// The ContainerId property.
        /// </summary>
        private const string ContainerIdProperty = "System.Devices.ContainerId";

        /// <summary>
        /// Average to Root Mean Squared conversion coefficient.
        /// </summary>
        private const float AverageToRms = 3.14159265f / (2 * 1.41421356f);

        /// <summary>
        /// Find sensor devices
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task<DeviceInformationCollection> FindSensorDevice(CancellationToken cancellationToken)
        {
            var filter = GattDeviceService.GetDeviceSelectorFromUuid(CurrentSensorServiceGuid);
            return DeviceInformation.FindAllAsync(filter, new[] { ContainerIdProperty }).AsTask(cancellationToken);
        }

        public static IObservable<SensorInformation> FindSensors()
        {
            return Observable
                .StartAsync(cancellationToken => FindSensorDevice(cancellationToken))
                .ObserveOn(SynchronizationContext.Current)  // GattDeviceService.FromIdAsync should be run on the UI thread. Thus we have to capture current synchronization context and observe on it.
                .SelectMany(devices => devices)
                .SelectMany(device => Observable.StartAsync(cancellationToken => GattDeviceService.FromIdAsync(device.Id).AsTask(cancellationToken)).Catch(Observable.Empty<GattDeviceService>()))
                .Select(service => new SensorInformation(SensorKind.Current) { LogicalDeviceId = service.DeviceId, PhysicalDeviceId = $"{service.Device.BluetoothAddress:012X}" });

        }
        
        private static async Task<DeviceInformation> EnsureWithContainerId(DeviceInformation deviceInformation)
        {
            if (!deviceInformation.Properties.ContainsKey(ContainerIdProperty))
            {
                deviceInformation = await DeviceInformation.CreateFromIdAsync(deviceInformation.Id, new[] { ContainerIdProperty });
            }
            return deviceInformation;
        }

        private static async Task<string> ReadStringCharacteristicAsync(GattDeviceService service, Guid guid, CancellationToken cancellationToken)
        {
            var characteristic = service.GetCharacteristics(guid).SingleOrDefault();
            if (characteristic != null)
            {
                return await characteristic.ReadValueAsync().AsStringOrDefault(cancellationToken) ?? "";
            }
            return null;
        }

        private static async Task<bool> EnableNotification(GattCharacteristic characteristic, CancellationToken cancellationToken)
        {
            if (!characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
            {
                throw new NotSupportedException("This characteristic does not support the notification.");
            }
            try
            {
                //var result = GattCommunicationStatus.Unreachable; 
                var result = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify).AsTask(cancellationToken);
                return result == GattCommunicationStatus.Success;
            }
            catch (Exception)
            {
                return false;
            }

        }

        private static async Task<IObservable<IBuffer>> ObserveValue<TTrigger>(GattCharacteristic characteristic, IObservable<TTrigger> triggerObservable, BluetoothCacheMode cacheMode, CancellationToken cancellationToken)
        {
            if (characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify) && await EnableNotification(characteristic, cancellationToken))
            {
                return characteristic.ValueChangedAsObservable();
            }
            else
            {
                return characteristic.Observe(triggerObservable, cacheMode);
            }
        }

        public static async Task<CurrentSensorDevice> FromServiceDeviceAsync(DeviceInformation serviceDeviceInformation, CancellationToken cancellationToken)
        {
            var device = new CurrentSensorDevice();
            await device.InitializeDevice(serviceDeviceInformation, cancellationToken);
            return device;
        }

        public static async Task<CurrentSensorDevice> FromSensorInformationAsync(SensorInformation sensorInformation, CancellationToken cancellationToken)
        {
            if (sensorInformation.Kind != SensorKind.Current) throw new ArgumentException("Invalid sensor type");
            var deviceId = await DeviceInformation.CreateFromIdAsync(sensorInformation.LogicalDeviceId, new[] { ContainerIdProperty });
            return await FromServiceDeviceAsync(deviceId, cancellationToken);
        }

        private readonly long traceId = TraceObjectTracker<CurrentSensorDevice>.NewId();
        private CompositeDisposable disposables = new CompositeDisposable();

        private GattDeviceService currentSensorService;
        private GattCharacteristic currentCharacteristic;

        private GattDeviceService batteryService;
        private GattCharacteristic batteryLevelCharacteristic;

        private IConnectableObservable<bool> isConnectedObservable;
        private Subject<Unit> updateIsConnectedSubject;

        public string HardwareRevision { get { return hardwareRevision; } set { SetProperty(ref hardwareRevision, value, hardwareRevisionPropertyChangedEventArgs); } }
        public string FirmwareRevision { get { return firmwareRevision; } set { SetProperty(ref firmwareRevision, value, firmwareRevisionPropertyChangedEventArgs); } }

        [NonNotify]
        public DeviceInformation Device { get; private set; }

        public float BatteryLevel { get { return batteryLevel; } private set { SetProperty(ref batteryLevel, value, batteryLevelPropertyChangedEventArgs); } }
        public int BatteryLevelRaw { get { return batteryLevelRaw; } private set { SetProperty(ref batteryLevelRaw, value, batteryLevelRawPropertyChangedEventArgs); } }
        public float Current { get { return current; } private set { SetProperty(ref current, value, currentPropertyChangedEventArgs); } }
        public int CurrentRaw { get { return currentRaw; } private set { SetProperty(ref currentRaw, value, currentRawPropertyChangedEventArgs); } }

        public Coefficient CoefficientNumerator { get { return coefficientNumerator; } set { SetProperty(ref coefficientNumerator, value, coefficientNumeratorPropertyChangedEventArgs); } }

        public bool IsConnected { get { return isConnected; } set { SetProperty(ref isConnected, value, isConnectedPropertyChangedEventArgs); } }

        private async Task InitializeDevice(DeviceInformation serviceDeviceInformation, CancellationToken cancellationToken)
        {
            eventSource.Initializing(this, serviceDeviceInformation);

            serviceDeviceInformation = await EnsureWithContainerId(serviceDeviceInformation);
            this.Device = serviceDeviceInformation;

            this.currentSensorService = await GattDeviceService.FromIdAsync(serviceDeviceInformation.Id).AsTask(cancellationToken);

            var connectionStatusChangedObservable = Observable
                .FromEvent<TypedEventHandler<BluetoothLEDevice, object>, BluetoothConnectionStatus>(
                    handler => ((o, e) => handler(o.ConnectionStatus)),
                    handler => this.currentSensorService.Device.ConnectionStatusChanged += handler,
                    handler => this.currentSensorService.Device.ConnectionStatusChanged -= handler)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .Select(status => status == BluetoothConnectionStatus.Connected)
                .Do(value => eventSource.ConnectionStatusChanged(this, serviceDeviceInformation, value));

            this.updateIsConnectedSubject = new Subject<Unit>();
            this.isConnectedObservable = Observable.Merge(
                    connectionStatusChangedObservable,
                    this.updateIsConnectedSubject.Select(_ => this.currentSensorService.Device.ConnectionStatus == BluetoothConnectionStatus.Connected)
                )
                .Publish();

            this.isConnectedObservable.Subscribe(isConnected => this.IsConnected = isConnected).AddTo(this.disposables);
                

            // initialize Coefficient characteristic
            var coefficientUpdateTimer = Observable.Return<Unit>(Unit.Default).Concat(Observable.Interval(TimeSpan.FromMinutes(2)).ToUnit());
            var coefficientCharacteristic = this.currentSensorService.GetCharacteristics(CoefficientCharacteristicGuid).Single();
            var coefficientObservable = this.isConnectedObservable
                .Select(isConnected => isConnected ? Observable.StartAsync(token => ObserveValue(coefficientCharacteristic, coefficientUpdateTimer, BluetoothCacheMode.Uncached, token)).Switch() : Observable.Return<IBuffer>(null))
                .Switch()
                .Select(value =>
                {
                    var numerator = value?.ToInt32(0) ?? 0;
                    var denominator = value?.ToInt32(4) ?? 0;
                    return new Coefficient() { Denominator = denominator == 0 ? 1 : denominator, Numerator = denominator == 0 ? 1 : numerator };
                })
                .Publish();

            // initialize Current characteristic
            this.currentCharacteristic = this.currentSensorService.GetCharacteristics(CurrentCharacteristicGuid).Single();

            var currentRaw = this.isConnectedObservable
                .Select(isConnected => isConnected ? Observable.StartAsync(token => ObserveValue(this.currentCharacteristic, Observable.Interval(TimeSpan.FromSeconds(10)), BluetoothCacheMode.Uncached, token)).Switch() : Observable.Return<IBuffer>(null))
                .Switch()
                .Select(value => value?.ToInt32() ?? 0)
                .Catch((Exception e) => { System.Diagnostics.Debug.WriteLine(e); return Observable.Empty<int>(); })
                .Do(value => eventSource.CurrentValueChanged(this, serviceDeviceInformation, value))
                .Publish();
            currentRaw.Subscribe(value => this.CurrentRaw = value).AddTo(this.disposables);

            var currentObservable = currentRaw
                .CombineLatest(coefficientObservable, (current, coefficient) => current * AverageToRms * coefficient.Numerator / coefficient.Denominator)
                .Buffer(TimeSpan.FromSeconds(1))
                .Where(values => values.Count > 0)
                .Select(values => values.Average())
                .Subscribe(value => this.Current = value).AddTo(this.disposables);

            currentRaw.Connect().AddTo(this.disposables);
            coefficientObservable.Connect().AddTo(this.disposables);

            {
                var bluetoothDevice = currentSensorService.Device;
                var deviceInformationService = bluetoothDevice.GetGattService(GattServiceUuids.DeviceInformation);

                this.HardwareRevision = await ReadStringCharacteristicAsync(deviceInformationService, GattCharacteristicUuids.HardwareRevisionString, cancellationToken) ?? "";
                this.FirmwareRevision = await ReadStringCharacteristicAsync(deviceInformationService, GattCharacteristicUuids.FirmwareRevisionString, cancellationToken) ?? "";
            }

            //this.batteryService = await GetOtherServiceAsync(serviceDeviceInformation, GattServiceUuids.Battery, cancellationToken);
            //this.batteryLevelCharacteristic = this.batteryService.GetCharacteristics(GattCharacteristicUuids.BatteryLevel).SingleOrDefault();

            //var batteryLevelRaw = (await ObserveValue(this.batteryLevelCharacteristic, Observable.Interval(TimeSpan.FromSeconds(30)), BluetoothCacheMode.Uncached, CancellationToken.None))
            //    .Select(value => (int)value.GetByte(0))
            //    .Publish();
            //batteryLevelRaw.Subscribe(value => this.BatteryLevelRaw = value).AddTo(this.disposables);
            //batteryLevelRaw
            //    .Select(value => value / 100.0f)
            //    .Subscribe(value => this.BatteryLevel = value)
            //    .AddTo(this.disposables);

            //batteryLevelRaw.Connect().AddTo(this.disposables);
            this.isConnectedObservable.Connect().AddTo(this.disposables);

            // Try to connect to the device.
            await this.currentCharacteristic.ReadValueAsync();
            this.updateIsConnectedSubject.OnNext(Unit.Default);

            eventSource.Initialized(this, serviceDeviceInformation);
        }

        private CurrentSensorDevice()
        {
            eventSource.Created(this);
            
        }

        public void Dispose()
        {
            this.disposables?.Dispose();
            this.disposables = null;

            eventSource.Disposed(this);
        }

        #region NotifyPropertyChangedGenerator

        public event PropertyChangedEventHandler PropertyChanged;

        private string hardwareRevision;
        private static readonly PropertyChangedEventArgs hardwareRevisionPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(HardwareRevision));
        private string firmwareRevision;
        private static readonly PropertyChangedEventArgs firmwareRevisionPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(FirmwareRevision));
        private float batteryLevel;
        private static readonly PropertyChangedEventArgs batteryLevelPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(BatteryLevel));
        private int batteryLevelRaw;
        private static readonly PropertyChangedEventArgs batteryLevelRawPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(BatteryLevelRaw));
        private float current;
        private static readonly PropertyChangedEventArgs currentPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(Current));
        private int currentRaw;
        private static readonly PropertyChangedEventArgs currentRawPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(CurrentRaw));
        private Coefficient coefficientNumerator;
        private static readonly PropertyChangedEventArgs coefficientNumeratorPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(CoefficientNumerator));
        private bool isConnected;
        private static readonly PropertyChangedEventArgs isConnectedPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(IsConnected));

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
