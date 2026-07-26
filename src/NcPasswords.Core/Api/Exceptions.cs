namespace NcPasswords.Core.Api;

/// <summary>Base exception for failures talking to the Passwords API.</summary>
public class PasswordsApiException : Exception
{
    public PasswordsApiException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}

/// <summary>Thrown when the account has client-side encryption (CSE) enabled, which this client does not support.</summary>
public sealed class CseNotSupportedException : PasswordsApiException
{
    public CseNotSupportedException()
        : base("This Nextcloud account has client-side encryption (CSE) enabled. " +
               "This app only supports accounts using the default server-side encryption.")
    {
    }
}

/// <summary>Thrown when the server rejects the supplied credentials.</summary>
public sealed class PasswordsAuthenticationException : PasswordsApiException
{
    public PasswordsAuthenticationException(string message = "Authentication failed. Check the server URL, username and password.")
        : base(message)
    {
    }
}
