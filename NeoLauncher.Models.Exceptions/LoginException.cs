namespace NeoLauncher.Models.Exceptions;

public class LoginException : AccountServiceException
{
	public LoginFailureReason Reason { get; }

	public LoginException(string message, LoginFailureReason reason = LoginFailureReason.Unknown)
		: base(message)
	{
		Reason = reason;
	}
}
