using UnityEngine;
using System.Collections;

public static class Sharing  
{
	private const string SCREENSHOT_FILENAME = "game_event_screenshot.png";
	private const string DEFAULT_URL = "www.zynga.com";
	private static bool  isNativeSharingExecuting;  // true if native sharing coroutine is in progress

	public static bool isAvailable
	{
		get 
		{
		// tbd do I need this sku wrapper? I don't think so, for HIR experiment will always return false
#if ZYNGA_SKU_SIR
			return ExperimentWrapper.NativeMobileSharing.isInExperiment;
#else	
			return false;
#endif	
		}
	}

	// subjectKey, 		// SCAT id of the subject title for the  share, such as subject line in email
	// eventName, 		// name of the share event, such as "Big Win". This will be inserted into subjectKey and bodyKey at {0}
	// body, 			// custom text for the body, if null bodyKey is used instead
	// bodyKey, 		// SCAT id of body text usually has a {0} in it when resolved, such as "I won a Jackpot on {0}!"
	// statFamily, 		// stat family for stat gui click tracking
	// srcGame   		// the name of the game associated with the share, if empty will be set from Gamestate, inserted into subjectKey at {1}
	public static void shareGameEventWithScreenShot(
		string subjectKey, 		
		string eventName, 		
		string body, 			
		string bodyKey, 		
		string statFamily, 		
		string srcGame = "")   
	{
		if (isNativeSharingExecuting)
		{
			return;     // in case users are slamming the share button repreatadly before the share couroutine has finished
		}

		// set the final name of the game
		string gameName = "";
 		if (srcGame != "")
 		{
 			gameName = srcGame;
 		}
 		else if (GameState.game != null)
  		{
  			gameName = GameState.game.name;
 		}

 		// set up the subject line
		string subject = "";
		if (!string.IsNullOrEmpty(eventName))
		{
			subject = Localize.text(subjectKey, eventName, gameName);
		}
		else
		{
			subject = Localize.text(subjectKey, gameName);
		}

		// setup the body text
		if (body == "")
		{
			if (!string.IsNullOrEmpty(eventName))
			{
				body = Localize.text(bodyKey, eventName, gameName);
			}
			else
			{
				body = Localize.text(bodyKey, gameName);
			}
		}
		
		// setup the store url
		string storeURL = Data.liveData.getString("NATIVE_SHARE_SHEET", DEFAULT_URL, DEFAULT_URL);

		// post to stats
		if (!string.IsNullOrEmpty(statFamily))
		{
			string trackedGameName = StatsManager.getGameName();
	 		if (srcGame != "")
	 		{
	 			trackedGameName = srcGame;
	 		}

			StatsManager.Instance.LogCount("dialog", statFamily, StatsManager.getGameTheme(), trackedGameName, "view", "share_sheet");
		}

		isNativeSharingExecuting = true;
		RoutineRunner.instance.StartCoroutine(waitForCaptureScreenThenShare(subject, body, storeURL));
	}		

	public static IEnumerator waitForCaptureScreenThenShare(string subject, string body, string storeURL)
	{
		// take a screenshot
		ScreenCapture.CaptureScreenshot(SCREENSHOT_FILENAME);		

		//it's not enough to just check that the file exists, since it doesn't mean it's finished saving
        //we have to check if it can actually be opened
        bool didImageLoad = false;
        float timeTaken = 0;
        string filePath = Application.persistentDataPath + "/" + SCREENSHOT_FILENAME;

#if UNITY_EDITOR
		filePath = SCREENSHOT_FILENAME;
#endif

    	while (!didImageLoad && timeTaken < 10.0f)
    	{
    		WWW fileLoader = new WWW("file://" + filePath);
    		yield return fileLoader;
			if (fileLoader.error == null)
			{
			    Texture2D screenshotTexture = fileLoader.texture;
				if (screenshotTexture != null)
				{
				    didImageLoad = true;
					break;
				}				
			}
    		timeTaken += Time.deltaTime;

			yield return new WaitForSeconds(0.25f);
		}

		yield return new WaitForSeconds(Data.liveData.getFloat("NATIVE_SHARE_SCREEN_CAP_DELAY", 0.0f));

		// do native sharing
		if (didImageLoad)
		{
#if UNITY_EDITOR
			Debug.Log("Reached Native sharing binding call with parms : " + "\n" +
				"Subject : " + subject + "\n" +
				"Body : " + body + "\n" +
				"URL : " + storeURL);
#else
			NativeBindings.ShareContent(subject, body, SCREENSHOT_FILENAME, storeURL);
#endif
		}
		
		isNativeSharingExecuting = false;
	}	

}
