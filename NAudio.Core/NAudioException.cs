using System;
using System.Diagnostics.CodeAnalysis;

namespace NAudio
{
    /// <summary>
    /// Master class of all the exceptions originating from the NAudio library. <br />
    /// To be noted, this class serves as the base exception class for new custom exception classes to inherit from - 
    /// as such, exceptions such as <see cref="ArgumentNullException"/> may still be used. <br />
    /// Should possibly not be instantiated directly - may become an abstract class if such decision is made.
    /// </summary>
    public class NAudioException : ApplicationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NAudioException"/> class.
        /// </summary>
        public NAudioException() : base() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="NAudioException"/> class, from the specified error message. <br />
        /// The error message can be <see langword="null"/>.
        /// </summary>
        /// <param name="message">The error message to specify to the current exception instance.</param>
        public NAudioException([AllowNull] string message) : base(message) { } // mdcdi1315: Nullable context is not supported?

        /// <summary>
        /// Initializes a new instance of the <see cref="NAudioException"/> class, from the specified error message, and the exception instance that is the cause of this exception to be created.  <br />
        /// The error message can be <see langword="null"/>.
        /// </summary>
        /// <param name="message">The error message to specify to the current exception instance.</param>
        /// <param name="innerException">The exception instance that is the reason why this exception is created for.</param>
        public NAudioException([AllowNull] string message, [AllowNull] Exception innerException) : base(message, innerException){ }

        /// <summary>
        /// Gets a value whether the currently specified <see cref="Exception.InnerException"/> instance is a derivant of the <see cref="NAudioException"/> class.
        /// </summary>
        public bool InnerIsNAudioException => InnerException is NAudioException;
    }
}
