using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Printer;

namespace Printing
{   
    /// <summary>
    /// Class for the printing service
    /// </summary>
    public class PrintingService : IPrintingService
    {
        private Connection _printerConnection;
        private IPrintLogger _printLogger;

        /// <summary>
        /// Event raised when confirmation is needed to proceed with bulk printing
        /// </summary>
        public event EventHandler<ProceedRequestEventArgs> ProceedRequested;
        public void Connect(string theIpAddress)
        {
            try
            {
                _printerConnection = new TcpConnection(theIpAddress, TcpConnection.DEFAULT_ZPL_TCP_PORT);
                _printerConnection.Open();
            }
            catch
            (ConnectionException ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Close the connection to free up resources
        /// </summary>
        public void Dispose()
        {
            if (_printerConnection != null)
            {
                _printerConnection.Close();
            }
        }

        /// <summary>
        /// Send ZPL data to the printer
        /// </summary>
        /// <param name="data">The ZPL data to send</param>
        public void SendZplOverTcp(string data)
        {

            try
            {
                if (IsPrinterReady())
                {


                    // This example prints "This is a ZPL test." near the top of the label.
                    string zplData = $"^XA^FO20,20^A0N,25,25^FDThis is a {data} test.^FS^XZ";

                    // Send the data to printer as a byte array.
                    _printerConnection.Write(Encoding.UTF8.GetBytes(zplData));
                }
            }
            catch (ConnectionException e)
            {
                // Handle communications error here.
                _printLogger.AddMessage(e.ToString());

            }
            finally
            {
                // Close the connection to release resources.
                // This is now handled in the dispose. Make sure Printing service is wrapped around a uising to invoke it
            }
        }

        /// <summary>
        /// Check if the printer is ready
        /// </summary>
        /// <param name="message">The message to return</param>
        /// <returns>True if the printer is ready, false otherwise</returns>
        public bool IsPrinterReady(string message = "")
        {
            if (_printerConnection == null)
            {
                throw new InvalidOperationException("Not connected to printer");
            }

            ZebraPrinter printer = ZebraPrinterFactory.GetLinkOsPrinter(_printerConnection, PrinterLanguage.ZPL);
            if (null == printer)
            {
                printer = ZebraPrinterFactory.GetInstance(PrinterLanguage.ZPL, _printerConnection);
            }
            PrinterStatus printerStatus = printer.GetCurrentStatus();

            if (printerStatus.isReadyToPrint)
            {
                _printLogger.AddMessage("Ready To Print");
                return true;
            }
            else if (printerStatus.isPaused)
            {
                _printLogger.AddMessage("Cannot Print because the printer is paused.");
            }
            else if (printerStatus.isHeadOpen)
            {
                _printLogger.AddMessage("Cannot Print because the printer head is open.");
            }
            else if (printerStatus.isPaperOut)
            {
                _printLogger.AddMessage("Cannot Print because the paper is out.");
            }
            else
            {
                _printLogger.AddMessage("Cannot Print.");
            }

            PrinterStatusMessages printerStatusMessages = new PrinterStatusMessages(printerStatus);

            message = printerStatusMessages.GetStatusMessage().ToString();

            return false;
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
            if (_printerConnection == null)
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