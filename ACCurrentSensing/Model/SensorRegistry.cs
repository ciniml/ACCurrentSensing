using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ACCurrentSensing.Model
{
    /// <summary>
    /// Registry of sensors.
    /// </summary>
    public class SensorRegistry
    {
        /// <summary>
        /// Internal object to serialize sensor registries.
        /// </summary>
        [DataContract]
        private class SerializingObject
        {
            [DataMember]
            public SensorInformation[] Sensors { get; private set; }

            public SerializingObject(IEnumerable<SensorInformation> sensors)
            {
                this.Sensors = sensors.ToArray();
            }
        }
        
        private readonly ObservableCollection<SensorInformation> sensors;
        private readonly Dictionary<SensorKind, ObservableCollection<SensorInformation>> classifiedSensors;
        
        public ReadOnlyObservableCollection<SensorInformation> Sensors { get; }
        public ReadOnlyObservableCollection<SensorInformation> CurrentSensors { get; }

        public SensorRegistry() : this(new SensorInformation[0])
        {
        }

        private SensorRegistry(IEnumerable<SensorInformation> sensors)
        {
            this.sensors = new ObservableCollection<SensorInformation>(sensors);
            this.Sensors = new ReadOnlyObservableCollection<SensorInformation>(this.sensors);

            this.classifiedSensors = Enum.GetValues(typeof(SensorKind)).OfType<SensorKind>()
                .ToDictionary(kind => kind, kind => new ObservableCollection<SensorInformation>(this.sensors.Where(sensor => sensor.Kind == kind)));

            this.CurrentSensors = new ReadOnlyObservableCollection<SensorInformation>(this.classifiedSensors[SensorKind.Current]);
        }

        /// <summary>
        /// Register a sensor to this registry.
        /// </summary>
        /// <param name="sensor"></param>
        /// <returns></returns>
        public Task RegisterSensor(SensorInformation sensor)
        {
            this.sensors.Add(sensor);
            this.classifiedSensors[sensor.Kind].Add(sensor);
            return Task.FromResult(0);
        }

        /// <summary>
        /// Unregister a sensor to this registry.
        /// </summary>
        /// <param name="sensor"></param>
        /// <returns></returns>
        public Task UnregisterSensor(SensorInformation sensor)
        {
            this.classifiedSensors[sensor.Kind].Remove(sensor);
            this.sensors.Remove(sensor);
            return Task.FromResult(0);
        }

        /// <summary>
        /// Check if a sensor is registered in this registry or not.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool IsRegistered(SensorInformation target)
        {
            return this.sensors.Any(sensor => sensor.Id == target.Id || (sensor.LogicalDeviceId == target.LogicalDeviceId && sensor.PhysicalDeviceId == target.PhysicalDeviceId));
        }

        /// <summary>
        /// Serialize the sensors in this registry.
        /// </summary>
        /// <returns></returns>
        public string Serialize()
        {
            var builder = new StringBuilder();
            using (var xmlWriter = XmlWriter.Create(builder))
            {
                var serializingObject = new SerializingObject(this.Sensors);
                var serializer = new DataContractSerializer(typeof(SerializingObject));
                serializer.WriteObject(xmlWriter, serializingObject);
            }
            return builder.ToString();
        }

        /// <summary>
        /// Try to deserialize a registry from string.
        /// If deserialization is failed, return an empty registry.
        /// </summary>
        /// <param name="serialized"></param>
        /// <returns></returns>
        public static SensorRegistry DeserializeOrDefault(string serialized)
        {
            try
            {
                using (var xmlReader = XmlReader.Create(new StringReader(serialized)))
                {
                    var serializer = new DataContractSerializer(typeof(SerializingObject));
                    var serializingObject = (SerializingObject)serializer.ReadObject(xmlReader);

                    return new SensorRegistry(serializingObject.Sensors);
                }
            }
            catch(SerializationException)
            {
                return new SensorRegistry();
            }
        }
    }
}
