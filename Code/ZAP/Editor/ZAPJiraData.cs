using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;

namespace Zap.Automation
{
	#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	public class ZAPJiraData : JIRAData
	{
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

		public ZAPJiraData()
		{

		}

		//This is the equivalent call from the old TRAMP jira ticket.
		public ZAPJiraData(AutomatableResult result, ZapLog log, string branchName)
		{
			machineName = SystemInfo.deviceName;
			this.branchName = branchName;

			if (result != null)
			{
				gameKey = result.automatableKey;
				gameName = result.automatableName;
			}

			if (log != null)
			{
				logTypeString = "<" + log.logType.ToString() + ">";
				issue = log.message;
				summary = log.message;
				stackTrace = log.stackTrace;
				foreach (KeyValuePair<string, string> kvp in log.additionalInfo)
				{
					if (kvp.Key.Equals("CURRENT_OUTCOME"))
					{
						currentSpinServerMessages = kvp.Value;
					}
				}

				timeStamp = log.timestamp.ToLongTimeString();
			}
		}

		//May want to create a Jira ticket with data from the entire testplan run
		//Not sure what information we'd want to include in this.  All outcomes? All logs? The error, exception, warning count? Crash count.
		//If the Test plan was all the games this may be way too much info for one ticket.
		public ZAPJiraData(TestPlanResults results, ZapLog log)
		{
			machineName = SystemInfo.deviceName;
			this.branchName = results.gitBranch;

			if (results != null)
			{
				foreach(AutomatableResult autoResult in results.automatableResults)
				{
					gameKey += autoResult.automatableKey + ", ";
					gameName += autoResult.automatableName + ", ";
				}				
			}

			if (log != null)
			{
				logTypeString = "<" + log.logType.ToString() + ">";
				issue = log.message;
				summary = log.message;
				stackTrace = log.stackTrace;
				if (log.additionalInfo != null)
				{
					foreach (KeyValuePair<string, string> kvp in log.additionalInfo)
					{
						if (kvp.Key.Equals("CURRENT_OUTCOME"))
						{
							currentSpinServerMessages = kvp.Value;
						}
					}
				}
				timeStamp = log.timestamp.ToLongTimeString();
			}
		}
		
		//Create a ticket based on a single automatable.
		public ZAPJiraData(AutomatableResult result, ZapLog log)
		{
			machineName = SystemInfo.deviceName;			
			
			if(result!=null)
			{
				gameKey = result.automatableKey;
				gameName = result.automatableName;
			}		
		}

		//Create a ticket based on a single test result.
		public ZAPJiraData(TestResult result, ZapLog log)
		{
			machineName = SystemInfo.deviceName;			

			if (result != null)
			{
				gameKey = result.parentAutomatableKey;
			}

			if (log != null)
			{
				logTypeString = "<" + log.logType.ToString() + ">";
				issue = log.message;
				summary = log.message;
				stackTrace = log.stackTrace;
				foreach (KeyValuePair<string, string> kvp in log.additionalInfo)
				{
					if (kvp.Key.Equals("CURRENT_OUTCOME"))
					{
						currentSpinServerMessages = kvp.Value;
					}
				}

				timeStamp = log.timestamp.ToLongTimeString();
			}
		}
	}
	#endif
}
