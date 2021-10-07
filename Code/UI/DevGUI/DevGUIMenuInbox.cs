using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;

/*
A dev panel.
*/

public class DevGUIMenuInbox : DevGUIMenu
{
	private static string fakeGiftZid = "";
	private static string fakeGiftDesignator = "";
	private static string fakeGiftBonusGame = "";
	public static bool logToConsole = false;
	private static string consoleLog = "";
	private static bool sentGift = false;
	public static bool subtractAllCoinGiftsOnAccept;
	
	private static string trackingLog = "";
	private static string trackingDisplayLog = "";
	private static long logBufferSize;
	private static bool paused;
	private static bool doGiftLogging;
	private static string numGifts = "1";
	private string[] timeOverrides;
	private bool init = true;

	public override void drawGuts()
	{
		if (init)
		{
			timeOverrides = new string[InboxInventory.items.Count];
			for (int i = 0; i < InboxInventory.items.Count; i++)
			{
				timeOverrides[i] = "";
			}

			init = false;
		}

		GUILayout.BeginHorizontal();

		GUILayout.Label("zid");		
		fakeGiftZid = GUILayout.TextField(fakeGiftZid).Trim();
		if (GUILayout.Button("Use My Zid"))
		{
			fakeGiftZid = SlotsPlayer.instance.socialMember.zId.ToString();
		}
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();	
			
		GUILayout.Label("game designator");		
		fakeGiftDesignator = GUILayout.TextField(fakeGiftDesignator).Trim();
		if (GameState.game != null && !string.IsNullOrEmpty(GameState.game.keyName))
		{
			if (GUILayout.Button("Use CurrentGame"))
			{
				fakeGiftDesignator = GameState.game.keyName;

				SlotGameData gameData = SlotGameData.find(GameState.game.keyName);
				
				foreach(string bonusGameName in gameData.bonusGames)
				{
					BonusGame bonusGameData = BonusGame.find(bonusGameName);
					if (bonusGameData.gift)
					{
						fakeGiftBonusGame = bonusGameName;
					}
				}
			}
		}

		// TODO: add a dropdown of all games
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		
		GUILayout.Label("bonus game name");		
		fakeGiftBonusGame = GUILayout.TextField(fakeGiftBonusGame).Trim();
		// TODO: add some logic for a dropdown or something to auto-populate this field
		
		GUILayout.EndHorizontal();
		
		if (!string.IsNullOrEmpty(fakeGiftZid))
		{
			long zid = 0L;
			long.TryParse(fakeGiftZid, out zid);
			if (zid > 0 && !string.IsNullOrEmpty(fakeGiftDesignator) && !string.IsNullOrEmpty(fakeGiftBonusGame))
			{
				GUILayout.BeginHorizontal();
				numGifts = GUILayout.TextField(numGifts);
				if (GUILayout.Button("Send Gift"))
				{
					int giftCount;
					if (int.TryParse(numGifts, out giftCount))
					{
						for (int i = 0; i < giftCount; i++)
						{
							SendFakeGiftAction.sendGift(zid, fakeGiftDesignator, fakeGiftBonusGame);							
						}

						sentGift = true;
						/*GenericDialog.showDialog(
							Dict.create(
								D.TITLE, "SENT",
								D.MESSAGE, "The Fake Free Spin has been sent, unless you screwed up the parameters.",
								D.REASON, "dev-gui-fake-gift-sent"
							),
							SchedulerPriority.PriorityType.IMMEDIATE
						);*/
						//DevGUI.isActive = false;
					}
				}

				if (sentGift)
				{
					GUILayout.Label("gift sent for: " + fakeGiftBonusGame);
				}
				GUILayout.EndHorizontal();
			}

			if (zid > 0)
			{
				if (GUILayout.Button("Send Credits Gift"))
				{
					SendFakeGiftAction.sendCreditsGift(zid, 10000L);
				}

				if (GUILayout.Button("Send Ask For Credits"))
				{
					SendFakeGiftAction.sendAskForCredits(zid);
				}
				
				if (GUILayout.Button("Send Ask For Rating"))
				{
					SendFakeGiftAction.sendAskForRating(zid, fakeGiftBonusGame);
				}
			}
		}

		if (GUILayout.Button("Force Rating For Past Spins"))
		{
			InboxAction.forceRatingForPastSpins(ActionPriority.IMMEDIATE);
		}
		
		GUILayout.BeginVertical();
		for (int i = 0; i < InboxInventory.items.Count; i++)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label("Override time remaining (in seconds): ");
			GUILayout.Label(InboxInventory.items[i].itemType.ToString());

			if (i < timeOverrides.Length)
			{
				timeOverrides[i] = GUILayout.TextField(timeOverrides[i]).Trim();
				if (!string.IsNullOrEmpty(timeOverrides[i]))
				{
					int time = int.Parse(timeOverrides[i]);
					if (GUILayout.Button("Update time remaining"))
					{
						if (time > 0)
						{
							InboxInventory.items[i].overrideTimerExpiration(time);
						}
					}
				}
			}

			GUILayout.EndHorizontal();
		}

		GUILayout.EndVertical();

		subtractAllCoinGiftsOnAccept = GUILayout.Toggle(subtractAllCoinGiftsOnAccept, "Subtract All Coin Gifts On Accept");

		renderGiftChestInfo();
	}

	public void renderGiftChestInfo()
	{

	}

	public static void logGiftTrackingData(string data)
	{
		trackingLog += (data + "\n");

		if (logToConsole)
		{
			consoleLog += (data + "\n");
		}
	}
	
	public void handleEmailClick()
	{
		string subject = "Gifting Log for";
		sendDebugEmail(subject, trackingLog);
	}
	private void renderLog()
	{
		if (!paused && logBufferSize != trackingLog.Length)
		{
			if (trackingLog.Length > 16000)   // unity gets unhappy if it has to render more than this
			{
				trackingDisplayLog = trackingLog.Substring(trackingLog.Length - 16000);
			}
			else
			{
				trackingDisplayLog = trackingLog;
			}

			logBufferSize = trackingDisplayLog.Length;
		}

		GUILayout.BeginHorizontal();
		GUILayout.TextArea(trackingDisplayLog);
		GUILayout.EndHorizontal();
	}
	
	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
}
