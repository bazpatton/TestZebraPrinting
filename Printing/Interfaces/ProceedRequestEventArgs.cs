using System;

namespace Printing
{
    /// <summary>
    /// Event args for requesting user confirmation to proceed
    /// </summary>
    public class ProceedRequestEventArgs : EventArgs
    {
        public string Message { get; set; }
        public bool Proceed { get; set; }
    }
}