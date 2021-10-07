// outdated file, please look at DesyncAction.cs

using UnityEngine;
using System.Collections;
using Com.Scheduler;

#if !ZYNGA_PRODUCTION
public class JIRADesyncErrorSubmitter : MonoBehaviour, IResetGame
{
	private static string previousSlotsOutcome;
	private static string latestSlotsOutcome;
	private static JSON loginData;

	// Set by PlayerResource.checkResourceChange.
	public static long clientExpectedCredits;
	public static long serverExpectedCredits;

	private static string kSubmitJiraPrefs = "PlayerPrefs.SubmittingJira";
	private static string kSubmitJiraGistIdPrefs = "PlayerPrefs.SubmittingJiraGistId";
	private static string jiraTicketKey;

	private static JIRADesyncErrorSubmitter instance = null;

	public static bool submittingJira
	{
		// Skip saving/reset on WebGL; writing data to player prefs is undependable (Safari).
		#if UNITY_WEBGL
		get { return false; }
		set {}
		#else
		get
		{
			return PlayerPrefsCache.GetInt(kSubmitJiraPrefs) == 1;
		}
		set
		{
			if (value)
			{
				PlayerPrefsCache.SetInt(kSubmitJiraPrefs, 1);
			}
			else
			{
				PlayerPrefsCache.SetInt(kSubmitJiraPrefs, 0);
			}
		}
		#endif
	}

	public static string gistIDForJira
	{
		// Skip saving/reset on WebGL; writing data to player prefs is undependable (Safari).
		#if UNITY_WEBGL
		get { return ""; }
		set {}
		#else
		get
		{
			return PlayerPrefsCache.GetString(kSubmitJiraGistIdPrefs);
		}
		set
		{
			if (!string.IsNullOrEmpty(value))
			{
				PlayerPrefsCache.SetString(kSubmitJiraGistIdPrefs, value);
			}
			else
			{
				PlayerPrefsCache.DeleteKey(kSubmitJiraGistIdPrefs);
			}
		}
		#endif
	}

	public static void reportDesyncError(Dict args)
	{
		if ((string)args.getWithDefault(D.ANSWER, "") == "2")
		{
			if (instance == null)
			{
				GameObject go = new GameObject("JiraDesyncErrorSubmitter");
				DontDestroyOnLoad(go);
				instance = go.AddComponent<JIRADesyncErrorSubmitter>();
			}
			instance.StartCoroutine(reportDesyncErrorAsync(args));
		}
	}

	private static IEnumerator reportDesyncErrorAsync(Dict args)
	{
		Debug.Log("Reporting desync error");

		yield return submitJira();
		string message;
		bool success = false;
		if (!string.IsNullOrEmpty(jiraTicketKey))
		{
			message = string.Format("Created Issue for desync: {0}", jiraTicketKey);
			success = true;
		}
		else
		{
			message = "Error creating issue";
		}
		Dict dict = Dict.create(
								D.TITLE, "Submitted Jira Issue",
								D.MESSAGE, message);

		// Do not reset game on WebGL because the canvas gets reset, so the game is completely restarted, and
		// writing data to player prefs is undependable (Safari).
		#if !UNITY_WEBGL
		if (success)
		{
			dict.merge(D.CALLBACK, new DialogBase.AnswerDelegate(resetToGetNewLoginData));
			message += "Will reset to get new login data";
		}
		#endif

		GenericDialog.showDialog(dict, SchedulerPriority.PriorityType.IMMEDIATE);
	}

	public static void resetToGetNewLoginData(Dict args)
	{
		// Reset the game to get new login data.  Store off old login data in static var and mark function to be
		// called after login to get new data and finish the jira submission work.
		Glb.resetGame("Getting new login data for desync error JIRA report");
	}

	public static void receiveServerOutcomes(string message)
	{
		if (!string.IsNullOrEmpty(message))
		{

			JSON serverResponseJson = new JSON(message);
			JSON[] events = serverResponseJson.getJsonArray("events");
			bool isSlotOutcome = false;
			for (int i = 0; i < events.Length; i++)
			{
				JSON currentEvent = events[i];
				string typeString = currentEvent.getString("type", "");
				if (typeString == "slots_outcome")
				{
					isSlotOutcome = true;
					break;
				}
			}
			if (isSlotOutcome)
			{
				previousSlotsOutcome = latestSlotsOutcome;
				latestSlotsOutcome = message;
			}
		}
	}

	// Should be common functionality somewhere?
	private static string IndentJsonString(string jsonString)
	{
		object jsonObject = Zynga.Core.JsonUtil.Json.Deserialize(jsonString);
		string indentedString = Zynga.Core.JsonUtil.Json.SerializeHumanReadable(jsonObject);
		return indentedString;
	}

	// Implements IResetGame
	public static void resetStaticClassData()
	{
		clientExpectedCredits = 0;
		serverExpectedCredits = 0;
	}

