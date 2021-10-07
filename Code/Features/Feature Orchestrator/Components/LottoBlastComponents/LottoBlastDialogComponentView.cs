using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zynga.Core.Util;

namespace FeatureOrchestrator
{
	public class LottoBlastDialogComponentView : GenericDialogComponentView
	{
		[SerializeField] private LabelWrapperComponent durationLabel;
		[SerializeField] private LabelWrapperComponent jackpotLabel;
		[SerializeField] private LabelWrapperComponent buffDurationLabel;
		[SerializeField] private LottoBlastProgressPath progressPath;
		[SerializeField] private LabelWrapperComponent descriptionLabel;
		[SerializeField] private LabelWrapperComponent purchaseOverlayTitleLabel;
		[SerializeField] private ButtonHandler purchaseButtonHandler;
		[SerializeField] private ButtonHandler purchaseOverlayCloseButtonHandler;
		
		[SerializeField] private AnimationListController.AnimationInformationList buffActiveAnim;
		[SerializeField] private AnimationListController.AnimationInformationList buffInactiveAnim;
		
		[SerializeField] private AnimationListController.AnimationInformationList showOfferAnimList;
		
		private int startLevel = 0;
		private int targetLevel = 0;
		private bool showPurchaseOfferBeforeClosing = false;
		private bool purchaseOfferOverlayActive = false;
		
		public override void init()
		{
			base.init();
			
			GameTimerRange timerRange = dialogArgs.getWithDefault(D.TIME, null) as GameTimerRange;
			timerRange.registerLabel(durationLabel.tmProLabel, GameTimerRange.TimeFormat.REMAINING_HMS_FORMAT, true);
			timerRange.registerFunction(onTimerExpired);
			jackpotLabel.text = dialogArgs.getWithDefault(D.AMOUNT, "") as string;
			int tripleXPDuration = ExperimentWrapper.LevelLotto.tripleXPDuration;
			string durationText = "";
			System.TimeSpan timeSpan = System.TimeSpan.FromSeconds(tripleXPDuration);
			if (timeSpan.Days > 0)
			{
				durationText = string.Format("{0}{1}", timeSpan.Days, Localize.text("days_abbreviation"));
			}
			else if (timeSpan.Hours > 0)
			{
				durationText = string.Format("{0}{1}", timeSpan.Hours, Localize.text("hours_abbreviation"));
			}
			else
			{
				durationText = string.Format("{0}{1}", timeSpan.Minutes, Localize.text("minutes_abbreviation"));
			}
			buffDurationLabel.text = durationText;

			PowerupBase buff = PowerupsManager.getActivePowerup(PowerupBase.LEVEL_LOTTO_TRIPLE_XP_KEY);

			if (buff == null)
			{
				showPurchaseOfferBeforeClosing = true;
				StartCoroutine(playBuffAnimation(false));
			}
			else
			{
				buff.runningTimer.registerFunction(onBuffExpired);
				StartCoroutine(playBuffAnimation(true));
			}

			XPProgressCounter progressData = dialogArgs.getWithDefault(D.VALUE, null) as XPProgressCounter;
			if (progressData != null)
			{
				startLevel = progressData.levelData.Keys.First();
				targetLevel = progressData.completeLevel;
				if (descriptionLabel != null)
				{
					descriptionLabel.text = Localize.text(dialogArgs.getWithDefault(D.OPTION, "") as string,
						progressData.completeLevel);
				}

				if (purchaseOverlayTitleLabel != null)
				{
					purchaseOverlayTitleLabel.text = Localize.text(dialogArgs.getWithDefault(D.OPTION1, "") as string,
						progressData.completeLevel);
				}

				//Consider the final node as well
				//If the total nodes in levelData are more than what we can display (currently 5 + target level)
				//then we need to get a subset of the levels that can be displayed on the progress bar.
				if (progressData.levelData.Count > progressPath.maxNodes + 1)
				{
					int index = -1;
					long currentXP = SlotsPlayer.instance.xp.amount;
					
					//progressData.levelData is a sorted dictionary
					//check which index we are on for the current XP
					foreach (KeyValuePair<int,long> levelData in progressData.levelData)
					{
						if (currentXP >= levelData.Value)
						{
							index++;
						}
					}

					//currently the maxnodes is set to 5, so we need a subset of 5 level values
					//with the current level value in the center if possible
					//calculate the endIndex first and count back 5 times to get the startIndex
					//make sure the endIndex and startIndex are within range
					//eg. if the levels are 1 to 10, and current level is 5 the progress bar shows 3-4-5-6-7-10
					//if current level is 8 the progress bar shows 5-6-7-8-9-10
					int endIndex = index + progressPath.maxNodes/2 + 1;
					endIndex = Mathf.Min(endIndex, progressData.levelData.Keys.Count - 1);
					int startIndex = Mathf.Max(endIndex - progressPath.maxNodes, 0);

					List<int> finalLevels = progressData.levelData.Keys.GetRange(startIndex, progressPath.maxNodes) as List<int>;
					//We always show the final target level. so just add it at the end
					finalLevels.Add(targetLevel);
					List<long> finalXPs = progressData.levelData.Values.GetRange(startIndex, progressPath.maxNodes) as List<long>;
					finalXPs.Add(progressData.levelData.Values.Last());
					progressPath.setup(finalLevels, finalXPs, buff != null);
				}
				else
				{
					List<int> finalLevels = progressData.levelData.Keys.ToList();
					progressPath.setup(finalLevels, progressData.levelData.Values.ToList(), buff != null);
				}
			}

			if(SlotBaseGame.instance != null && SlotBaseGame.instance.isRoyalRushGame && SlotBaseGame.instance.tokenBar != null)
			{
				RoyalRushCollectionModule rrMeter = SlotBaseGame.instance.tokenBar as RoyalRushCollectionModule;
				if (rrMeter != null && rrMeter.currentRushInfo.currentState == RoyalRushInfo.STATE.PAUSED)
				{
					rrMeter.pauseTimers();
				}
			}
			if (purchaseButtonHandler != null)
			{
				purchaseButtonHandler.registerEventDelegate(onBuyButtonClick);
			}
			if (purchaseOverlayCloseButtonHandler != null)
			{
				purchaseOverlayCloseButtonHandler.registerEventDelegate(onCloseButtonClicked);
			}
			StatsLottoBlast.logFeatureDialogAction(startLevel, targetLevel, "view");
		}

