using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

#if ZYNGA_TRAMP
public class AutomatedCompanionLog 
{
	public LogType logType;					// Type of the log.
	public string logMessage;				// Log message.
	public string stackTrace;				// Stack trace of the log.
	public System.DateTime timestamp;		// Time at which the error occurred. 
	public string logTitle;					// First few characters of log.
	public string activeAction;
	public int spinNumber;
	public JSON prevOutcome;
	public JSON outcome;

	private const int MAX_TITLE_LENGTH = 99;


	public int logNum = 0;

	// Note a match is found with Contains
	public static readonly List<string> warningsToIgnore = new List<string>()
		{
			"Streamed image dimensions are too big at",
			"Falling back to old way of getting soundMap for game",
			"_experimentData is null",
		};

	// Note a match is found with Contains
	public static readonly List<string> errorsToIgnore = new List<string>()
		{
			"_experimentData is null",
		};

	public AutomatedCompanionLog(LogType logType, string message, string stack, int logNum, System.DateTime timestamp, string activeAction, int spinNumber, JSON outcome = null, JSON prevOutcome = null)
	{
		init(logType, message, stack, logNum, timestamp, activeAction, spinNumber, outcome, prevOutcome);
	}

	public AutomatedCompanionLog(JSON json)
	{
		LogType logType = (LogType)json.getInt(AutomationJSONKeys.LOG_TYPE_KEY, 0);
		string logMessage = json.getString(AutomationJSONKeys.LOG_MESSAGE_KEY, AutomationJSONKeys.DEFAULT_KEY);
		string stack = json.getString(AutomationJSONKeys.STACK_TRACE_KEY, AutomationJSONKeys.DEFAULT_KEY);
		int logNum = json.getInt(AutomationJSONKeys.LOGNUM_KEY, 0);
		System.DateTime timestamp = System.DateTime.Parse(json.getString(AutomationJSONKeys.TIMESTAMP_KEY, System.DateTime.MinValue.ToString()));
		string activeAction = json.getString(AutomationJSONKeys.ACTIVE_ACTION_KEY, AutomationJSONKeys.DEFAULT_KEY);
		int spinNumber = json.getInt(AutomationJSONKeys.SPIN_NUMBER_KEY, -1);
		JSON prevOutcome = json.getJSON(AutomationJSONKeys.PREV_OUTCOME);
		JSON outcome = json.getJSON(AutomationJSONKeys.OUTCOME);

		init(logType, logMessage, stack, logNum, timestamp, activeAction, spinNumber, outcome, prevOutcome);
	}

	public void init(LogType logType, string message, string stack, int logNum, System.DateTime timestamp, string activeAction, int spinNumber, JSON outcome = null, JSON prevOutcome = null)
	{
		this.logType = logType;	
		this.logMessage = message;		
		this.stackTrace = stack;

		this.timestamp = timestamp;

		this.logNum = logNum;
		this.logTitle = getTitle();
		this.activeAction = activeAction;
		this.spinNumber = spinNumber;

		if (logType != LogType.Warning)
		{
			this.prevOutcome = prevOutcome;
			this.outcome = outcome;
		}
	}


	public string ToJSON()
	{
		StringBuilder build = new StringBuilder();

		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.LOG_TYPE_KEY, (int)logType));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.LOG_MESSAGE_KEY, logMessage));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.STACK_TRACE_KEY, stackTrace));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.TIMESTAMP_KEY, timestamp.ToString()));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.LOGNUM_KEY, logNum));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.ACTIVE_ACTION_KEY, activeAction));

		if (prevOutcome != null)
		{
			build.AppendFormat("\"{0}\": {1},", AutomationJSONKeys.PREV_OUTCOME, prevOutcome.ToString());
		}

		if (outcome != null)
		{
			build.AppendFormat("\"{0}\": {1},", AutomationJSONKeys.OUTCOME, outcome.ToString());
		}

		build.AppendFormat("{0}", JSON.createJsonString(AutomationJSONKeys.SPIN_NUMBER_KEY, spinNumber));

		return build.ToString();
	}
		
	// Gets the title of this log for display
	public string getTitle()
	{
		int titleLength = getTitleLength();

		if (titleLength == 0)
		{
			return "DEFAULT LOG TITLE";
		}
		return logMessage.Substring(0, titleLength);
	}

	public void updateMessage(string message)
	{
		this.logMessage = message;
		logTitle = getTitle();

	}

	// Gets the title of this log by taking the first line or the max length (whichever comes first).
	private int getTitleLength()
	{
		if (logMessage.Length == 0)
		{
			return 0;
		}
		int maxLength = (MAX_TITLE_LENGTH < logMessage.Length) ? MAX_TITLE_LENGTH : logMessage.Length;
		for (int i = 0; i < maxLength; i++)
		{
			if (logMessage[i] == '\n')
			{
				return i;
			}
		}
		return maxLength;
	}

	public static bool isValidWarning(string warning)
	{		
		foreach(string test in warningsToIgnore)
		{
			if(warning.Contains(test))
			{
				return false;
			}
		}

		return true;
	}

	public static bool isValidError(string error)
	{
		foreach(string test in errorsToIgnore)
		{
			if(error.Contains(test))
			{
				return false;
			}
		}

		return true;
	}
}
#endif
