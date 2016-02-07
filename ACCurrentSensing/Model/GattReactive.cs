using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls.Primitives;

namespace ACCurrentSensing.Model
{
    public static class GattReactiveExtensions
    {
        public static IObservable<IBuffer> Observe<TTrigger>(this GattCharacteristic characteristic, IObservable<TTrigger> triggerObservable, BluetoothCacheMode cacheMode = BluetoothCacheMode.Cached)
        {
            return triggerObservable
                .Select(_ => Observable.StartAsync(token => characteristic.ReadValueAsync(cacheMode).AsTask(token)))
                .Switch()
                .Where(result => result.Status == GattCommunicationStatus.Success)
                .Select(result => result.Value);
        }

        public static IObservable<IBuffer> ValueChangedAsObservable(this GattCharacteristic characteristic)
        {
            return Observable.FromEvent<TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs>, GattValueChangedEventArgs>(
                handler => ((_, eventArgs) => handler(eventArgs)),
                handler => characteristic.ValueChanged += handler,
                handler => characteristic.ValueChanged -= handler)
                .Catch((Exception e) => { Debug.WriteLine($"Exception on ValueChanged ${e}"); return Observable.Empty<GattValueChangedEventArgs>(); })
                .Do(eventArgs => Debug.WriteLine($"ValueChanged: {eventArgs.CharacteristicValue.Length}"))
                .Select(eventArgs => eventArgs.CharacteristicValue);
                
        }
    
    }

}
