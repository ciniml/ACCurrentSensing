using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACCurrentSensing.Model
{
    [Notify]
    public class ViewSettings : INotifyPropertyChanged
    {
        public PlotStyleSetting PlotStyle { get { return plotStyle; } set { SetProperty(ref plotStyle, value, plotStylePropertyChangedEventArgs); } }

        public ViewSettings()
        {
            this.PlotStyle = PlotStyleSettings.Inverted;
        }
        #region NotifyPropertyChangedGenerator

        public event PropertyChangedEventHandler PropertyChanged;

        private PlotStyleSetting plotStyle;
        private static readonly PropertyChangedEventArgs plotStylePropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(PlotStyle));

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