	public static void onLogin(JSON newLoginData)
	{
		if (submittingJira)
		{
			updateGist(newLoginData);
			submittingJira = false;
			loginData = newLoginData;
		}
		else
		{
			loginData = newLoginData;
			previousSlotsOutcome = "";
			latestSlotsOutcome = "";
		}
	}

	private static IEnumerator submitJira()
	{
		string desyncGameKey = "None";
		string desyncGameName = "None";
		string zid = "Unknown";
		string serverUrl;
		string splunkUrl;

		if (GameState.game != null)
		{
			desyncGameKey = GameState.game.keyName;
			desyncGameName = GameState.game.name;
		}

		if (SlotsPlayer.instance != null && SlotsPlayer.instance.socialMember != null)
		{
			zid = SlotsPlayer.instance.socialMember.zId;
		}

		System.DateTime desyncTime = System.DateTime.Now;
		int desyncTimeUnix = Common.dateTimeToUnixSecondsAsInt(desyncTime.ToUniversalTime());
		splunkUrl = string.Format("https://search.splunk.zynga.com/en-US/app/searchville-socialslots/hit_it_rich_user_dive?form.stage=socialslotsweb_staging&form.time_range.earliest={0}&form.time_range.latest={1}&form.zid={2}", 
		(desyncTimeUnix - 600), (desyncTimeUnix + 600), zid);

		serverUrl = Data.serverUrl;

		// Make new JIRA data.
		JIRAData jiraData = new JIRAData();
		jiraData.summary = string.Format("Desync error reported on client version {0} in {1} ({2})", Glb.clientVersion, desyncGameKey, desyncGameName);
		jiraData.priority = JIRAPriority.TBD;
		jiraData.issueType = "Bug";
		string descriptionSummary = string.Format("Desync error reported in game {0}.\nClient expected {1}, server {2}. Difference from server of {3}",
												  desyncGameKey, clientExpectedCredits, serverExpectedCredits, (serverExpectedCredits - clientExpectedCredits));
		string descriptionOtherInfo = string.Format("Server URL: {0}\nPlayer zid: {1}\nSplunk Userflows: {2}", serverUrl, zid, splunkUrl);

		// Make new Gist creator and content.
		AutomatedGameGistCreator gistMaker = new AutomatedGameGistCreator();
		gistMaker.description = jiraData.summary + ": Files";
		gistMaker.isPublic = false;
		string prettyLatestSlotOutcome = IndentJsonString(latestSlotsOutcome);
		string prettyPreviousSlotOutcome = string.IsNullOrEmpty(previousSlotsOutcome) ? "None" : IndentJsonString(previousSlotsOutcome);

		gistMaker.addFile("1.LatestSlotsOutcome.json", prettyLatestSlotOutcome);
		gistMaker.addFile("2.PreviousSlotsOutcome.json", prettyPreviousSlotOutcome);
		yield return gistMaker.create(); // Create gist.

		if (gistMaker.error)
		{
			// Error creating gist so can't link it, so put as much as we can in description directly.
			Debug.LogError("Error creating gist for Jira, putting outcome info in Jira description instead");
			jiraData.description = string.Format("{0}\n{1}\nLast slot outcome:\n{2}\nPrevious slot outcome:\n{3}",
												 descriptionSummary, descriptionOtherInfo, prettyLatestSlotOutcome, prettyPreviousSlotOutcome);
		}
		else
		{
			// Set description now that is filled out.
			jiraData.description = string.Format("{0}\n{1}\n\n\nFull outcome and login output here: {2}",
												 descriptionSummary, descriptionOtherInfo, gistMaker.gistURL);
		}

		// Debug.Log(jiraData.createJSONStringforJIRA());

		// Actually make the ticket.
		yield return jiraData.sendRequest();
		jiraTicketKey = jiraData.resultJSON.getString("key", "");

		submittingJira = true;
		gistIDForJira = gistMaker.gistID;
	}

	private static IEnumerator updateGist(JSON newLoginData)
	{
		if (loginData == null || string.IsNullOrEmpty(gistIDForJira))
		{
			// Shouldn't ever happen, something is wrong so just bail.
			Debug.LogError("JIRADesyncErrorSubmitter is missing required data, not updating report.");
			submittingJira = false;
			gistIDForJira = "";
			yield break;
		}

		// Compare new login data to old login data
		JSON loginDiff = JSON.getDiff(loginData, newLoginData);
		string prettyLoginDiff = IndentJsonString(loginDiff.ToString());

		AutomatedGameGistCreator gistMaker = new AutomatedGameGistCreator();
		yield return gistMaker.populateFromGistId(gistIDForJira);

		gistMaker.addFile("3.LoginDataDiff", IndentJsonString(prettyLoginDiff));
		gistMaker.addFile("4.OldLoginData.json", IndentJsonString(loginData.ToString()));
		gistMaker.addFile("5.NewLoginData.json", IndentJsonString(newLoginData.ToString()));

		yield return gistMaker.editGist();
	}
}
#endif
