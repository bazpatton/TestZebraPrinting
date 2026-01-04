using System;
using System.Threading;
using System.Threading.Tasks;

namespace Printing
{
    /// <summary>
    /// Interface for the printing service
    /// </summary>
    public interface IPrintingService : IDisposable
    {
        /// <summary>
        /// Event raised when confirmation is needed to proceed with bulk printing
        /// </summary>
        event EventHandler<ProceedRequestEventArgs> ProceedRequested;

        /// <summary>
        /// Performs bulk printing operation
        /// </summary>
        /// <param name="totalPrints">Total number of prints to perform</param>
        /// <param name="progress">Progress reporter for tracking print completion</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
        /// <returns>Task that completes when bulk printing is done</returns>
        Task BulkPrint(int totalPrints, IProgress<int> progress, CancellationToken cancellationToken);

        /// <summary>
        /// Connect to the printer
        /// </summary>
        /// <param name="theIpAddress">The IP address of the printer</param>
        void Connect(string theIpAddress);

        /// <summary>
        /// Check if the printer is ready
        /// </summary>
        /// <param name="message">The message to return</param>
        /// <returns>True if the printer is ready, false otherwise</returns>
        bool IsPrinterReady(string message = "");

        /// <summary>
        /// Send ZPL data to the printer
        /// </summary>
        /// <param name="data">The ZPL data to send</param>
        void SendZplOverTcp(string data);
    }
}