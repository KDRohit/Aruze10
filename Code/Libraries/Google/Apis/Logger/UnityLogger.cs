using UnityEngine;
using System.Collections;
using Google.Apis.Logging;

public class UnityLogger {
	
	private static UnityLogger instance;
	
	public static UnityLogger Instance {
		get {
			if(instance == null) {
				instance = new UnityLogger();
			}
			
			return instance;
		}
	}
	
	public bool IsDebugEnabled {
		get {
			return false;
		}
	}
	
	public UnityLogger ForType (System.Type type)
	{
		return Instance;
	}

	public UnityLogger ForType<T> ()
	{
		return ForType(typeof(T));
	}

	public void Info (string message, params object[] formatArgs)
	{
		UnityEngine.Debug.Log(string.Format(message, formatArgs));
	}

	public void Warning (string message, params object[] formatArgs)
	{
		UnityEngine.Debug.LogWarning(string.Format(message, formatArgs));
	}

	public void Debug (string message, params object[] formatArgs)
	{
		if(IsDebugEnabled) {
			Info(message, formatArgs);
		}	
	}

	public void Error (System.Exception exception, string message, params object[] formatArgs)
	{
		UnityEngine.Debug.LogError(string.Format(message, formatArgs));
	}

	public void Error (string message, params object[] formatArgs)
	{
		Error(null, message, formatArgs);
	}
}
