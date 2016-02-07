using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Notifications;
using Windows.UI.Xaml.Media;
using ACCurrentSensing.Model;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive;
using Windows.UI;
using Windows.UI.Xaml;
using OxyPlot;
using OxyPlot.Windows;

namespace ACCurrentSensing.ViewModel
{
    public class MainViewModel : IDisposable
    {
        public static readonly string NormalAlertState = nameof(NormalAlertState);
        public static readonly string WarningAlertState = nameof(WarningAlertState);
        public static readonly string ErrorAlertState = nameof(ErrorAlertState);

        public enum PlotType
        {
            Live,
            OneDay,
            OneWeek,
            OneMonth,
            Custom,
        }

        public struct HistoricPlotTypeCondition
        {
            public PlotType PlotType { get; set; }
            public Func<DateTimeOffset, DateTimeOffset> CalculateFrom { get; set; }
            public Func<DateTimeOffset, DateTimeOffset> CalculateTo { get; set; }
        }

        public static readonly HistoricPlotTypeCondition[] HistoricPlotTypeConditions = new[]
        {
            new HistoricPlotTypeCondition() {PlotType = PlotType.OneDay, CalculateFrom = (now) => now.AddDays(-1), CalculateTo = (now) => now},
            new HistoricPlotTypeCondition() {PlotType = PlotType.OneWeek, CalculateFrom = (now) => now.AddDays(-7), CalculateTo = (now) => now},
            new HistoricPlotTypeCondition() {PlotType = PlotType.OneMonth, CalculateFrom = (now) => now.AddMonths(-1), CalculateTo = (now) => now},
        };

        public PlotType[] PlotTypeValues { get; } = Enum.GetValues(typeof (PlotType)).Cast<PlotType>().ToArray();

        private CompositeDisposable disposables = new CompositeDisposable();

        private readonly ReactiveCollection<CurrentSensorViewModel> currentSensors = new ReactiveCollection<CurrentSensorViewModel>();
        public ReadOnlyReactiveCollection<CurrentSensorViewModel>  CurrentSensors { get; }

        public ReactiveProperty<PlotType> CurrentPlotType { get; }
        public ReadOnlyReactiveProperty<bool> IsLive { get; }
        public ReadOnlyReactiveProperty<bool> IsCustom { get; }
        public ReadOnlyReactiveProperty<bool> IsHistroy { get; }
        public ReadOnlyReactiveProperty<bool> HasDataPoints { get; }

        public ReactiveProperty<DateTimeOffset> PlotFromDate { get; }
        public ReactiveProperty<TimeSpan> PlotFromTime { get; }
        public ReactiveProperty<DateTimeOffset> PlotToDate { get; }
        public ReactiveProperty<TimeSpan> PlotToTime { get; }
        public ReactiveCommand UpdatePlotPeriodCommand { get; }

        public ReadOnlyReactiveProperty<float> TotalCurrent { get; }
        public ReadOnlyReactiveProperty<float> Capacity { get; }
        public ReadOnlyReactiveProperty<float> Usage { get; }
        public ReadOnlyReactiveProperty<float> AccumulatedCurrent { get; }

        public ReadOnlyReactiveProperty<string> AlertState { get; }
        

        public OxyPlot.PlotModel CurrentPlotModel { get; }
        public ReadOnlyReactiveProperty<SolidColorBrush> PlotForeground { get; }
        public ReadOnlyReactiveProperty<SolidColorBrush> PlotBackground { get; }

