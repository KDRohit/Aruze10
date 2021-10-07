using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/**
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
**/
namespace QuestForTheChest
{
	public class QFCSpinPanelMessageBox : MonoBehaviour
	{
		private const string QFC_BET_HIGH_KEY = "qfc_bet_high";
		private const string QFC_FOUND_KEY = "qfc_reward_key";
		private const string QFC_FOUND_KEYS = "qfc_reward_keys";

		[SerializeField] private Animator panelAnimator;
		[SerializeField] private TextMeshPro messageLabel;
		[SerializeField] private GameObject keySprite;
		
		public void init()
		{
			QuestForTheChestFeature.instance.onDisabledEvent += eventEnded;
		}

		private void eventEnded()
		{
			gameObject.SetActive(false);
		}

		private void OnDestroy()
		{
			QuestForTheChestFeature.instance.onDisabledEvent -= eventEnded;
		}
		
		public void onKeyAwarded(int numKeys)
		{
			keySprite.SetActive(false);
			messageLabel.text = (numKeys == 1) ? Localize.text(QFC_FOUND_KEY, numKeys) : Localize.text(QFC_FOUND_KEYS, numKeys);
			animateMessageBox();
		}

		public void onWagerChange()
		{
			keySprite.SetActive(true);
			messageLabel.text = Localize.text(QFC_BET_HIGH_KEY);
			animateMessageBox();
		}

		private void animateMessageBox()
		{
			if (panelAnimator.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
			{
				panelAnimator.Play("Change Bet Intro");
			}
			else if 
			(
				panelAnimator.GetCurrentAnimatorStateInfo(0).IsName("Change Bet Outro") ||
				panelAnimator.GetCurrentAnimatorStateInfo(0).IsName("Change Bet Outro 0")
			)
			{
				panelAnimator.Play("Change Bet Outro 0");  // same as resetting change bet outro
			}
		}

		public void onSpinClicked()
		{
			if 
			(
				panelAnimator.GetCurrentAnimatorStateInfo(0).IsName("Change Bet Intro") || 
				panelAnimator.GetCurrentAnimatorStateInfo(0).IsName("Change Bet Outro") ||
				panelAnimator.GetCurrentAnimatorStateInfo(0).IsName("Change Bet Outro 0")
			)
			{
				panelAnimator.Play("Idle");
			}
		}
	}
}
