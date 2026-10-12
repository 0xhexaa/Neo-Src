namespace NeoLauncher.Models.Exceptions;

public enum LaunchErrorCode
{
	Unknown,
	AlreadyRunning,
	Banned,
	ServicesDown,
	ServicesUnreachable,
	ExchangeCodeFailed,
	BinaryNotFound,
	PrismAssetMissing,
	ProcessFailed,
	ProcessExited,
	Timeout,
	Injection,
	ModuleInUse,
	ModuleNotFound,
	Cancelled
}
