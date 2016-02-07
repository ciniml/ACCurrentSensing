using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using SQLite.Net;
using System.Reactive.Disposables;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Azure.Devices.Client;

namespace ACCurrentSensing.Model
{
    public class PowerDistributionRecord
    {
        public int Id { get; set; }
        public float Consumption { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
    }

    public class PowerDistributionLogger : IDisposable
    {
        private const string storageFileName = "power_distribution.sqlite";
        

        private SQLiteConnection connection;
        private SQLite.Net.Platform.WinRT.SQLitePlatformWinRT platform;

        private PowerDistribution powerDistribution;
        private CompositeDisposable disposables = new CompositeDisposable();

        private Subject<PowerDistributionRecord> powerDistributionSubject;

        public IObservable<PowerDistributionRecord> Observable
        {
            get { return this.powerDistributionSubject.AsObservable(); }
        }

        public PowerDistributionRecord[] GetRecordByPeriod(DateTimeOffset from, DateTimeOffset to)
        {
            return this.connection.Table<PowerDistributionRecord>()
                .Where(value => from <= value.TimeStamp && value.TimeStamp < to)
                .ToArray();
        }

        private void InitializeStorage()
        {
            var storagePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, storageFileName);
            this.platform = new SQLite.Net.Platform.WinRT.SQLitePlatformWinRT();

            this.connection = new SQLiteConnection(this.platform, storagePath);

            // Create the table if not exist.
            this.connection.CreateTable<PowerDistributionRecord>();
        }

        public PowerDistributionLogger(PowerDistribution powerDistribution)
        {
            this.InitializeStorage();

            this.powerDistribution = powerDistribution;
            this.powerDistributionSubject = new Subject<PowerDistributionRecord>().AddTo(this.disposables);

            this.powerDistribution.ObserveProperty(self => self.TotalCurrent)
                .Buffer(TimeSpan.FromSeconds(5))
                .Where(values => values.Count > 0)
                .Select(values => new PowerDistributionRecord() { Consumption = values.Average(), TimeStamp = DateTimeOffset.Now })
                .Do(record => this.connection.Insert(record))
                .Do(record => this.powerDistributionSubject.OnNext(record))
                .OnErrorRetry()
                .Subscribe()
                .AddTo(this.disposables);

            if (IoTHubConnectionSettings.HubConnectionString != null)
            {
                this.powerDistribution.ObserveProperty(self => self.TotalCurrent)
                    .Buffer(TimeSpan.FromMinutes(1))
                    .Where(values => values.Count > 0)
                    .Select(values => System.Reactive.Linq.Observable.StartAsync(async (token) =>
                    {
                        var average = values.Average();
                        var deviceClient = DeviceClient.CreateFromConnectionString(IoTHubConnectionSettings.HubConnectionString, TransportType.Http1);
                        var json = $"{{consumption: {average}}}";
                        var message = new Message(Encoding.UTF8.GetBytes(json));
                        await deviceClient.SendEventAsync(message).AsTask(token);
                    }))
                    .Switch()
                    .OnErrorRetry((Exception e) => Debug.WriteLine($"Failed to send data to the IoT Hub. {e.Message}"))
                    .Subscribe()
                    .AddTo(this.disposables);
            }
        }

        public IEnumerable<PowerDistributionRecord> GetPowerDistrubutionByPeriod(DateTimeOffset from, DateTimeOffset to)
        {
            return this.connection.Table<PowerDistributionRecord>().Where(record => from <= record.TimeStamp && record.TimeStamp < to).AsEnumerable();
        }

        public void Dispose()
        {
            this.connection?.Dispose();
            this.connection = null;
            this.disposables?.Dispose();
            this.disposables = null;
        }
    }
}
