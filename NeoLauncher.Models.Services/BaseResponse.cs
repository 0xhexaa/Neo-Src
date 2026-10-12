namespace NeoLauncher.Models.Services;

public class BaseResponse
{
	public bool IsError => !string.IsNullOrEmpty(Error);

	public string? Error { get; set; }

	public BaseResponse()
	{
		Error = string.Empty;
	}

	public BaseResponse(string error)
	{
		Error = error;
	}
}
