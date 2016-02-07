using Microsoft.WindowsAzure.MobileServices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ACCurrentSensing.Model.HomeLog.Entities;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;

namespace ACCurrentSensing.Model.HomeLog
{
    [Notify]
    public class HomeLogService : INotifyPropertyChanged, IDisposable
    {
        private readonly MobileServiceClient client;
        private readonly object isBusyLock = new object();

        public bool IsBusy { get { return isBusy; } private set { SetProperty(ref isBusy, value, isBusyPropertyChangedEventArgs); } }

        public MobileServiceCollection<Sensor, Sensor> Sensors { get { return sensors; } private set { SetProperty(ref sensors, value, sensorsPropertyChangedEventArgs); } }

        private Task transmissionTask;
        private CancellationTokenSource transmissionCancelSource = new CancellationTokenSource();
        private ConcurrentBag<CurrentLogItem> currentLogItems = new ConcurrentBag<CurrentLogItem>();
        
        public HomeLogService(MobileServiceClient client)
        {
            this.client = client;

            this.transmissionTask = Task.Run(this.TransmitLogItems, this.transmissionCancelSource.Token);
        }

        private async Task TransmitLogItems()
        {
            var items = new List<CurrentLogItem>();
            while(true)
            {
                await Task.Delay(TimeSpan.FromSeconds(10));

                CurrentLogItem item;
                while(this.currentLogItems.TryTake(out item))
                {
                    items.Add(item);
                }

                try
                {
                    if (items.Count > 0)
                    {
                        await this.client.InvokeApiAsync<List<CurrentLogItem>, object>("values", items);
                        items.Clear();
                    }
                }
                catch(Exception)
                {
                    // Remove older than 1 minute ago.
                    var now = DateTimeOffset.Now;
                    items.RemoveAll(x => (now - x.MeasuredAt).TotalMinutes > 1);
                }
            }
        }

        private void EnsureIsNotBusy()
        {
            lock(this.isBusyLock)
            {
                if (this.IsBusy) throw new InvalidOperationException("Processing another operation.");
                this.IsBusy = true;
            }
        }
        public async Task UpdateSensorsAsync()
        {
            this.EnsureIsNotBusy();
            try
            {
                var table = this.client.GetTable<Sensor>();
                this.Sensors = await table.OrderBy(sensor => sensor.Name).ToCollectionAsync();
            }
            catch (MobileServiceInvalidOperationException)
            {
                throw;
            }
            catch (HttpRequestException)
            {
                throw;
            }
            finally
            {
                this.IsBusy = false;
            }
        }

        public async Task<Sensor> GetSensorByLocation(string location)
        {
            await this.UpdateSensorsAsync();
            return this.Sensors.SingleOrDefault(sensor => sensor.Location == location);
        }

        public async Task AddSensorAsync(Sensor sensor)
        {
            this.EnsureIsNotBusy();
            try
            {
                var table = this.client.GetTable<Sensor>();
                await table.InsertAsync(sensor);
                this.Sensors.Add(sensor);
            }
            finally
            {
                this.IsBusy = false;
            }
        }
        
        public Task AddCurrentLogItemAsync(CurrentLogItem item)
        {
            item.MeasuredAt = DateTimeOffset.Now;
            this.currentLogItems.Add(item);
            return Task.CompletedTask;
            //this.EnsureIsNotBusy();
            //try
            //{
            //    var table = this.client.GetTable<CurrentLogItem>();
            //    await table.InsertAsync(item);
            //}
            //catch(Exception e)
            //{
            //    throw;
            //}
            //finally
            //{
            //    this.IsBusy = false;
            //}
        }

        public async Task AddCurrentLogItemsAsync(IEnumerable<CurrentLogItem> items)
        {
            this.EnsureIsNotBusy();
            try
            {
                await this.client.InvokeApiAsync<IEnumerable<CurrentLogItem>, JObject>("values", items);
            }
            finally
            {
                this.IsBusy = false;
            }
        }
        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.transmissionCancelSource.Cancel();
                    // Consume all items in ConcurrentBag.
                    CurrentLogItem item;
                    while (this.currentLogItems.TryTake(out item)) ;
                }

                // TODO: アンマネージ リソース (アンマネージ オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                // TODO: 大きなフィールドを null に設定します。

                disposedValue = true;
            }
        }

        // TODO: 上の Dispose(bool disposing) にアンマネージ リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
        // ~HomeLogService() {
        //   // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
        //   Dispose(false);
        // }

        // このコードは、破棄可能なパターンを正しく実装できるように追加されました。
        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
            Dispose(true);
            // TODO: 上のファイナライザーがオーバーライドされる場合は、次の行のコメントを解除してください。
            // GC.SuppressFinalize(this);
        }
        #endregion


        #region NotifyPropertyChangedGenerator

        public event PropertyChangedEventHandler PropertyChanged;

        private bool isBusy;
        private static readonly PropertyChangedEventArgs isBusyPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(IsBusy));
        private MobileServiceCollection<Sensor, Sensor> sensors;
        private static readonly PropertyChangedEventArgs sensorsPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(Sensors));

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
