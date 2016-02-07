using Microsoft.WindowsAzure.MobileServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACCurrentSensing.Model.HomeLog.Entities
{
    public class CurrentLogItem
    {
        public string Id { get; set; }
        public float Current { get; set; }

        public string SensorId { get; set; }

        public DateTimeOffset MeasuredAt { get; set; }
    }

    public class Sensor
    {
        public string Id { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }

        public string Location { get; set; }
    }
}
