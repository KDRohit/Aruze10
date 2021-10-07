using PrizePop;
using UnityEngine;

public class DevGUIMenuPrizePop : DevGUIMenu
{
	private const string TEST_DATA_DIRECTORY = "Test Data/PrizePop/";
	private const string LOGIN_FILE = "login";
	private static bool useFakeData = false;
	private static int pointsToIncrement = 1;
	private static int picksToAdd = 1;
	private static int creditsToWin = 0;
	private static bool isJackpotPick = false;
	private static bool forceBonus = false;
	private static int economyVersion = 1;
	private static int pickObjectIndex = 0;
	private static int newStartTime = -1;
	private static int newEndTime = -1;
	private static string forceEosVariant = "";
	private static bool forceReset = false;

	public override void drawGuts()
	{
		if (useFakeData)
		{
			if (GUILayout.Button("Turn off Feature"))
			{
				PrizePopFeature.resetStaticClassData();
				useFakeData = false;
			}
		}
		else
		{
			if (GUILayout.Button("Initialize with Fake Data"))
			{
				useFakeData = true;
				
				//send login data
				JSON data = getFakeLoginData();
				PrizePopFeature.instantiateFeature(data);
			}
		}
		
		if (PrizePopFeature.instance == null)
		{
			drawFeatureDataMissing();
		}
		else if (PrizePopFeature.instance != null && !PrizePopFeature.instance.isEnabled)
		{
			drawFeatureNotActive();
		}
		else
		{
			drawFeatureEnabled();
		}
	}
	
	private JSON getFakeLoginData()
	{
		string testDataPath = TEST_DATA_DIRECTORY + LOGIN_FILE;
		TextAsset textAsset = (TextAsset)Resources.Load(testDataPath,typeof(TextAsset));
		string text = textAsset.text;
		//update start/end time to be current
		text = text.Replace("\"end_time\" : 0000000", "\"end_time\" : " +   (GameTimer.currentTime + (60 * 60)));
		return new JSON(text);
	}

	private void drawFeatureDataMissing()
	{
		GUILayout.BeginHorizontal();
		//Add a final check here to see if we aren't currently on a team
		GUILayout.TextArea("Feature not initialized");
		GUILayout.EndHorizontal();
	}

	private void drawFeatureNotActive()
	{
		GUILayout.BeginHorizontal();
		//Add a final check here to see if we aren't currently on a team
		GUILayout.TextArea("Feature isn't active because: " + System.Environment.NewLine + PrizePopFeature.instance.getInactiveReason());
		GUILayout.EndHorizontal();
	}

	private void drawFeatureEnabled()
	{
		drawStatus();
		drawControls();
		drawPickHistory();
	}

	private void drawStatus()
	{
		GUILayout.BeginVertical();
		drawField("Current Points", PrizePopFeature.instance.currentPoints.ToString());
		drawField("Required Points", PrizePopFeature.instance.maximumPoints.ToString());
		drawField("Meter Fills", PrizePopFeature.instance.meterFillCount.ToString());
		drawField("Maximum Meter Fills", PrizePopFeature.instance.maximumMeterFills.ToString());
		drawField("Available Picks", PrizePopFeature.instance.numPicksAvailable.ToString());
		drawPickHistory();
		GUILayout.EndVertical();
	}
	
