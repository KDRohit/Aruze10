using UnityEngine;

/*
 * A version of BonusGamePresenter which lies within a dialog.
*/
public class DialogBonusGamePresenter : BonusGamePresenter
{
	[Tooltip("Background music for this bonus game. Plays on start and plays previous music on close")]
	[SerializeField] private string backgroundMusicKey;
	
	private string previousMusicKey = "";
	
	// use this to pass themed music key
	[System.NonSerialized] public string overrideMusicKey = "";
	
	public override void init(bool isCheckingReelGameCarryOverValue)
	{
		// if backgroundMusic is specified, make sure base does not play any music
		isAutoPlayingInitMusic = false;
		base.init(isCheckingReelGameCarryOverValue);
		if (!string.IsNullOrEmpty(overrideMusicKey))
		{
			backgroundMusicKey = overrideMusicKey;
		}

		// Play music only if defined. Otherwise don't do anything
		if (!string.IsNullOrEmpty(backgroundMusicKey))
		{
			previousMusicKey = Audio.defaultMusicKey;
			Audio.switchMusicKeyImmediate(backgroundMusicKey);
		}
	}

	/// <summary>
	/// Grants any pending credits after the game has ended.
	/// </summary>
	public override void finalCleanup()
	{
		base.finalCleanup();
		if (BonusGameManager.instance.finalPayout > 0)
		{
			SlotsPlayer.addCredits(BonusGameManager.instance.finalPayout, Dialog.instance.currentDialog.economyTrackingName,
				false); // since we know it is a dialog
			BonusGameManager.instance.finalPayout = 0;
		}

		if (!string.IsNullOrEmpty(previousMusicKey))
		{
			// play the previous music if this presenter had changed it
			Audio.switchMusicKeyImmediate(previousMusicKey);
		}
	}

	public void setBonusSummaryAsSeen()
	{
		//Mark the bonus as seen since a summary screen won't be appearing
		if (forceEarlyEnd && HasBonusGameIdentifier() && !BonusGameManager.instance.hasStackedBonusGames())
		{
			SlotAction.seenBonusSummaryScreen(NextBonusGameIdentifier());
		}
	}

	public override bool gameEnded()
	{
		setBonusSummaryAsSeen();
		return base.gameEnded();
	}
}