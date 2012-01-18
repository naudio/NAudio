using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.Utils
{
    /// <summary>
    /// Progress Event Arguments
    /// </summary>
    public class ProgressEventArgs : EventArgs
    {
        private string message;
        private ProgressMessageType messageType;

        /// <summary>
        /// New progress event arguments
        /// </summary>
        /// <param name="messageType">The message type</param>
        /// <param name="message">The message</param>
        public ProgressEventArgs(ProgressMessageType messageType, string message)
        {
            this.message = message;
            this.messageType = messageType;
        }

        /// <summary>
        /// New progress event arguments
        /// </summary>
        /// <param name="messageType">The message type</param>
        /// <param name="message">the message format string</param>
        /// <param name="args">format arguments</param>
        public ProgressEventArgs(ProgressMessageType messageType, string message, params object[] args)
        {
            this.messageType = messageType;
            this.message = String.Format(message, args);
        }

        /// <summary>
        /// The message
        /// </summary>
        public string Message
        {
            get
            {
                return message;
            }
        }

        /// <summary>
        /// The message type
        /// </summary>
        public ProgressMessageType MessageType
        {
            get
            {
                return messageType;
            }
        }
    }

    /// <summary>
    /// Progress Message Type
    /// </summary>
    public enum ProgressMessageType
    {
        /// <summary>
        /// Trace
        /// </summary>
        Trace,
        /// <summary>
        /// Information
        /// </summary>
        Information,
        /// <summary>
        /// Warning
        /// </summary>
        Warning,
        /// <summary>
        /// Error
        /// </summary>
        Error,
    }
}
