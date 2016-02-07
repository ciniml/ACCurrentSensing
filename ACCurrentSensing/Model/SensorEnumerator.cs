using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACCurrentSensing.Model
{
    public class SensorEnumerator
    {
        public IObservable<SensorInformation> Enumerate()
        {
            return CurrentSensorDevice.FindSensors();
        }
    }
}
