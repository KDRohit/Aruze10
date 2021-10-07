using UnityEngine;

public class HIRLogHandler : ILogHandler
{
	private static ILogHandler defaultHandler = null;
	public static void init()
	{
		defaultHandler = Debug.unityLogger.logHandler;
#if !UNITY_EDITOR
		Debug.unityLogger.logHandler = new HIRLogHandler();
#endif
	}

	public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
	{
		if (Data.debugMode)
		{
			defaultHandler.LogFormat(logType, context, format, args);
		}
	}

	public void LogException(System.Exception exception, UnityEngine.Object context)
	{
		if (Data.debugMode)
		{
			defaultHandler.LogException(exception, context);
		}
	}
}