using UnityEngine;
using System.Collections;
using TMPro;

// Class to handle populating the Game Unlock Reward Dialog called from CollectReward.cs
public class GameUnlockRewardDialog : DialogBase
{
	public MeshRenderer gameRenderer;
	public TextMeshPro rewardLabel;
	
	private bool shouldClose = false;
	private string gameKey = "";
	
	public override void init()
	{
		LobbyOption lobbyOption = (LobbyOption)dialogArgs.getWithDefault(D.BONUS_GAME, null);
		if (lobbyOption == null)
		{
			Debug.LogErrorFormat("GameUnlockRewardDialog.cs -- init -- lobbyOption was null, bailing.");
			shouldClose = true;
		}
		else if (lobbyOption.game != null)
		{
			gameKey = lobbyOption.game.keyName;
		}
		rewardLabel.text = string.Format("You have unlocked \n{0}",lobbyOption.game.name);
	    downloadedTextureToRenderer(gameRenderer, index:0);
	}

	public override void close()
	{
		
	}

	protected override void onFadeInComplete()
	{
		base.onFadeInComplete();
		if (shouldClose)
		{
			// Closing the dialog early, an error should have been logged already.
			Dialog.close();
		}
	}

	private void collectClicked()
	{
		SlotsPlayer.unlockGame("", gameKey);
		Dialog.close();
	}
	
	public static void showDialog(LobbyOption option)
	{
		string gameImagePath = SlotResourceMap.getLobbyImagePath(option.game.groupInfo.keyName, option.game.keyName, "1X2");		
		Dialog.instance.showDialogAfterDownloadingTextures("reward_game_unlock_dialog", nonMappedBundledTextures:new string[]{gameImagePath}, args:Dict.create(D.BONUS_GAME, option));
	}
}
