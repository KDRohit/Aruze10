using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
**/
namespace QuestForTheChest
{
	public class QFCMiniGameOverlay : QFCMapDialogOverlay, IResetGame
	{
		[SerializeField] private BonusGamePresenter challengePresenter;
		[SerializeField] private ModularChallengeGame challengeGame;
		[SerializeField] private ButtonHandler collectButton;
		private ModularChallengeGameOutcome challengeGameOutcome;
		private DialogBase.AnswerDelegate clickCallback;
		private DialogBase.AnswerDelegate keyOutroCompleteCallback;

		private QFCContainerItem coinsReward = null;
		private QFCRewardItemKeys keysRewardItem = null;
		
		private int keysWon = 0;
		private long bonusGameTotal = 0L;
		
		//Note: The wheel intro sound - QfcWheelIntroCandy is setup in the prefab in the "Challenge Game Animate On Start Round" Module component 
		private string wheelMusic;
		private string wheelAnimateSound; //TODO: when to play this?
		private string wheelAwardFanfareSound;
		private string wheelAwardCollectSound;

		private const float KEY_OUTRO_ANIM_LENGTH = 4.0f;

		private Dictionary<long, long> winIdToAbsouluteCreditValues = new Dictionary<long, long>();

		public static QFCMiniGameOverlay instance { get; private set; }
		public void init(int nodeIndex, SlotOutcome bonusGameOutcome, JSON absoluteCoinRewards, int keysReward, DialogBase.AnswerDelegate collectCallback, DialogBase.AnswerDelegate outroCallback)
		{
			instance = this;
			initSounds();
			Audio.switchMusicKeyImmediate(wheelMusic);
			setUpDynamicNodeAssets(nodeIndex);
			keysWon = keysReward;
			
			challengeGameOutcome = new ModularChallengeGameOutcome(bonusGameOutcome);
			winIdToAbsouluteCreditValues = QuestForTheChestFeature.convertAbsoluteCreditValues(absoluteCoinRewards);

			initRewards();
			StartCoroutine(startBonusGame());
			collectButton.registerEventDelegate(onCollectClicked);
			clickCallback = collectCallback;
			keyOutroCompleteCallback = outroCallback;
			StatsManager.Instance.LogCount("dialog", "ptr_v2", "dialog_final_node", "", "", "view");
		}

		private void initSounds()
		{
			string suffix = ExperimentWrapper.QuestForTheChest.theme;
			wheelMusic = "QfcWheelMusic" + suffix;
			wheelAnimateSound = "QfcWheelAnimate" + suffix; //TODO: when to play this
			wheelAwardFanfareSound = "QfcWheelAwardFanfare" + suffix;
			wheelAwardCollectSound = "QfcCheckpointCollect" + suffix;
		}

		private void initKeyObjects()
		{
			QFCKeyObject[] keyItems = gameObject.GetComponentsInChildren<QFCKeyObject>();
			if (keyItems != null)
			{
				for (int i = 0; i < keyItems.Length; i++)
				{
					if (keyItems[i] == null)
					{
						continue;
					}
					keyItems[i].setupKeyObjects();
				}
			}
		}

		private void initRewards()
		{
			if (keysWon > 0)
			{
				keysRewardItem = attachKeysReward(keysWon);
			}
			else
			{
				bonusGameTotal = getAbsoulteCreditsValue(challengeGameOutcome.getRound(challengeGameOutcome.outcomeIndex).entries[0].winID);
				coinsReward = attachCoinReward(0);
			}
		}

