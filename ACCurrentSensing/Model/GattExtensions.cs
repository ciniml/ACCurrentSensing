using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;

namespace ACCurrentSensing.Model
{
    public static class GattExtensions
    {
        /// <summary>
        /// Treat the value read from the GATT characteristic as a string in the specified encoding.
        /// </summary>
        /// <param name="readResultTask">The task reading from a value from a GATT characteristic.</param>
        /// <param name="encoding">The encoding in which the characteristic value is encoded.</param>
        /// <param name="cancellationToken">The token to notify the cancellation of this operation.</param>
        /// <returns></returns>
        public static async Task<string> AsStringOrDefault(this IAsyncOperation<GattReadResult> readResultTask, Encoding encoding, CancellationToken cancellationToken)
        {
            var result = await readResultTask;
            if (result.Status != GattCommunicationStatus.Success)
            {
                return null;
            }

            var data = result.Value.ToArray();
            return encoding.GetString(data, 0, data.Length);
        }
        public static Task<string> AsStringOrDefault(this IAsyncOperation<GattReadResult> readResultTask, CancellationToken cancellationToken)
        {
            return AsStringOrDefault(readResultTask, Encoding.UTF8, cancellationToken);
        }
        public static Task<string> AsStringOrDefault(this IAsyncOperation<GattReadResult> readResultTask)
        {
            return AsStringOrDefault(readResultTask, Encoding.UTF8, CancellationToken.None);
        }
    }
}
