using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace QuestForTheChest
{
	public class QFCWinOverlay : MonoBehaviour
	{
		[SerializeField] private ButtonHandler button;
		[SerializeField] private MultiLabelWrapperComponent yourTeamWinsLabel;
		[SerializeField] private MultiLabelWrapperComponent yourAmountLabel;
		[SerializeField] private TextMeshPro yourShareLabel;
		[SerializeField] private TextMeshPro yourBaseAmountLabel;
		[SerializeField] private TextMeshPro levelLabel;
		[SerializeField] private TextMeshPro levelBonusLabel;
		[SerializeField] private GameObject yourShareRoot;
		[SerializeField] private GameObject yourShareInfoRoot;
		
		[SerializeField] private AnimationListController.AnimationInformationList introAnimations;
		[SerializeField] private AnimationListController.AnimationInformationList outroAnimations;
		[SerializeField] private Animator animator;
		[SerializeField] private Animator buttonAnimator;

		private ClickHandler.onClickDelegate onClickHandler;
		private string eventId;
		private CommonAnimatedChest animatedChest;
		private const string QFC_FINAL_KEY_COLLECT_SOUND = "QfcFinalKeyCollect";

		public void init(string id, long teamWin, long userWin, int xpLevel, int inflationFactor, long normalizedReward, CommonAnimatedChest chest, ClickHandler.onClickDelegate onClick)
		{
			onClickHandler = onClick;
			eventId = id;
			animatedChest = chest;

			yourTeamWinsLabel.text = Localize.text("qfc_your_team_wins_{0}", CreditsEconomy.convertCredits(teamWin));
			yourAmountLabel.text = CreditsEconomy.convertCredits(userWin);
			gameObject.SetActive(true);
			button.gameObject.SetActive(true);
			yourShareRoot.SetActive(true);
			if (normalizedReward > 0)
			{
				yourShareInfoRoot.SetActive(true);
				yourShareLabel.text = Localize.text("qfc_your_share");
				yourBaseAmountLabel.text = CreditsEconomy.convertCredits(normalizedReward);
				levelLabel.text = Localize.text("qfc_level_{0}", CommonText.formatNumber(xpLevel));
				levelBonusLabel.text = Localize.text("qfc_level_bonus_{0}", CommonText.formatNumber(inflationFactor));
			}
			else
			{
				yourShareInfoRoot.SetActive(false);
			}
			StartCoroutine(AnimationListController.playListOfAnimationInformation(introAnimations));
			button.enabled = true;
			button.registerEventDelegate(okClicked);

		}

		public void initLose(string id, CommonAnimatedChest chest, ClickHandler.onClickDelegate onClick)
		{
			onClickHandler = onClick;
			eventId = id;
			animatedChest = chest;

			yourTeamWinsLabel.text = string.Format("Bummer!  Red team wins the chest!"); //TODO: localize this
			yourAmountLabel.text = "";
			gameObject.SetActive(true);
			button.gameObject.SetActive(false);
			yourShareRoot.SetActive(false);

			StartCoroutine(losePresentation());
		}

		private void okClicked(Dict args)
		{
			Audio.play(QFC_FINAL_KEY_COLLECT_SOUND + ExperimentWrapper.QuestForTheChest.theme);
			float delayTime = 0.1f;
			if (animatedChest != null)
			{
				animatedChest.playOutro();
				delayTime = 4.25f;
			}

			button.enabled = false;
			StartCoroutine(AnimationListController.playListOfAnimationInformation(outroAnimations));
			button.unregisterEventDelegate(okClicked);
			
			StartCoroutine(delayFinish(delayTime));
		}

		private IEnumerator delayFinish(float fTime)
		{
			//wait one frame for events to finish
			yield return null;

			//consume race complete if necessary
			int raceIndex = QuestForTheChestFeature.instance.raceIndex;
			bool consume = checkCompletedRace();

			//delay input time
			yield return new WaitForSeconds(fTime);

			//if we're consuming the race verify that it's finished
			if (consume)
			{
				yield return StartCoroutine(QFCMapDialog.verifyRaceConsumed(raceIndex));
			}

			if (onClickHandler != null)
			{
				onClickHandler.Invoke(Dict.create(D.EVENT_ID, eventId));
			}

			if (gameObject != null)
			{
				gameObject.SetActive(false);
			}
		}

		private bool checkCompletedRace()
		{
			//if the user has no keys, we need to consume the race here because they won't get a key award overlay
			string raceId = QuestForTheChestFeature.instance.getCompletedRaceId();
			int keyTotal = QuestForTheChestFeature.instance.getCurrentUserKeyTotal();
			if (!string.IsNullOrEmpty(raceId) && keyTotal <= 0)
			{
				QuestForTheChestFeature.instance.consumeRaceComplete(raceId);
				return true;
			}

			return false;
		}

		private IEnumerator losePresentation()
		{
			//wait one frame to ensure all events come in
			yield return null;

			//consume race complete if necessary
			int raceIndex = QuestForTheChestFeature.instance.raceIndex;
			bool consume = checkCompletedRace();

			//display text for 2 seconds
			yield return new WaitForSeconds(2.0f);

			//animate the chest off
			animatedChest.playSlideOff();

			//wait a minimum 1.5 seconds
			yield return new WaitForSeconds(1.5f);

			//if we're consuming the race verify that it's finished
			if (consume)
			{
				yield return StartCoroutine(QFCMapDialog.verifyRaceConsumed(raceIndex));
			}

			//run callback
			if (onClickHandler != null)
			{
				onClickHandler.Invoke(Dict.create(D.EVENT_ID, eventId));
			}

			//disable object
			if (gameObject != null)
			{
				gameObject.SetActive(false);
			}
		}
	}
}
