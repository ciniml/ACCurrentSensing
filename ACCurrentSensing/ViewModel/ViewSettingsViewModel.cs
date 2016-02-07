using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.AllJoyn;
using Windows.UI.Xaml;
using ACCurrentSensing.Model;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Windows;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;

namespace ACCurrentSensing.ViewModel
{
    public class ViewSettingsViewModel : IDisposable
    {
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        public Model.PlotStyleSetting[] PlotStyleSettings { get; } = new[]
        {
            Model.PlotStyleSettings.Default,
            Model.PlotStyleSettings.Inverted,
            Model.PlotStyleSettings.Blue,
            Model.PlotStyleSettings.Pink,
        };

        public ReactiveProperty<Model.PlotStyleSetting> SelectedPlotStyle { get; }
        public PlotModel SamplePlotModel { get; set; }

        public ViewSettingsViewModel()
        {
            var viewSettings = ((App)Application.Current).ViewSettings;
            this.SelectedPlotStyle = viewSettings.ToReactivePropertyAsSynchronized(self => self.PlotStyle).AddTo(this.disposables);

            this.SamplePlotModel = new OxyPlot.PlotModel()
            {
                Axes =
                {
                    new OxyPlot.Axes.LinearAxis() { Unit = "A", Position = OxyPlot.Axes.AxisPosition.Left, Minimum = 0 },
                    new OxyPlot.Axes.DateTimeAxis() { Unit = "Time", Position = OxyPlot.Axes.AxisPosition.Bottom },
                },
            };
            var sampleSeries = new FunctionSeries(x => Math.Sin(x*Math.PI*2), 0, 1, 1000, "Total Current");
            this.SamplePlotModel.Series.Add(sampleSeries);

            this.SelectedPlotStyle
                .Do(style =>
                {
                    var foregroundColor = style.PlotForeground.ToOxyColor();
                    this.SamplePlotModel.Background = style.PlotBackground.ToOxyColor();
                    this.SamplePlotModel.TextColor = foregroundColor;
                    this.SamplePlotModel.PlotAreaBorderColor = foregroundColor;
                    foreach (var axis in this.SamplePlotModel.Axes)
                    {
                        axis.MajorGridlineColor = foregroundColor;
                        axis.MinorGridlineColor = foregroundColor;
                        axis.TextColor = foregroundColor;
                        axis.AxislineColor = foregroundColor;
                        axis.TicklineColor = foregroundColor;
                    }
                    sampleSeries.Color = style.SeriesColor.ToOxyColor();
                    this.SamplePlotModel.InvalidatePlot(false);
                })
                .OnErrorRetry()
                .Subscribe()
                .AddTo(this.disposables);
        }

        public void Dispose()
        {
            this.disposables.Dispose();
        }
    }
}