		private IEnumerator startBonusGame()
		{
			challengePresenter.gameObject.SetActive(true);
			BonusGamePresenter.instance = challengePresenter;
			challengePresenter.isReturningToBaseGameWhenDone = false;
			challengePresenter.init(isCheckingReelGameCarryOverValue:false);

			List<ModularChallengeGameOutcome> variantOutcomeList = new List<ModularChallengeGameOutcome>();

			// since each variant will use the same outcome we need to add as many outcomes as there are variants setup
			for (int m = 0; m < challengeGame.pickingRounds[0].roundVariants.Length; m++)
			{
				variantOutcomeList.Add(challengeGameOutcome);
			}

			challengeGame.addVariantOutcomeOverrideListForRound(0, variantOutcomeList);
			challengeGame.init();

			initKeyObjects();
			
			while (challengePresenter.isGameActive)
			{
				yield return null;
			}

			Audio.play(wheelAwardFanfareSound);
			if (coinsReward != null)
			{
				coinsReward.init(CreditsEconomy.convertCredits(BonusGameManager.instance.finalPayout));
			}

			collectButton.gameObject.SetActive(true);
			rewardParent.gameObject.SetActive(true);
			
			if (keysRewardItem != null)
			{
				keysRewardItem.init(keysWon);
			}
		}

		private void onCollectClicked(Dict data = null)
		{
			Audio.play(wheelAwardCollectSound);
			if (BonusGamePresenter.HasBonusGameIdentifier() && !BonusGameManager.instance.hasStackedBonusGames())
			{
				SlotAction.seenBonusSummaryScreen(BonusGamePresenter.NextBonusGameIdentifier());
			}

			if (bonusGameTotal > 0)
			{
				SlotsPlayer.addFeatureCredits(bonusGameTotal, QuestForTheChestFeature.QFC_REWARD_PENDING_CREDIT_BONUS_SOURCE);	
			}
			
			BonusGameManager.instance.finalPayout = 0;
			collectButton.enabled = false;

			StatsManager.Instance.LogCount("dialog", "ptr_v2", "dialog_final_node", "", "collect", "click");

			if (clickCallback != null)
			{
				clickCallback(Dict.create(D.OPTION, false));
			}

			if (keysRewardItem != null)
			{
				StartCoroutine(playKeysOutro());
			}
			else
			{
				//destroy this game object
				if (keyOutroCompleteCallback != null)
				{
					keyOutroCompleteCallback(Dict.create(D.OPTION, false, D.NEW_LEVEL, true));
				}

				Destroy(this.gameObject);
			}

			StatsManager.Instance.LogCount("dialog", "ptr_v2", "dialog_final_node", "", "close", "click");
		}

		private IEnumerator playKeysOutro()
		{
			//Need to hide the wheel game while keeping the key visible
			keysRewardItem.keyOverlay.transform.SetParent(transform.parent);
			keysRewardItem.keyOverlay.transform.localScale = Vector3.one;
			iTween.ScaleTo(gameObject, iTween.Hash("x", 0, "y", 0, "time", 0.25f, "easetype", iTween.EaseType.linear));
			CommonTransform.setDepth(keysRewardItem.keyOverlay.transform, 1.0f);
			
			//Move the key to the center so the animation can line up with the meters
			iTween.MoveTo(keysRewardItem.keyOverlay.gameObject, iTween.Hash("position", Vector3.zero, "islocal", true, "time", 0.5f, "easetype", iTween.EaseType.linear));
			
			//Play outro animation
			keysRewardItem.keyOverlay.playWheelCollectAnimation();
			yield return new WaitForSeconds(KEY_OUTRO_ANIM_LENGTH);
			
			if (keyOutroCompleteCallback != null)
			{
				keyOutroCompleteCallback(Dict.create(D.OPTION, false, D.NEW_LEVEL, true));
			}
			
			//Turn off no longer needed object
			keysRewardItem.gameObject.SetActive(false);
			gameObject.SetActive(false);
		}

		public long getAbsoulteCreditsValue(long winId)
		{
			long result = 0;
			if (winIdToAbsouluteCreditValues.TryGetValue(winId, out result))
			{
				return result;
			}

			return 0;
		}

		private void OnDestroy()
		{
			instance = null;
		}

		public static void resetStaticClassData()
		{
			instance = null;
		}
	}
}
