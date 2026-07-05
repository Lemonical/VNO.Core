using System;

namespace VNO.Core.Protocol;

/// <summary>
/// Thrown when inbound bytes cannot be parsed as a valid message
/// </summary>
/// <remarks>
/// A decode failure is a property of one peer's traffic, not the listener. Transports
/// catch this, close only the offending session, and keep serving everyone else. Kept
/// distinct from <see cref="InvalidOperationException"/> so a caller can tell a malformed
/// frame from a genuine program bug
/// </remarks>
public sealed class ProtocolFormatException : Exception
{
    /// <summary>
    /// Creates the exception with a human readable reason
    /// </summary>
    public ProtocolFormatException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates the exception with a reason and an inner cause
    /// </summary>
    public ProtocolFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
