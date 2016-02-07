using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ACCurrentSensing.Model
{
    public enum SensorKind
    {
        /// <summary>
        /// Current sensor
        /// </summary>
        Current,
        /// <summary>
        /// Themometer (Not supported yet)
        /// </summary>
        Thermometer,
    }

    [Notify]
    [DataContract]
    public class SensorInformation : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets the identifier of this sensor. (Generated automatically when this sensor is registered.)
        /// </summary>
        [NonNotify]
        [DataMember]
        public Guid Id { get; private set; }

        /// <summary>
        /// Gets or sets the name of this sensor.
        /// </summary>
        [DataMember]
        public string Name { get { return name; } set { SetProperty(ref name, value, namePropertyChangedEventArgs); } }

        [DataMember]
        public string LogicalDeviceId { get { return logicalDeviceId; } set { SetProperty(ref logicalDeviceId, value, logicalDeviceIdPropertyChangedEventArgs); } }
        [DataMember]
        public string PhysicalDeviceId { get { return physicalDeviceId; } set { SetProperty(ref physicalDeviceId, value, physicalDeviceIdPropertyChangedEventArgs); } }

        /// <summary>
        /// Gets or sets the ID of the location this sensor is placed at.
        /// </summary>
        [DataMember]
        public Guid LocationId { get { return locationId; } set { SetProperty(ref locationId, value, locationIdPropertyChangedEventArgs); } }

        /// <summary>
        /// Gets or sets kind of this sensor.
        /// </summary>
        [DataMember]
        public SensorKind Kind { get { return kind; } private set { SetProperty(ref kind, value, kindPropertyChangedEventArgs); } }

        public SensorInformation(SensorKind kind)
        {
            this.Id = Guid.NewGuid();
            this.Name = "";
            this.LogicalDeviceId = null;
            this.PhysicalDeviceId = null;
            this.LocationId = Guid.Empty;
            this.Kind = kind;
        }

        public SensorInformation(Guid id, SensorKind kind) : this(kind)
        {
            this.Id = id;
        }

        #region NotifyPropertyChangedGenerator

        public event PropertyChangedEventHandler PropertyChanged;

        private string name;
        private static readonly PropertyChangedEventArgs namePropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(Name));
        private string logicalDeviceId;
        private static readonly PropertyChangedEventArgs logicalDeviceIdPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(LogicalDeviceId));
        private string physicalDeviceId;
        private static readonly PropertyChangedEventArgs physicalDeviceIdPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(PhysicalDeviceId));
        private Guid locationId;
        private static readonly PropertyChangedEventArgs locationIdPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(LocationId));
        private SensorKind kind;
        private static readonly PropertyChangedEventArgs kindPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(Kind));

        private void SetProperty<T>(ref T field, T value, PropertyChangedEventArgs ev)
        {
            if (!System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, ev);
            }
        }

        #endregion
    }
}
