using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*//
Class to deal with LoLa (lobby layout) data.
*/

public class LoLa : IResetGame
{
	public static string earlyAccessGameKey = "";
	public static string vipRevampNewGameKey = "";
	public static string newGameMotdKey = "";
	public static string newMVZGameMotdKey = "";
	public static string priorityGameKey = "";
	public static string priorityGameExp = "";

	public static List<string> sneakPreviewGameKeys = new List<string>();

	public static int earlyAccessMinLevel = 4;		// Hardcoded to 4 so it's 4 if LoLa isn't defining it.
	
	public static GameTimerRange sneakPreviewTimeRange = null;	// If the sneak preview feature is enabled.

	public const string LOLA_VERSION_EXPERIMENT_NAME = "lola_versioning";	// Server sends down the proper SKU as this name.
	private const string S3_DATA_RESPONSE_KEY = "lola_data";
	private const string S3_DATA_CACHE_FILE = "_lola_data";

	public static string version = "";
	public static string versionUrl = ""; // The url used to retrieve the LoLa data.

	private static HashSet<string> validGameKeys = new HashSet<string>();

	// Retrieve the LoLa config data from S3.
	public static IEnumerator getDataFromS3()
	{
		LoLaExperiment eos = ExperimentManager.GetEosExperiment(LOLA_VERSION_EXPERIMENT_NAME) as LoLaExperiment;
		
		if (eos == null)
		{
			Debug.LogError("Could not find LoLa experiment " + LOLA_VERSION_EXPERIMENT_NAME);
			yield break;
		}
		
		if (eos.version == "NONE")
		{
			Debug.LogError("No valid lola_version found in " + LOLA_VERSION_EXPERIMENT_NAME + " EOS experiment data.");
			yield break;
		}
		
		string url = Data.getFullUrl(versionUrl.Replace("version.txt", string.Format("{0}.txt", eos.version)));

		yield return RoutineRunner.instance.StartCoroutine(Server.attemptRequest(url, null, "", S3_DATA_RESPONSE_KEY, false, S3_DATA_CACHE_FILE));
		//yield return RoutineRunner.instance.StartCoroutine(Data.attemptServerRequest(url, null, "", S3_DATA_RESPONSE_KEY, false));

		JSON jsonData = Server.getResponseData(S3_DATA_RESPONSE_KEY);

		if (jsonData == null)
		{
			Debug.LogError("No contents in LoLa data request.");
		}
		else
		{
#if UNITY_EDITOR
			//Debug.LogWarning(jsonData.ToString());
#endif

			earlyAccessGameKey = jsonData.getString("early_access", "");
			vipRevampNewGameKey = jsonData.getString("new_vip_revamp_game_motd", "");
			newMVZGameMotdKey = jsonData.getString("new_mvz_game_motd", "");
			newGameMotdKey = jsonData.getString("new_game_motd", "");
			priorityGameKey = jsonData.getString("priority_game", "");
			priorityGameExp = jsonData.getString("priority_experiment", "");
			
			LoLaLobby.populateAll(jsonData);
			LoLaGame.populateAll(jsonData);
			LoLaAction.populateAll(jsonData);
			LoLaLobby.addWagerSets();

			MOTDDialogDataNewGame data = MOTDDialogData.newGameMotdData;
			if (data != null)
			{
				data.handleLolaNewGame(newGameMotdKey);
			}
		}
	}
	public static void registerActiveGame(string gameKey)
	{
		if (!validGameKeys.Contains(gameKey))
		{
			validGameKeys.Add(gameKey);
		}
	}

	public static bool doesGameExistInLobby(string gameKey)
	{
		return validGameKeys.Contains(gameKey);
	}

	public static void resetStaticClassData()
	{
		earlyAccessGameKey = "";
		sneakPreviewGameKeys = new List<string>();
		earlyAccessMinLevel = 4;
		sneakPreviewTimeRange = null;
		validGameKeys.Clear();
		version = "";
		versionUrl = "";
		sneakPreviewTimeRange = null;
	}
}
