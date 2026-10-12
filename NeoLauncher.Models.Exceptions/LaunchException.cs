namespace NeoLauncher.Models.Exceptions;

public class LaunchException : AccountServiceException
{
	public LaunchErrorCode Code { get; }

	public LaunchException(string message, LaunchErrorCode code = LaunchErrorCode.Unknown)
		: base(message)
	{
		Code = code;
	}
}
