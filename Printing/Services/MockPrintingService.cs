using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Zebra.Sdk.Comm;

namespace Printing.Services
{
    /// <summary>
    /// Class for the mock printing service
    /// </summary>
    public class MockPrintingService : IPrintingService
    {
        private readonly List<string> _errorMessages = new List<string>
    {
        "Printer is paused",
        "Printer head is open",
        "Paper is out",
        "Ribbon out",
        "Printer not responding",
        "Communication error",
        "Printer buffer full",
        "Temperature warning",
        "Media jam detected",
        "Printhead dirty"
    };

        private readonly IPrintLogger _printLogger;
        private readonly Random _random = new Random();
        private string _ipAddress = "";
        private bool _isConnected = false;
        private bool _isDisposed = false;
        
        /// <summary>
        /// Event raised when confirmation is needed to proceed with bulk printing
        /// </summary>
        public event EventHandler<ProceedRequestEventArgs> ProceedRequested;

        /// <summary>
        /// Constructor for the MockPrintingService
        /// </summary>
        /// <param name="printLogger">The print logger</param>
        public MockPrintingService(IPrintLogger printLogger)
        {
            _printLogger = printLogger;
        }

        /// <summary>
        /// Connect to the printer
        /// </summary>
        /// <param name="theIpAddress">The IP address of the printer</param>
        public void Connect(string theIpAddress)
        {
            if (_isDisposed)
            {
                throw new InvalidOperationException("Service has been disposed");
            }

            // 10% chance of connection failure
            if (_random.Next(1, 11) == 1) // 1 in 10 chance
            {
                _printLogger.AddMessage($"Failed to connect to printer at {theIpAddress}: Connection refused");
                throw new ConnectionException($"Unable to connect to {theIpAddress}");
            }

            _ipAddress = theIpAddress;
            _isConnected = true;
            _printLogger.AddMessage($"Successfully connected to printer at {theIpAddress}");
        }

        /// <summary>
        /// Dispose of the printing service
        /// </summary>
        public void Dispose()
        {
            if (_isConnected)
            {
                _printLogger.AddMessage($"Disconnected from printer at {_ipAddress}");
                _isConnected = false;
            }
            else
            {
                _printLogger.AddMessage($"All resoursed are clear");
            }
            _isDisposed = true;
        }

        /// <summary>
        /// Check if the printer is ready
        /// </summary>
        /// <param name="message">The message to return</param>
        /// <returns>True if the printer is ready, false otherwise</returns>
        public bool IsPrinterReady(string message = "")
        {
            if (_isDisposed)
            {
                throw new InvalidOperationException("Service has been disposed");
            }

            if (!_isConnected)
            {
                message = "Not connected to printer";
                _printLogger.AddMessage(message);
                return false;
            }

            // 1 in 10 chance of returning false with random error
            if (_random.Next(1, 11) == 1)
            {
                string error = _errorMessages[_random.Next(0, _errorMessages.Count)];
                _printLogger.AddMessage($"Printer not ready: {error}");
                message = error;
                return false;
            }

            // 1 in 50 chance of throwing connection exception
            if (_random.Next(1, 51) == 1)
            {
                _printLogger.AddMessage("Connection lost while checking printer status");
                throw new ConnectionException("Connection lost");
            }

            // Simulate printer status check delay
            Thread.Sleep(_random.Next(20, 100));

            // 90% chance printer is ready
            if (_random.Next(1, 11) <= 9) // 9 in 10 chance
            {
                _printLogger.AddMessage("Ready To Print");
                return true;
            }
            else
            {
                // Simulate various printer states
                string error = _errorMessages[_random.Next(0, _errorMessages.Count)];
                _printLogger.AddMessage($"Printer not ready: {error}");
                message = error;
                return false;
            }
        }

        /// <summary>
        /// Send ZPL data to the printer
        /// </summary>
        /// <param name="data">The ZPL data to send</param>
        public void SendZplOverTcp(string data)
        {
            if (_isDisposed)
            {
                throw new InvalidOperationException("Service has been disposed");
            }

            try
            {
                if (!_isConnected)
                {
                    _printLogger.AddMessage("Cannot send data: Not connected to printer");
                    throw new ConnectionException("Not connected to printer");
                }

                if (IsPrinterReady())
                {
                    string zplData = $"^XA^FO20,20^A0N,25,25^FDThis is a {data} test.^FS^XZ";

                    // Simulate sending data (add small delay to mimic real transmission)
                    Thread.Sleep(_random.Next(50, 200));

                    _printLogger.AddMessage($"Successfully sent ZPL data: {data}");

                    // Small chance of send failure even when printer was ready
                    if (_random.Next(1, 21) == 1) // 1 in 20 chance
                    {
                        _printLogger.AddMessage("Data transmission failed mid-send");
                        throw new ConnectionException("Data transmission error");
                    }
                }
                else
                {
                    _printLogger.AddMessage($"Failed to send data: Printer not ready");
                    throw new Exception("Failed to send data: Printer not ready");
                }
            }
            catch (Exception e)
            {
                _printLogger.AddMessage($"Error in SendZplOverTcp: {e}");
                throw;
            }
        }

        /// <summary>
        /// Performs bulk printing operation
        /// </summary>
        /// <param name="totalPrints">Total number of prints to perform</param>
        /// <param name="progress">Progress reporter for tracking print completion</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
        /// <returns>Task that completes when bulk printing is done</returns>
        public async Task BulkPrint(int totalPrints, IProgress<int> progress, CancellationToken cancellationToken)
        {
            if (_isDisposed)
            {
                throw new InvalidOperationException("Service has been disposed");
            }

            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to printer");
            }

            // Request confirmation before proceeding
            var proceedArgs = new ProceedRequestEventArgs
            {
                Message = $"About to print {totalPrints} labels. Do you want to proceed?",
                Proceed = false
            };

            // Raise the event to request confirmation
            ProceedRequested?.Invoke(this, proceedArgs);

            // If user declined, exit
            if (!proceedArgs.Proceed)
            {
                _printLogger.AddMessage("Bulk print operation cancelled by user");
                return;
            }

            _printLogger.AddMessage($"Starting bulk print of {totalPrints} labels...");

            for (int i = 1; i <= totalPrints; i++)
            {
                // Check for cancellation
                cancellationToken.ThrowIfCancellationRequested();

                _printLogger.AddMessage($"Printing label {i} of {totalPrints}...");

                try
                {
                    // Generate ZPL data for this label
                    string zplData = $"^XA^FO50,50^A0N,36,36^FDLabel {i}^FS^FO50,100^BQN,2,4^FDMM,Label {i}^FS^XZ";

                    // Send the ZPL data to the printer
                    SendZplOverTcp(zplData);

                    _printLogger.AddMessage($"Successfully printed label {i}");
                }
                catch (Exception ex)
                {
                    _printLogger.AddMessage($"Error printing label {i}: {ex.Message}");
                    
                    // Ask if user wants to continue after error
                    var errorProceedArgs = new ProceedRequestEventArgs
                    {
                        Message = $"Error printing label {i}: {ex.Message}\n\nDo you want to continue?",
                        Proceed = false
                    };

                    ProceedRequested?.Invoke(this, errorProceedArgs);

                    if (!errorProceedArgs.Proceed)
                    {
                        _printLogger.AddMessage("Bulk print operation stopped by user after error");
                        return;
                    }
                }

                // Report progress
                progress?.Report(i);

                // Add delay between prints (except for the last one)
                if (i < totalPrints)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(500, cancellationToken);
                }
            }

            _printLogger.AddMessage($"Bulk print operation completed successfully. Total: {totalPrints} labels");
        }
    }
}