using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using System.ComponentModel;

namespace ACCurrentSensing.Model
{
    /// <summary>
    /// Plot style
    /// </summary>
    [Notify]
    public class PlotStyleSetting : INotifyPropertyChanged
    {
        public string Name { get { return name; } set { SetProperty(ref name, value, namePropertyChangedEventArgs); } }
        public Color PlotBackground { get { return plotBackground; } set { SetProperty(ref plotBackground, value, plotBackgroundPropertyChangedEventArgs); } }
        public Color PlotForeground { get { return plotForeground; } set { SetProperty(ref plotForeground, value, plotForegroundPropertyChangedEventArgs); } }
        public Color SeriesColor { get { return seriesColor; } set { SetProperty(ref seriesColor, value, seriesColorPropertyChangedEventArgs); } }

        #region NotifyPropertyChangedGenerator

        public event PropertyChangedEventHandler PropertyChanged;

        private string name;
        private static readonly PropertyChangedEventArgs namePropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(Name));
        private Color plotBackground;
        private static readonly PropertyChangedEventArgs plotBackgroundPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(PlotBackground));
        private Color plotForeground;
        private static readonly PropertyChangedEventArgs plotForegroundPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(PlotForeground));
        private Color seriesColor;
        private static readonly PropertyChangedEventArgs seriesColorPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(SeriesColor));

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

    /// <summary>
    /// Plot styles
    /// </summary>
    public class PlotStyleSettings
    {
        public static readonly PlotStyleSetting Default = new PlotStyleSetting()
        {
            Name = nameof(Default),
            PlotBackground = Colors.White,
            PlotForeground = Colors.Black,
            SeriesColor = Colors.DarkGreen,
        };
        public static readonly PlotStyleSetting Inverted = new PlotStyleSetting()
        {
            Name = nameof(Inverted),
            PlotBackground = Colors.Black,
            PlotForeground = Colors.White,
            SeriesColor = Colors.LightGreen,
        };
        public static readonly PlotStyleSetting Blue = new PlotStyleSetting()
        {
            Name = nameof(Blue),
            PlotBackground = Colors.White,
            PlotForeground = Colors.DarkBlue,
            SeriesColor = Colors.DodgerBlue,
        };
        public static readonly PlotStyleSetting Pink = new PlotStyleSetting()
        {
            Name = nameof(Pink),
            PlotBackground = Colors.DimGray,
            PlotForeground = Colors.HotPink,
            SeriesColor = Colors.Salmon,
        };
    }
}
