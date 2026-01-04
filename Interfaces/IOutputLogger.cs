using Printing;

namespace TestZebraPrinting
{
    /// <summary>
    /// Interface for output logging
    /// </summary>
    public interface IOutputLogger : IPrintLogger
    {
        /// <summary>
        /// Clear the output logger
        /// </summary>
        void Clear();
    }
}
