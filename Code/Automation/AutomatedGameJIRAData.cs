using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;

#if ZYNGA_TRAMP
// This class holds information that should be going into a JIRA ticket.
// Author: Leo Schnee
// Date: 5/3/2017

public class AutomatedGameJIRAData : JIRAData
{
	private AutomatedCompanionLog gameLog = null; // Stores most of the debug information that we need to get for this JIRA ticket.
	private AutomatedGameIteration selectedGame = null;
	// Data Fields From AutomatedCompanionLog
	public string machineName = "??";
	public string branchName = "??";
	public string logTypeString = "Unknown";
	public int gameIterationNumber = -1;
	public int spinNumber = -1;
	public string timeStamp = "Some time...";
	public string issue = ""; // The logMessage
	public string stackTrace = ""; // The trace from the log.
	public string previousSpinServerMessages = "Not Stored";
	public string currentSpinServerMessages = "Not Stored";
	private AutomatedGameGistCreator outcomeGists = new AutomatedGameGistCreator();

	// Data Fields from AutomatedGameIteration
	public string gameKey = "Unknown01";
	public string gameName = "Unknown Game Of The JIRA";

	// OUTPUT CONSTS:
	private const string DESCRIPTION_FORMAT = @"
*Machine:* {0}
*Branch:* {1}
*Game:* {2} ({3})
*Game Iteration Number:* {4}
*Spin Number:* {5}
*Time Stamp:* {6}
*Issue:* {7}
*Notes:*
{8}
*Stack Trace:* 
{9}
*Outcomes:*
{10}
*General TRAMP info:*
{11}";

	private const string GENERAL_TRAMP_INFO = @"How to look at TRAMP: https://wiki.corp.zynga.com/display/hititrich/How+to+Run+TRAMP";

	private string createDescription()
	{

		// We want to try and make some GISTs here if we can.
		outcomeGists.description = "These are the outcomes for the TRAMP run of " + summary;
		outcomeGists.isPublic = false;
		bool useGist = false;
		if (previousSpinServerMessages != "Not Stored" || currentSpinServerMessages != "Not Stored")
		{
			// We have a message so lets try and make a gist.
			useGist = true;
		}
		string outcomeStrings = "Not Stored";

		if (useGist)
		{
			if (previousSpinServerMessages != "Not Stored")
			{
				// We have an outcme and we want to pretty print the JSON.
				var previousJSON = Zynga.Core.JsonUtil.Json.Deserialize(previousSpinServerMessages);
				previousSpinServerMessages = Zynga.Core.JsonUtil.Json.SerializeHumanReadable(previousJSON);
			}

			if (currentSpinServerMessages != "Not Stored")
			{
				// We have an outcme and we want to pretty print the JSON.
				var currentJSON = Zynga.Core.JsonUtil.Json.Deserialize(currentSpinServerMessages);
				currentSpinServerMessages = Zynga.Core.JsonUtil.Json.SerializeHumanReadable(currentJSON);
			}
			outcomeGists.addFile("Previous Outcome:", previousSpinServerMessages);
			outcomeGists.addFile("Current Outcome:", currentSpinServerMessages);
			outcomeGists.create();
			if (outcomeGists.gistURL != null)
			{
				outcomeStrings = outcomeGists.gistURL;
			}
		}

		string desc = string.Format(DESCRIPTION_FORMAT,
				machineName,
				branchName,
				gameKey, gameName,
				gameIterationNumber,
				spinNumber,
				timeStamp,
				issue,
				notes,
				stackTrace,
				outcomeStrings,
				GENERAL_TRAMP_INFO
			);
		return desc;
	}

	public override string createJSONStringforJIRA()
	{
		description = createDescription();
		return base.createJSONStringforJIRA();
	}

	public AutomatedGameJIRAData(AutomatedGameIteration game, AutomatedCompanionLog log, string branchName)
	{
		machineName = SystemInfo.deviceName;
		this.branchName = branchName;
		
		if (game != null && game.commonGame != null)
		{
			gameKey = game.commonGame.gameKey;
			gameName = game.commonGame.gameName;
			gameIterationNumber = game.gameIterationNumber;
		}

		if (log != null)
		{
			logTypeString = logTypeToString(log.logType);
			spinNumber = log.spinNumber;
			issue = log.logMessage;
			summary = log.logMessage;
			stackTrace = log.stackTrace;
			if (log.outcome != null)
			{
				currentSpinServerMessages = log.outcome.ToString();
			}
			if (log.prevOutcome != null)
			{
				previousSpinServerMessages = log.prevOutcome.ToString();
			}
			if (log.timestamp != null)
			{
				timeStamp = log.timestamp.ToLongTimeString();
			}
		}
	}

}

#endif
