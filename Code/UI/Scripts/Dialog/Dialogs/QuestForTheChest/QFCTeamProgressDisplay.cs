using System;
using System.Collections;
using System.Collections.Generic;
using QuestForTheChest;
using TMPro;
using UnityEngine;

namespace QuestForTheChest
{
	public class QFCTeamProgressDisplay : MonoBehaviour
	{
		private const string IDLE_ANIM = "Idle";
		private const string INTRO_ANIM = "Intro";
		private const string CHEST_OUTRO_ANIM = "Meter Unlock Outro";
		private const string OUTRO_ANIM = "Outro";
		private const string RED_FILLED_ANIM = "Red Filled";
		private const string RED_INCREASE_ANIM = "Red Increase";
		private const string BLUE_FILLED_ANIM = "Blue Filled";
		private const string BLUE_INCREASE_ANIM = "Blue Increase";

		private enum DisplayState
		{
			OFF,
			ON
		}
		[SerializeField] private QFCBoardTeamKeyMeter homeTeamMeter;
		[SerializeField] private QFCBoardTeamKeyMeter awayTeamMeter;
		[SerializeField] private TextMeshPro chestLockRequirementLabel;
		[SerializeField] private TextMeshPro messageLabel;
		[SerializeField] private TextMeshPro messageLabelShadow;
		[SerializeField] private Animator animator;


		private DisplayState currentState = DisplayState.OFF;


		public void init()
		{
			currentState = DisplayState.OFF;
			chestLockRequirementLabel.text = CommonText.formatNumber(QuestForTheChestFeature.instance.requiredKeys);
			clearMessage();
			showTeamMeters(false);
		}

		public void reset(bool shouldShowTeamMeters)
		{
			chestLockRequirementLabel.text =CommonText.formatNumber(QuestForTheChestFeature.instance.requiredKeys);
			clearMessage();
			if (shouldShowTeamMeters)
			{
				showTeamMeters(false);
			}

		}

		public void showTeamMeters(bool tween = false)
		{
			homeTeamMeter.init(QuestForTheChestFeature.instance.getTeamKeyTotal(QFCTeams.HOME), QuestForTheChestFeature.instance.requiredKeys, tween);
			awayTeamMeter.init(QuestForTheChestFeature.instance.getTeamKeyTotal(QFCTeams.AWAY), QuestForTheChestFeature.instance.requiredKeys, tween);

			StartCoroutine(playIdle(currentState));
			currentState = DisplayState.ON;
		}

		public IEnumerator playIdle()
		{
			yield return RoutineRunner.instance.StartCoroutine(playIdle(currentState));
		}

		private IEnumerator playIdle(DisplayState state)
		{
			if (state == DisplayState.OFF)
			{
				currentState = DisplayState.ON;
				yield return RoutineRunner.instance.StartCoroutine(CommonAnimation.playAnimAndWait(animator, INTRO_ANIM));
			}

			animator.Play(IDLE_ANIM);
		}

		public IEnumerator playOutro()
		{
			if (currentState == DisplayState.ON)
			{
				currentState = DisplayState.OFF;
				yield return RoutineRunner.instance.StartCoroutine(CommonAnimation.playAnimAndWait(animator, OUTRO_ANIM));
			}
			yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
		}

		public void showMessage(string msg)
		{
			messageLabel.text = msg;
			messageLabelShadow.text = msg;
		}

		public void clearMessage()
		{
			messageLabel.text = "";
			messageLabelShadow.text = "";
		}

		private IEnumerator playAndGoBackToIdle(string animation, DisplayState intermediateState, float waitTime)
		{
			animator.Play(animation);
			yield return new WaitForSeconds(waitTime);
			yield return StartCoroutine(playIdle(intermediateState));
		}

		public void updateKeyTotals()
		{
			long newHomeTotal = QuestForTheChestFeature.instance.getTeamKeyTotal(QFCTeams.HOME);
			long oldHomeTotal = homeTeamMeter.Value;
			if (oldHomeTotal != newHomeTotal)
			{
				homeTeamMeter.init((int)newHomeTotal, QuestForTheChestFeature.instance.requiredKeys, true);
				StartCoroutine(playAndGoBackToIdle(newHomeTotal >= QuestForTheChestFeature.instance.requiredKeys ? BLUE_FILLED_ANIM : BLUE_INCREASE_ANIM, DisplayState.ON, 1.0f));
			}

			long newAwayTotal = QuestForTheChestFeature.instance.getTeamKeyTotal(QFCTeams.AWAY);
			long oldAwayTotal = awayTeamMeter.Value;
			if (newAwayTotal != oldAwayTotal)
			{
				awayTeamMeter.init((int)newAwayTotal, QuestForTheChestFeature.instance.requiredKeys, true);
				StartCoroutine(playAndGoBackToIdle(newAwayTotal >= QuestForTheChestFeature.instance.requiredKeys ? RED_FILLED_ANIM : RED_INCREASE_ANIM, DisplayState.ON, 1.0f));
			}

			currentState = DisplayState.ON;
		}

		public IEnumerator dropKeysToZero(float fTime)
		{
			float homeDuration = homeTeamMeter.drainMeter(QuestForTheChestFeature.instance.requiredKeys, fTime);
			float awayDuration = awayTeamMeter.drainMeter(QuestForTheChestFeature.instance.requiredKeys, fTime);

			float maxDuration = Mathf.Max(homeDuration, awayDuration);

			yield return new WaitForSeconds(maxDuration);

			//hide overlay
			RoutineRunner.instance.StartCoroutine(playOutro());
		}

		public void Unlock()
		{
			if (currentState == DisplayState.ON)
			{
				animator.Play(CHEST_OUTRO_ANIM);
			}

			currentState = DisplayState.OFF;
		}
	}
}
