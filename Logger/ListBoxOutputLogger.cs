using Printing;
using System;
using System.Windows.Forms;

namespace TestZebraPrinting
{
    /// <summary>
    /// Class for logging output to a ListBox
    /// </summary>
    public class ListBoxOutputLogger : IOutputLogger, IPrintLogger
    {
        private readonly ListBox _listBox;

        private delegate void Update();

        /// <summary>
        /// Constructor for the ListBoxOutputLogger
        /// </summary>
        /// <param name="listBox">The ListBox to log the output to</param>
        public ListBoxOutputLogger(ListBox listBox)
        {
            _listBox = listBox ?? throw new ArgumentNullException(nameof(listBox));
        }

        /// <summary>
        /// Add a message to the ListBox
        /// </summary>
        /// <param name="message">The message to add</param>
        public void AddMessage(string message)
        {

                if (_listBox.InvokeRequired)
                {
                    _listBox.BeginInvoke(new Action(() =>
                    {
                        _listBox.Items.Add(message);
                        _listBox.TopIndex = _listBox.Items.Count - 1;
                    }));
                }
                else
                {
                    _listBox.Items.Add(message);
                    _listBox.TopIndex = _listBox.Items.Count - 1;
                }
        }

        /// <summary>
        /// Clear the ListBox
        /// </summary>
        public void Clear()
        {
            _listBox.Items.Clear();
        }
    }
}
