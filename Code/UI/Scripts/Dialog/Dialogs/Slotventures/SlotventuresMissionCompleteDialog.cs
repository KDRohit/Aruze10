using UnityEngine;
using System.Collections;
using Com.Scheduler;
using TMPro;

public class SlotventuresMissionCompleteDialog : DialogBase
{
	public TextMeshPro goalsCompleteText;
	public GameObject panelAnchor;
	private const int RETURN_TO_LOBBY_DELAY = 8;

	public override void init()
	{
		Audio.play(SlotventuresLobby.assetData.audioMap[LobbyAssetData.ON_MISSION_COMPLETE]);
		NGUIExt.disableAllMouseInput();
	}

	protected override void onFadeInComplete()
	{
		StartCoroutine(waitAndShow());
		StartCoroutine(CommonEffects.fadeSpritesAndText(goalsCompleteText.gameObject, 255.0f, 2.0f));
		base.onFadeInComplete();
	}

	private IEnumerator waitAndShow()
	{
		if (SpinPanel.hir != null && SpinPanel.hir.objectivesGrid != null)
		{
			yield return new WaitForSeconds(0.1f);
			SpinPanel.hir.objectivesGrid.gameObject.transform.parent = panelAnchor.transform;
			SpinPanel.hir.objectivesGrid.gameObject.transform.localPosition = Vector3.zero;
			SpinPanel.hir.objectivesGrid.refresh(dialogArgs[D.OPTION1] as Mission);
			yield return new WaitForSeconds(RETURN_TO_LOBBY_DELAY);
			onTimeOut();
		}
		else
		{
			Dialog.close(this);
		}
	}

	private void onTimeOut(Dict args = null, GameTimerRange sender = null)
	{
		// Clear the game state, we aren't in a game any more since we are going to SlotVenture Lobby.
		// When we go to slotventure lobby, we might display lootbox dialog which might dispatch powerup in the lootbox.
		// if we do not clear game stack here, powerup panel will be displayed since InGameFeatureContainer thinks we
		// we are still is a game (in a SpinPanel)
		GameState.clearGameStack();
		
		Dialog.close();

		if (LobbyLoader.lastLobby != LobbyInfo.Type.SLOTVENTURE)
		{
			LobbyLoader.lastLobby = LobbyInfo.Type.SLOTVENTURE;
			Debug.LogWarning("Missions completed when not coming from the SlotVenture lobby.Forcing the player into the Slotventures lobby to show coins rollup");
		}

		NGUIExt.disableAllMouseInput();
		Loading.show(Loading.LoadingTransactionTarget.LOBBY);
		Glb.loadLobby();

		if (Audio.currentMusicPlayer != null && Audio.currentMusicPlayer.isPlaying && Audio.currentMusicPlayer.relativeVolume < 0.01f)
		{
			Audio.switchMusicKeyImmediate("");
		}

		Audio.stopAll();
		Audio.removeDelays();
		Audio.listenerVolume = Audio.maxGlobalVolume;
		Audio.play("return_to_lobby");
	}

	public void Update()
	{
		AndroidUtil.checkBackButton(closeClicked);
	}

	private void closeClicked(Dict args = null)
	{
		Dialog.close();
	}

	public static void showDialog()
	{
		Scheduler.addDialog("challenge_slotventures_dialog");
	}

	// Do NOT call -- called by Dialog.close()
	public override void close()
	{
		// Display Loot box reward dialog if we have one
		if (LootBoxFeature.instance != null)
		{
			LootBoxFeature.instance.showLootBoxRewardDialog(LootBoxFeature.SOURCE_SLOTVENTURES);
		}
	}
}