		public override void onCloseButtonClicked(Dict args = null)
		{
			if (showPurchaseOfferBeforeClosing)
			{
				showPurchaseOfferBeforeClosing = false;
				switch (ExperimentWrapper.LevelLotto.dialogCloseAction)
				{
					case LottoBlastExperiment.DialogCloseAction.BUY_PAGE:
						onBuyButtonClick();
						break;
					case LottoBlastExperiment.DialogCloseAction.EXTRA_DIALOG:
						StartCoroutine(showPurchaseOfferOverlayAnimations());
						break;
					default:
						closeDialogAndTrack();
						break;
				}
			}
			else
			{
				if (purchaseOfferOverlayActive)
				{
					StatsLottoBlast.logFeatureDialogPurchaseOverlayAction("close");
				}

				closeDialogAndTrack();
			}
		}

		private void closeDialogAndTrack()
		{
			StatsLottoBlast.logFeatureDialogAction(startLevel, targetLevel, "close");
			Dialog.close(this);
		}

		private void onBuyButtonClick(Dict args = null)
		{
			if (purchaseOfferOverlayActive)
			{
				StatsLottoBlast.logFeatureDialogPurchaseOverlayAction("click");
			}
			BuyCreditsDialog.showDialog();
			closeDialogAndTrack();
		}

		private void onTimerExpired(Dict args = null, GameTimerRange sender = null)
		{
			closeDialogAndTrack();
		}

		public IEnumerator showPurchaseOfferOverlayAnimations()
		{
			purchaseOfferOverlayActive = true;
			StatsLottoBlast.logFeatureDialogPurchaseOverlayAction("view");
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(showOfferAnimList));
		}

		private void onBuffExpired(Dict args = null, GameTimerRange sender = null)
		{
			StartCoroutine(playBuffAnimation(false));
		}

		IEnumerator playBuffAnimation(bool isBuffActive)
		{
			if (isBuffActive)
			{
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(buffInactiveAnim));
			}
		}
	}
}
