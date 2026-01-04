using Printing;
using Printing.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestZebraPrinting
{
    /// <summary>
    /// Class for the PrintForm
    /// </summary>
    public partial class PrintForm : Form
    {
        private CancellationTokenSource _cancellationTokenSource;
        private IOutputLogger _outputLogger;

        /// <summary>
        /// Constructor for the PrintForm
        /// </summary>
        public PrintForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Event handler for the Cancel button click
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void CancelButton_Click(object sender, EventArgs e)
        {
            // Cancel the printing operation
            _cancellationTokenSource?.Cancel();
            CancelButton.Enabled = false;
        }

        /// <summary>
        /// Event handler for the Print button click
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private async void PrintButtonClick(object sender, EventArgs e)
        {
            // Disable Print button and enable Cancel button
            PrintButton.Enabled = false;
            CancelButton.Enabled = true;
            _outputLogger.Clear();

            // Create a new cancellation token source for this print job
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                // Get the total number of prints from the NumericUpDown
                int totalPrints = (int)PrintTotal.Value;

                // Setup progress reporting
                var progress = new Progress<int>(value =>
                {
                    // Update progress bar
                    ProgressBar.Value = value;
                });

                // Setup the progress bar
                ProgressBar.Minimum = 0;
                ProgressBar.Maximum = totalPrints;
                ProgressBar.Value = 0;

                // Create a new printer service connection
                using (IPrintingService printerService = new MockPrintingService(_outputLogger))
                {
                    // Wire up the event handler for proceed requests
                    printerService.ProceedRequested += PrinterService_ProceedRequested;

                    try
                    {
                        // Connect to the printer
                        _outputLogger.AddMessage("Connecting to printer...");
                        printerService.Connect("192.168.0.1");

                        string statusMessage = string.Empty;
                        bool isConnected = printerService.IsPrinterReady(statusMessage);

                        if (!isConnected)
                        {
                            throw new InvalidOperationException($"Printer not ready: {statusMessage}");
                        }

                        _outputLogger.AddMessage("Printer connected successfully.");

                        // Use the new BulkPrint method
                        await printerService.BulkPrint(totalPrints, progress, _cancellationTokenSource.Token);

                        _outputLogger.AddMessage("Printing completed successfully!");
                    }
                    finally
                    {
                        // Unwire the event handler
                        printerService.ProceedRequested -= PrinterService_ProceedRequested;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _outputLogger.AddMessage("Printing cancelled by user.");
            }
            catch (Exception ex)
            {
                _outputLogger.AddMessage($"Error: {ex.Message}");
            }
            finally
            {
                // Reset UI state
                PrintButton.Enabled = true;
                CancelButton.Enabled = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        /// <summary>
        /// Event handler for the printer service proceed requested
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void PrinterService_ProceedRequested(object sender, ProceedRequestEventArgs e)
        {
            // Use Invoke to ensure we're on the UI thread
            if (InvokeRequired)
            {
                Invoke(new Action(() => PrinterService_ProceedRequested(sender, e)));
                return;
            }

            // Show a dialog to the user
            var result = MessageBox.Show(
                e.Message,
                "Confirmation Required",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            // Set the Proceed property based on user response
            e.Proceed = (result == DialogResult.Yes);
        }

        /// <summary>
        /// Event handler for the form load
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void PrintForm_Load(object sender, EventArgs e)
        {
            // Initialize the output logger with the ListBox
            _outputLogger = new ListBoxOutputLogger(OutputListBox);

            // Initialize UI state
            CancelButton.Enabled = false;
            PrintTotal.Value = 10; // Set a default value
        }

        /// <summary>
        /// Event handler for the test connection button click
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private async void TestConnectionButton_Click(object sender, EventArgs e)
        {
            _outputLogger.Clear();
            _outputLogger.AddMessage("Testing printer Connection...");

            // Disable button to prevent multiple clicks
            TestConnectionButton.Enabled = false;

            try
            {
                using (IPrintingService printerService = new MockPrintingService(_outputLogger))
                {
                    // Run the blocking printer operations on a background thread
                    var result = await Task.Run(() =>
                    {
                        string statusMessage = string.Empty;
                        printerService.Connect("192.168.0.1");
                        bool isConnected = printerService.IsPrinterReady(statusMessage);
                        return new { IsConnected = isConnected, StatusMessage = statusMessage };
                    });

                    if (result.IsConnected)
                    {
                        _outputLogger.AddMessage("Connection Successful");
                    }
                    else
                    {
                        _outputLogger.AddMessage($"Error: {result.StatusMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                _outputLogger.AddMessage($"Error: {ex.Message}");
            }
            finally
            {
                // Re-enable button
                TestConnectionButton.Enabled = true;
            }
        }

        /// <summary>
        /// Generate ZPL data for a label
        /// </summary>
        /// <param name="labelNumber">The number of the label</param>
        /// <returns>The ZPL data for the label</returns>
        private string GenerateZplData(int labelNumber)
        {
            // Example ZPL data generation - customize based on your needs
            return $"^XA\n" +
                   $"^FO50,50^A0N,36,36^FDLabel {labelNumber}^FS\n" +
                   $"^FO50,100^BQN,2,4^FDMM,Label {labelNumber}^FS\n" +
                   $"^XZ";
        }
    }
}