	private void drawField(string fieldName, string fieldValue)
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label(fieldName, new GUILayoutOption[]{ GUILayout.Width(200) });
		GUILayout.Label(fieldValue);
		GUILayout.EndHorizontal();
	}

	private void drawPickHistory()
	{
		GUILayout.BeginVertical();
		GUILayout.EndVertical();
	}

	private void drawControls()
	{
		//layout option for text fields
		GUILayoutOption[] optionsText = new GUILayoutOption[]
		{
			GUILayout.Height(18),
			GUILayout.Width(200),
			GUILayout.ExpandWidth(false)
		};

		GUILayoutOption[] optionsButton = new GUILayoutOption[]
		{
			GUILayout.Width(300),
			GUILayout.ExpandWidth(true)
		};
		
		//draw in a vertical stack
		GUILayout.BeginVertical();
		
		//line 0
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Expire Timer", optionsButton))
		{
			//TODO: make this actually expire the timer
			PrizePopFeature.instance.disableFeature();
			InGameFeatureContainer.refreshDisplay(InGameFeatureContainer.PRIZE_POP_KEY);
		}
		GUILayout.EndHorizontal();
		
		//line 1
		GUILayout.BeginHorizontal();
		GUILayout.BeginVertical();
		GUILayout.Label("Points to Add:", optionsText);
		string tempText = GUILayout.TextField(pointsToIncrement.ToString(), optionsText);
		if (!System.Int32.TryParse(tempText, out pointsToIncrement))
		{
			pointsToIncrement = 1;
		}
		GUILayout.EndVertical();
		if (GUILayout.Button("Increment Meter Points", optionsButton))
		{
			if (useFakeData)
			{
				PrizePopFeature.instance.incrementPoints(pointsToIncrement);	
			}
			else
			{
				PrizePopAction.devAddMeterProgress(pointsToIncrement);
			}
		}
		GUILayout.EndHorizontal(); // line 1
		
		//line 2
		GUILayout.BeginHorizontal();
		GUILayout.BeginVertical();
		GUILayout.Label("Picks to Add:", optionsText);
		tempText = GUILayout.TextField(picksToAdd.ToString(), optionsText);
		if (!System.Int32.TryParse(tempText, out picksToAdd))
		{
			picksToAdd = 1;
		}
		GUILayout.EndVertical();
		if (GUILayout.Button("Increment Picks Available", optionsButton))
		{
			if (useFakeData)
			{
				PrizePopFeature.instance.incrementPickCount();
			}
			else
			{
				PrizePopAction.devAddPicks(picksToAdd);
			}
		}
		GUILayout.EndHorizontal(); // line 2
		
		//Line 3
		GUILayout.BeginHorizontal();
		if (useFakeData)
		{
			//This is only used for fake data, so hide it if we're testing real server communication
			GUILayout.BeginVertical();
			GUILayout.Label("Coin Value:", optionsText);
			tempText = GUILayout.TextField(creditsToWin.ToString(), optionsText);
			if (!System.Int32.TryParse(tempText, out creditsToWin))
			{
				creditsToWin = 1;
			}

			GUILayout.EndVertical();

			GUILayout.BeginVertical();
			GUILayout.Label("Is Jackpot Win", optionsText);
			isJackpotPick = GUILayout.Toggle(isJackpotPick, "Is Jackpot", optionsText);
			GUILayout.EndVertical();
		}

		GUILayout.BeginVertical();
		GUILayout.Label("Pick Object Index:", optionsText);
		tempText = GUILayout.TextField(pickObjectIndex.ToString(), optionsText);
		if (!System.Int32.TryParse(tempText, out pickObjectIndex))
		{
			pickObjectIndex = 0;
		}
		GUILayout.EndVertical();
		
		if (GUILayout.Button("Make Debug Pick", optionsButton))
		{
			if (useFakeData)
			{
				PrizePopFeature.instance.makePick(pickObjectIndex, true);
			}
			else
			{
				PrizePopFeature.instance.makePick(pickObjectIndex, false);
			}
		}
		GUILayout.EndHorizontal(); //line 3
		
		//line 4
		GUILayout.BeginHorizontal();
		GUILayout.BeginVertical();
		GUILayout.Label("Force Full Meter", optionsText);
		forceBonus = GUILayout.Toggle(forceBonus, "Force Full Meter", optionsText);
		GUILayout.EndVertical();
		
		if (GUILayout.Button("Start Bonus Game", optionsButton))
		{
			if (forceBonus)
			{
				//fill the meter here
			}
			PrizePopFeature.instance.startBonusGame(true, true);
			if (useFakeData)
			{
				//Just open the dialog
			}
			else
			{
				PrizePopAction.devStartBonusGame(forceBonus);
			}
		}
		GUILayout.EndHorizontal(); 
		// line 4

		//Don't bother allowing trying to swap the econonmy version if we're not using real data
		if (!useFakeData)
		{
			//line 5
			GUILayout.BeginHorizontal();
			GUILayout.BeginVertical();
			GUILayout.Label("Economy Version (Integer ID)", optionsText);
			tempText = GUILayout.TextField(economyVersion.ToString(), optionsText);
			if (!System.Int32.TryParse(tempText, out economyVersion))
			{
				economyVersion = 1;
			}

			GUILayout.EndVertical();

			if (GUILayout.Button("Set Economy Version", optionsButton))
			{
				PrizePopAction.devSetEconomyVersion(economyVersion);
			}

			GUILayout.EndHorizontal();
			// line 5
		}
		
		GUILayout.BeginHorizontal();
		GUILayout.BeginVertical();
		GUILayout.Label("Force New START Time", optionsText);
		tempText = GUILayout.TextField(newStartTime.ToString(), optionsText);
		if (!System.Int32.TryParse(tempText, out newStartTime))
		{
			newStartTime = -1;
		}
		GUILayout.EndVertical();
		
		GUILayout.BeginVertical();
		GUILayout.Label("Force New END Time", optionsText);
		tempText = GUILayout.TextField(newEndTime.ToString(), optionsText);
		if (!System.Int32.TryParse(tempText, out newEndTime))
		{
			newEndTime = -1;
		}
		GUILayout.EndVertical();
		
		GUILayout.BeginVertical();
		GUILayout.Label("Force EOS Variant", optionsText);
		forceEosVariant = GUILayout.TextField(forceEosVariant, optionsText);
		GUILayout.EndVertical();
		
		GUILayout.BeginVertical();
		GUILayout.Label("Force Reset Feature", optionsText);
		forceReset = GUILayout.Toggle(forceReset, "Force Reset Feature", optionsText);
		GUILayout.EndVertical();

		if (GUILayout.Button("Reinitialize Feature", optionsButton))
		{
			PrizePopAction.devInitialize(newStartTime, newEndTime, forceEosVariant, forceReset);
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Clear Extra Picks", optionsButton))
		{
			PrizePopAction.devClearExtraPicks();
		}
		
		if (GUILayout.Button("Reset Meter", optionsButton))
		{
			PrizePopAction.devResetMeter();
			PrizePopFeature.instance.clearPointsAndMeterFill();
		}
		
		if (GUILayout.Button("Reset Current Round", optionsButton))
		{
			PrizePopAction.devResetRound();
		}
		
		if (GUILayout.Button("Reset Bonus Game", optionsButton))
		{
			PrizePopAction.devResetBonusGame();
		}
		
		if (GUILayout.Button("Open Dialog", optionsButton))
		{
			PrizePopDialog.showDialog(true);
		}
		GUILayout.EndHorizontal();

		//close main container
		GUILayout.EndVertical();
	}
	
	public new static void resetStaticClassData()
	{
		useFakeData = false;
		pointsToIncrement = 1;
		creditsToWin = 0;
		isJackpotPick = false;
	}
}
