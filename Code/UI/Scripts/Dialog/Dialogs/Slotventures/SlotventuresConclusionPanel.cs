using UnityEngine;
using System.Collections;
using TMPro;

public class SlotventuresConclusionPanel : MonoBehaviour, IResetGame
{
	public ButtonHandler continueButton;
	public TextMeshPro textMeshMessage;
	private GameTimerRange killTime;
	private GameTimerRange dismissTime;

	public static ChallengeCampaign slotventuresCampaign;
	public static SlotventuresLobby lobbyInstance;

	private const int CONCLUSION_Z = 400;
	private const int PROGRESS_Z = 200;
	private const int DISMISS_TIME = 5;
	private const int KILL_TIME = 2;
	private const int ONSCREEN_TWEEN_POINT = 740;

	private void Awake()
	{
		// Grab stuff we need
		lobbyInstance = SlotventuresLobby.instance as SlotventuresLobby;
		slotventuresCampaign = CampaignDirector.find(SlotventuresChallengeCampaign.CAMPAIGN_ID);

		int secondsLeft = slotventuresCampaign.timerRange.endTimestamp - GameTimer.currentTime;

		// The continue button isnt on the ending soon prefab
		if (continueButton != null)
		{
			continueButton.registerEventDelegate(onClickClose);
		}

		// If the campaign is still going
		if (slotventuresCampaign.timerRange != null && slotventuresCampaign.timerRange.timeRemaining > 0)
		{
			// If it's complete or on the last mission (the collect one)
			if (slotventuresCampaign.state == ChallengeCampaign.COMPLETE || slotventuresCampaign.currentEventIndex >= slotventuresCampaign.missions.Count - 1)
			{
				textMeshMessage.text = Localize.text("slotventure_complete");
			}
			else
			{
				textMeshMessage.text = Localize.text("event_ends_in_{0}_minutes", secondsLeft / Common.SECONDS_PER_MINUTE);
				dismissTime = new GameTimerRange(GameTimer.currentTime, GameTimer.currentTime + DISMISS_TIME);
				dismissTime.registerFunction(onDismiss);
			}
		}
		else
		{
			textMeshMessage.text = Localize.text("the_event_has_ended");
		}

		// Tween this up. At this point the progress panel has been tweened down ideally
		iTween.MoveTo(gameObject, iTween.Hash("y", ONSCREEN_TWEEN_POINT, "z", CONCLUSION_Z, "time", 1, "islocal", true, "easetype", iTween.EaseType.linear));

		if (slotventuresCampaign != null && slotventuresCampaign.timerRange.timeRemaining > 0)
		{
			Audio.playWithDelay("EventEndToastSlotVenturesCommon", 1.0f);
		}
	}

	// Go back to the main lobby of the event is over in any way. Otherwise just destroy this.
	private void onKill(Dict args = null, GameTimerRange sender = null)
	{
		if (slotventuresCampaign == null || slotventuresCampaign.timerRange.timeRemaining <= 0 || slotventuresCampaign.state == ChallengeLobbyCampaign.COMPLETE)
		{
			// Back to the main lobby, now
			LobbyLoader.lastLobby = LobbyInfo.Type.MAIN;
			NGUIExt.disableAllMouseInput();
			Loading.show(Loading.LoadingTransactionTarget.LOBBY);
			Glb.loadLobby();
			if (Audio.currentMusicPlayer != null && Audio.currentMusicPlayer.isPlaying)
			{
				if (Audio.currentMusicPlayer.relativeVolume < 0.01f)
				{
					Audio.switchMusicKeyImmediate("");
				}
			}

			Audio.stopAll();
			Audio.removeDelays();
			Audio.listenerVolume = Audio.maxGlobalVolume;
			Audio.play("return_to_lobby");
		}

		if (this != null && gameObject != null)
		{
			Destroy(gameObject);
		}
	}

	// Move this down and the progress panel back up if it's there. Or if we're out of time go back to main lobby
	private void onDismiss(Dict args = null, GameTimerRange sender = null)
	{
		// Get us out of here if the campaign is null or we're out of time
		if (slotventuresCampaign == null || slotventuresCampaign.timerRange == null || slotventuresCampaign.timerRange.timeRemaining <= 0 || slotventuresCampaign.state == ChallengeLobbyCampaign.COMPLETE)
		{
			onKill();
			return;
		}

		// Start moving this back down
		iTween.MoveTo(gameObject, iTween.Hash("y", 0, "time", 1, "islocal", true, "easetype", iTween.EaseType.linear));

		if (lobbyInstance != null && lobbyInstance.progressPanel != null)
		{
			// Move progress panel back
			iTween.MoveTo(lobbyInstance.progressPanel.gameObject, iTween.Hash("y", 0, "z", PROGRESS_Z, "time", 1, "islocal", true, "easetype", iTween.EaseType.linear));
		}

		// Delete this after 2 more seconds
		killTime = new GameTimerRange(GameTimer.currentTime, GameTimer.currentTime + KILL_TIME);
		killTime.registerFunction(onKill);
	}

	private void onClickClose(Dict args = null)
	{
		onDismiss();
	}

	public static void resetStaticClassData()
	{
		slotventuresCampaign = null;
		lobbyInstance = null;
	}

	private void OnDestroy()
	{
		if (killTime != null)
		{
			killTime.clearEvent();
		}
		if (dismissTime != null)
		{
			dismissTime.clearEvent();
		}

		killTime = null;
		dismissTime = null;
	}
}
