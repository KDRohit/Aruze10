using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
///   Wrapper class for logging. Allows for filtering logs that only contain specific keywords, assert logging,
///   and overall better log control/maintenance
/// </summary>
public class SmartLog
{
	public static List<string> mustContainKeywords = new List<string>();
	
	public static void Log(object message, params object[] formatWords)
	{
		conditionalLog(message, formatWords);
	}

	public static void LogWarning(object message, params object[] formatWords)
	{
		conditionalLogWarning(message, formatWords);
	}

	public static void LogError(object message, params object[] formatWords)
	{
		conditionalLogError(message, formatWords);
	}

	public static void LogException(System.Exception e, UnityEngine.Object o = null)
	{
		conditionalLogExeption(e, o);
	}

	public static bool Assert(bool ifTrueLogError, object message, params object[] formatWords)
	{
		if (ifTrueLogError)
		{
			conditionalLogError(message, formatWords);
		}

		return ifTrueLogError;
	}

	public static void addWordFilter(string word)
	{
		if (!mustContainKeywords.Contains(word))
		{
			mustContainKeywords.Add(word);
		}
	}

	public static void clearWordFilter()
	{
		mustContainKeywords = new List<string>();
	}

	/*=========================================================================================
	PRIVATE METHODS
	=========================================================================================*/
	private static void conditionalLog(object message, params object[] formatWords)
	{
		if (containsKeyword(message))
		{
			Debug.Log(string.Format(message.ToString(), formatWords));
		}
	}

	private static void conditionalLogWarning(object message, params object[] formatWords)
	{
		if (containsKeyword(message))
		{
			Debug.LogWarning(string.Format(message.ToString(), formatWords));
		}
	}

	private static void conditionalLogError(object message, params object[] formatWords)
	{
		if (containsKeyword(message))
		{
			Debug.LogError(string.Format(message.ToString(), formatWords));
		}
	}

	private static void conditionalLogExeption(System.Exception e, UnityEngine.Object o)
	{
		if (containsKeyword(e.Message))
		{
			Debug.LogException(e, o);
		}
	}

	public static bool containsKeyword(object message)
	{
		if (mustContainKeywords.Count > 0)
		{
			for (int i = 0; i < mustContainKeywords.Count; ++i)
			{
				if (message.ToString().Contains(mustContainKeywords[i].ToString()))
				{
					return true;
				}
			}

			return false;
		}
		return true;
	}
}