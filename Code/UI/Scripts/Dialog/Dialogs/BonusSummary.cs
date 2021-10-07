using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

/*
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
*/

public abstract class BonusSummary : DialogBase
{
	public GameObject baseWinLabel;
	public TextMeshPro baseWinAmount;	// Will be same as finalWinAmount if no multiplier is used.
	public TextMeshPro finalWinAmount;
	public GameObject multiplierLabel;
	public TextMeshPro multiplierAmount;
	public ButtonHandler collectButtonHandler;
	public GameObject closeButton;
	public GameObject bonusWinLabel;
	public UITexture summaryIcon;

	/// Initialization
	public override void init()
	{
		collectButtonHandler.gameObject.SetActive(false);
		closeButton.gameObject.SetActive(false);
		summaryIcon.gameObject.SetActive(false);

		collectButtonHandler.registerEventDelegate(collectClicked);

		//send seen action if necessary
		processBonusSummary();
		
		SlotResourceMap.getSummaryIcon(BonusGameManager.instance.currentGameKey, BonusGameManager.instance.currentGameType, loadSummaryIconCallback, null);
	}
	
	protected void loadSummaryIconCallback(string asset, Object obj, Dict data)
	{
		Texture iconTex = obj as Texture;
		if (iconTex != null && this != null && summaryIcon != null)
		{
			summaryIcon.gameObject.SetActive(true);
			NGUIExt.applyUITexture(summaryIcon, iconTex);
		}
	}
		
	/// Returns whether the summary should show the multiplier info.
	protected bool isUsingMultiplier
	{
		get
		{
			return (BonusGamePresenter.instance != null && (BonusGamePresenter.instance.useMultiplier && BonusGameManager.instance.currentMultiplier > 1));
		}
	}
	
	void Update()
	{
		if (shouldAutoClose)
		{
			closeClicked();
		}
		AndroidUtil.checkBackButton(closeClicked);
	}
	
	protected abstract IEnumerator showResults();

	protected virtual IEnumerator showResultsBase(GameObject[] objectsToHideWithoutMultiplier)
	{
		// Request from Chris to always play the summary fanfare when the summary screen appears, rather than allowing it to play later which we used to do
		Audio.switchMusicKeyImmediate("");
		playCorrectSound();

		baseWinAmount.text = CreditsEconomy.convertCredits(BonusGamePresenter.instance.currentPayout);
		finalWinAmount.text = CreditsEconomy.convertCredits(BonusGameManager.instance.currentGameFinalPayout);
		if (GameState.giftedBonus != null)
		{
			multiplierAmount.text = Localize.text("{0}X", CommonText.formatNumber(GiftedSpinsVipMultiplier.playerMultiplier));
		}
		else
		{
			multiplierAmount.text = Localize.text("{0}X", CommonText.formatNumber(BonusGameManager.instance.currentMultiplier));
		}

	
		baseWinAmount.gameObject.SetActive(false);
		finalWinAmount.gameObject.SetActive(false);
		multiplierAmount.gameObject.SetActive(false);

		string summaryPlaylistKey = Audio.soundMap("bonus_summary_reveal_value");
		PlaylistInfo summaryPlaylist = PlaylistInfo.find(summaryPlaylistKey);

		if (isUsingMultiplier)
		{
			yield return new WaitForSeconds(1);
			baseWinAmount.gameObject.SetActive(true);
			Audio.play(summaryPlaylistKey);
		
			yield return new WaitForSeconds(1);
			multiplierAmount.gameObject.SetActive(true);
			Audio.play(summaryPlaylistKey);
		}
		else
		{
			if (summaryPlaylist != null)
			{
				summaryPlaylist.skipTrack();
				summaryPlaylist.skipTrack();
			}
			
			foreach (GameObject go in objectsToHideWithoutMultiplier)
			{
				go.SetActive(false);
			}
		}
		
		yield return new WaitForSeconds(1);
		finalWinAmount.gameObject.SetActive(true);
		Audio.play(summaryPlaylistKey);
		
		collectButtonHandler.gameObject.SetActive(true);
		closeButton.gameObject.SetActive(true);	
	}
	
