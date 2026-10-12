using System;

namespace NeoLauncher.Models.Exceptions;

public class AccountServiceException : Exception
{
	public AccountServiceException(string message)
		: base(message)
	{
	}
}
