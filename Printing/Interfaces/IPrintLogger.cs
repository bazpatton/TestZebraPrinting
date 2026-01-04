namespace Printing
{
    /// <summary>
    /// Interface for the print logger
    /// </summary>
    public interface IPrintLogger
    {
        /// <summary>
        /// Add a message to the log
        /// </summary>
        /// <param name="message">The message to add</param>
        void AddMessage(string message);
    }
}