	/// NGUI button callback.
	private void closeClicked()
	{
		if (Dialog.instance.isClosing)
		{
			return;
		}

		cancelAutoClose();
		Dialog.close();
	}

	/// NGUI button callback.
	protected void collectClicked(Dict args = null)
	{
		if (Dialog.instance.isClosing)
		{
			return;
		}

		cancelAutoClose();
		Dialog.close();
	}

	// This function is used to play the appropriate voiceover if possible
	protected void playSummaryVO(string vo_key, float delayVO = 0.0f)
	{
		if (Audio.canSoundBeMapped(vo_key))
		{
			// PLay the voiceover if it's defined
			string VO = Audio.soundMap(vo_key);
			if (!string.IsNullOrEmpty(VO) && !Audio.isPlaying(VO) &&
				!Audio.isSoundInRelativeDelayList(VO))
			{
				Audio.play(VO, 1.0f , 0.0f , delayVO);
			}
		}
	}

	protected void playCorrectSound()
	{
		if (BonusGamePresenter.instance.isProgressive)
		{
			// Play the progressive summary fanfare.
			Audio.play(Audio.soundMap("progressive_summary_fanfare"));
		}
		else if (FreeSpinGame.instance != null || (SlotBaseGame.instance != null && SlotBaseGame.instance.isDoingFreespinsInBasegame()))
		{
			// Play the freespin summary fanfare, we are playing freespins or freespins in base
			Audio.playSoundMapOrSoundKey(BonusGamePresenter.instance.FREESPIN_SUMMARY_FANFARE);
			if (ReelGame.activeGame != null)
			{
				playSummaryVO("freespin_summary_vo", ReelGame.activeGame.delayBonusSummaryVo);
			}
			else
			{
				// ReelGame.activeGame returns null for gifted freespins once they're completed (HIR-52603)
				playSummaryVO("freespin_summary_vo", FreeSpinGame.instance.delayBonusSummaryVo);
			}

		}
		else if (ChallengeGame.instance != null)
		{
			// Should be a challenge game so use the fanfareType
			switch (ChallengeGame.instance.fanfareType)
			{
				case ChallengeGame.FanfareEnum.BonusSummary:
					// Play the bonus summary fanfare.
					Audio.play(Audio.soundMap("bonus_summary_fanfare"));
					playSummaryVO("bonus_summary_vo", ChallengeGame.instance.delayBonusSummaryVo);
					break;

				case ChallengeGame.FanfareEnum.WheelSummary:
					// Play the wheel summary fanfare.
					Audio.play(Audio.soundMap("wheel_summary_fanfare"));
					playSummaryVO("bonus_summary_vo", ChallengeGame.instance.delayBonusSummaryVo);
					break;

				case ChallengeGame.FanfareEnum.FreeSpinSummary:
					// NOTE : Probably shouldn't be using this, but just in case there is ever a reason you need this override
					Audio.playSoundMapOrSoundKey(BonusGamePresenter.instance.FREESPIN_SUMMARY_FANFARE);
					break;
			}
		}
	}
	
	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup.
	}

	public static void handlePresentation(Dict args)
	{
		if (BonusGamePresenter.instance.currentPayout > 0 || BonusGameManager.instance.finalPayout > 0)
		{
			showDialog(args);
		}
		else
		{
			processBonusSummary();
			if (args != null)
			{
				AnswerDelegate callback = args.getWithDefault(D.CALLBACK, null) as AnswerDelegate;
				if (callback != null)
				{
					callback.Invoke(null);
				}
			}
		}
	}
	
	private static void showDialog(Dict args)
	{
		Scheduler.addDialog(
			"bonus_summary", 
			args,
			SchedulerPriority.PriorityType.IMMEDIATE
		);
	}

	public static void processBonusSummary()
	{
		// Only send this if we have an id and this isn't a stacked bonus
		// since stacked bonuses so far will only send a single seenBonusSummaryScreen action
		// once the root bonus is over
		if (BonusGamePresenter.HasBonusGameIdentifier() && !BonusGameManager.instance.hasStackedBonusGames())
		{
			SlotAction.seenBonusSummaryScreen(BonusGamePresenter.NextBonusGameIdentifier());
		}

	}
}
