using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Reactive.Bindings;
using Windows.Devices.Bluetooth.Advertisement;
using System.Diagnostics;

namespace ACCurrentSensing.Model
{
    public class CurrentSensorDeviceWatcher
    {
        private BluetoothLEAdvertisementWatcher watcher;
        
        public CurrentSensorDeviceWatcher()
        {
            this.watcher = new BluetoothLEAdvertisementWatcher();
            this.watcher.Received += (o, e) => {
                Debug.WriteLine($"Address:{e.BluetoothAddress:X08},RSSI:{e.RawSignalStrengthInDBm}");
            };
            this.watcher.ScanningMode = BluetoothLEScanningMode.Active;
            this.watcher.Start();
        }
        
    }
}