        public MainViewModel()
        {
            var powerDistribution = ((App)App.Current).PowerDistribution;
            var powerDistributionLogger = ((App) Application.Current).PowerDistributionLogger;
            var viewSettings = ((App) Application.Current).ViewSettings;

            // View Settings
            this.PlotForeground = viewSettings
                .ObserveProperty(self => self.PlotStyle).Select(style => style.ObserveProperty(self => self.PlotForeground))
                .Switch()
                .Select(color => new SolidColorBrush(color))
                .ToReadOnlyReactiveProperty()
                .AddTo(this.disposables);
            this.PlotBackground = viewSettings
                .ObserveProperty(self => self.PlotStyle).Select(style => style.ObserveProperty(self => self.PlotBackground))
                .Switch()
                .Select(color => new SolidColorBrush(color))
                .ToReadOnlyReactiveProperty()
                .AddTo(this.disposables);

            this.CurrentSensors = powerDistribution.Sensors.ToReadOnlyReactiveCollection(sensor => new CurrentSensorViewModel(sensor)).AddTo(this.disposables);

            this.TotalCurrent = powerDistribution.ObserveProperty(self => self.TotalCurrent).ToReadOnlyReactiveProperty(mode:ReactivePropertyMode.RaiseLatestValueOnSubscribe)
                .AddTo(this.disposables);

            this.Capacity = powerDistribution.ObserveProperty(self => self.Capacity).ToReadOnlyReactiveProperty().AddTo(this.disposables);
            this.Usage = Observable.CombineLatest(
                    this.TotalCurrent,
                    this.Capacity,
                    (totalCurrent, capacity) => capacity > 0 ? totalCurrent / capacity : 0)
                .ToReadOnlyReactiveProperty()
                .AddTo(this.disposables);

            this.AlertState = Observable.CombineLatest(
                    powerDistribution.ObserveProperty(self => self.IsWarningCondition),
                    powerDistribution.ObserveProperty(self => self.IsCriticalCondition),
                    (isWarning, isCritical) => isCritical ? ErrorAlertState : (isWarning ? WarningAlertState : NormalAlertState))
                .ToReadOnlyReactiveProperty()
                .AddTo(this.disposables);


            this.CurrentPlotType = new ReactiveProperty<PlotType>().AddTo(this.disposables);
            this.IsLive = this.CurrentPlotType.Select(type => type == PlotType.Live).ToReadOnlyReactiveProperty().AddTo(this.disposables);
            this.IsCustom = this.CurrentPlotType.Select(type => type == PlotType.Custom).ToReadOnlyReactiveProperty().AddTo(this.disposables);
            this.IsHistroy = this.IsLive.Select(value => !value).ToReadOnlyReactiveProperty().AddTo(this.disposables);

            this.CurrentPlotModel = new OxyPlot.PlotModel()
            {
                Axes =
                {
                    new OxyPlot.Axes.LinearAxis() { Unit = "A", Position = OxyPlot.Axes.AxisPosition.Left, Minimum = 0 },
                    new OxyPlot.Axes.DateTimeAxis() { Unit = "Time", Position = OxyPlot.Axes.AxisPosition.Bottom },
                },
            };
            // Change plot model colors.
            viewSettings
                .ObserveProperty(self => self.PlotStyle).Select(style => style.ObserveProperty(self => self.PlotForeground))
                .Switch()
                .Subscribe(color_ =>
                {
                    var color = color_.ToOxyColor();
                    this.CurrentPlotModel.TextColor = color;
                    this.CurrentPlotModel.PlotAreaBorderColor = color;
                    foreach (var axis in this.CurrentPlotModel.Axes)
                    {
                        axis.MajorGridlineColor = color;
                        axis.MinorGridlineColor = color;
                        axis.TextColor = color;
                        axis.AxislineColor = color;
                        axis.TicklineColor = color;
                    }
                })
                .AddTo(this.disposables);
            var totalCurrentSeries = new OxyPlot.Series.LineSeries()
            {
                Title = "Total Current",
            };
            // Change plot series color.
            viewSettings
                .ObserveProperty(self => self.PlotStyle).Select(style => style.ObserveProperty(self => self.SeriesColor))
                .Switch()
                .Subscribe(color => totalCurrentSeries.Color = color.ToOxyColor())
                .AddTo(this.disposables);

            this.CurrentPlotModel.Series.Add(totalCurrentSeries);


            var currentHistory = this.TotalCurrent
                .Select(Value => new { Value, TimeStamp = DateTime.Now })
                .ToObservationHistory(Observable.Empty<Unit>(), pair => (DateTime.Now - pair.TimeStamp) >= TimeSpan.FromSeconds(60))
                .AddTo(this.disposables);

            var livePlotSource = currentHistory.HistoryChanged
                .Select(_ =>
                {
                    var history = currentHistory.GetHistory();
                    return history.Select(pair => new OxyPlot.DataPoint() {X = OxyPlot.Axes.DateTimeAxis.ToDouble(pair.TimeStamp), Y = pair.Value});
                });


            var historicPlotPeriods = HistoricPlotTypeConditions
                .Select(condition => this.CurrentPlotType
                    .Where(plotType => plotType == condition.PlotType)
                    .Select(_ => { var now_ = DateTimeOffset.Now; return new { From = condition.CalculateFrom(now_), To = condition.CalculateTo(now_)}; })
                )
                .ToArray();
            
            var now = DateTimeOffset.Now;
            var today = now.Subtract(now.TimeOfDay);
            this.PlotFromDate = new ReactiveProperty<DateTimeOffset>(today).AddTo(this.disposables);
            this.PlotToDate = new ReactiveProperty<DateTimeOffset>(today).AddTo(this.disposables);
            this.PlotFromTime = new ReactiveProperty<TimeSpan>(now.TimeOfDay).AddTo(this.disposables);
            this.PlotToTime = new ReactiveProperty<TimeSpan>(now.TimeOfDay).AddTo(this.disposables);
            this.UpdatePlotPeriodCommand = new ReactiveCommand().AddTo(this.disposables);
            var customPlotPeriod = this.UpdatePlotPeriodCommand
                .Select(_ => new {From = this.PlotFromDate.Value.Add(this.PlotFromTime.Value), To = this.PlotToDate.Value.Add(this.PlotToTime.Value)});

            var recordedPlotSource = Observable
                .Merge(historicPlotPeriods.Concat(new[] {customPlotPeriod}))
                .Select(period => powerDistributionLogger.GetPowerDistrubutionByPeriod(period.From, period.To))
                .Select(dataPoints => dataPoints.Select(pair => new OxyPlot.DataPoint() {X = OxyPlot.Axes.DateTimeAxis.ToDouble(pair.TimeStamp.LocalDateTime), Y = pair.Consumption}));

            var hasDataPointsSubject = new Subject<bool>().AddTo(this.disposables);
            var accumulatedCurrentSubject = new Subject<float>().AddTo(this.disposables);
            this.AccumulatedCurrent = accumulatedCurrentSubject.ToReadOnlyReactiveProperty().AddTo(this.disposables);
            this.IsLive
                .Select(isLive => isLive ? livePlotSource : recordedPlotSource)
                .Switch()
                .ObserveOnUIDispatcher()
                .Do(dataPointsEnumerable =>
                {
                    var dataPoints = dataPointsEnumerable.ToArray();
                    accumulatedCurrentSubject.OnNext(
                        (float)dataPoints.Pairwise().Sum(dataPoint => 
                            (dataPoint.OldItem.Y + dataPoint.NewItem.Y) * (OxyPlot.Axes.DateTimeAxis.ToDateTime(dataPoint.NewItem.X) - OxyPlot.Axes.DateTimeAxis.ToDateTime(dataPoint.OldItem.X)).TotalSeconds )
                                / 3600.0f);

                    hasDataPointsSubject.OnNext(dataPoints.Length > 0);
                    totalCurrentSeries.Points.Clear();
                    totalCurrentSeries.Points.AddRange(dataPoints);
                })
                .Do(_ => this.CurrentPlotModel.InvalidatePlot(true))
                .Subscribe()
                .AddTo(this.disposables);

            this.HasDataPoints = hasDataPointsSubject.ToReadOnlyReactiveProperty().AddTo(this.disposables);
        }


        public void Dispose()
        {
            this.disposables?.Dispose();
            this.disposables = null;
        }
    }
}
