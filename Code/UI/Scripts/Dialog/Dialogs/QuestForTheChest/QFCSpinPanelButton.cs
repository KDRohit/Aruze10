using System.Collections;
using Com.Scheduler;
using TMPro;
using UnityEngine;

/**
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
**/
namespace QuestForTheChest
{
	public class QFCSpinPanelButton : MonoBehaviour
	{
		[SerializeField] private ButtonHandler panelButton;
		[SerializeField] private TextMeshPro keyGoalLabel;
		[SerializeField] private QFCBoardTeamKeyMeter homeTeamMeter;
		[SerializeField] private QFCBoardTeamKeyMeter awayTeamMeter;
		[SerializeField] private GameObject keyPrefab;
		[SerializeField] private Transform keyParent;
		
		[SerializeField] private AnimationListController.AnimationInformationList keyDropAnimations;

		private QFCKeyOverlay overlayKey;
		

		private const string QFC_ICON_PRESS_SOUND = "QfcIconFlourish";
		private const string QFC_KEY_DROP_SOUND = "QfcKeysInGame";

		public void init()
		{
			panelButton.registerEventDelegate(onButtonClick);
			homeTeamMeter.init(QuestForTheChestFeature.instance.getTeamKeyTotal(QFCTeams.HOME), QuestForTheChestFeature.instance.requiredKeys);
			awayTeamMeter.init(QuestForTheChestFeature.instance.getTeamKeyTotal(QFCTeams.AWAY), QuestForTheChestFeature.instance.requiredKeys);
			keyGoalLabel.text = QuestForTheChestFeature.instance.requiredKeys.ToString(); 
			QuestForTheChestFeature.instance.onPlayerAwardTokenEvent += updateMeters;
			QuestForTheChestFeature.instance.onPlayerProgressToNonStoryNodeEvent += startKeyEarnedAnimation;
			QuestForTheChestFeature.instance.onNewRaceEvent += updateMeters;
			QuestForTheChestFeature.instance.onRestartEvent += updateMeters;
			QuestForTheChestFeature.instance.onDisabledEvent += eventEnded;
		}

		private void updateMeters(string zid, int amount)
		{
			homeTeamMeter.init(QuestForTheChestFeature.instance.getTeamKeyTotal(QFCTeams.HOME), QuestForTheChestFeature.instance.requiredKeys);
			awayTeamMeter.init(QuestForTheChestFeature.instance.getTeamKeyTotal(QFCTeams.AWAY), QuestForTheChestFeature.instance.requiredKeys);
			keyGoalLabel.text = QuestForTheChestFeature.instance.requiredKeys.ToString();
		}

		private void startKeyEarnedAnimation(string zid, int amount)
		{
			if (overlayKey == null)
			{
				GameObject obj = NGUITools.AddChild(keyParent, keyPrefab);
				overlayKey = obj.GetComponent<QFCKeyOverlay>();
			}
			StopAllCoroutines();
			StartCoroutine(playKeyDropAnimation(amount));
		}

		private void updateMeters()
		{
			homeTeamMeter.init(QuestForTheChestFeature.instance.getTeamKeyTotal(QFCTeams.HOME), QuestForTheChestFeature.instance.requiredKeys);
			awayTeamMeter.init(QuestForTheChestFeature.instance.getTeamKeyTotal(QFCTeams.AWAY), QuestForTheChestFeature.instance.requiredKeys);
			keyGoalLabel.text = QuestForTheChestFeature.instance.requiredKeys.ToString();
			
		}

		private void onButtonClick(Dict args = null)
		{
			if (!Scheduler.hasTaskWith("quest_for_the_chest_map"))
			{
				Audio.play(QFC_ICON_PRESS_SOUND + ExperimentWrapper.QuestForTheChest.theme);
				QFCMapDialog.showDialog();
			}
		}

		private void eventEnded()
		{
			gameObject.SetActive(false); //Disable when the event ends
		}

		private void OnDestroy()
		{
			QuestForTheChestFeature.instance.onRestartEvent -= updateMeters;
			QuestForTheChestFeature.instance.onPlayerAwardTokenEvent -= updateMeters;
			QuestForTheChestFeature.instance.onPlayerProgressToNonStoryNodeEvent -= startKeyEarnedAnimation;
			QuestForTheChestFeature.instance.onNewRaceEvent -= updateMeters;
			QuestForTheChestFeature.instance.onDisabledEvent -= eventEnded;			
		}

		private IEnumerator playKeyDropAnimation(int amount)
		{
			Audio.play(QFC_KEY_DROP_SOUND + ExperimentWrapper.QuestForTheChest.theme);
			QuestForTheChestFeature.instance.spinPanelMessageBox.onKeyAwarded(amount);
			overlayKey.playKeyFindSmall(amount);
			yield return new WaitForSeconds(3f);
			StartCoroutine(AnimationListController.playListOfAnimationInformation(keyDropAnimations));
		}
	}
}
