using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CollectionsBetPanel : MonoBehaviour 
{
	[SerializeField] private Animator panelAnimator;
	[SerializeField] private Animator[] betPips;
	[SerializeField] private TextMeshPro stateLabel;

	private GameTimerRange closeTimer = null;

	private int currentBetIndex = -1;
	private int totalNumberOfWagers = -1;
	private int currentPipIndex = -1;
	private bool isOpen = false;

	private const string OUTRO_ANIM_NAME = "outro";
	private const string INTRO_ANIM_NAME = "intro";
	private const string BET_INCREASE_ANIM_NAME = "increase";
	private const string BET_DECREASE_ANIM_NAME = "decrease";

	private const string MAX_TIER_OUTRO_ANIM_NAME = "outroSpecial";
	private const string MAX_TIER_INTRO_ANIM_NAME = "introSpecial";
	private const string MAX_TIER_BET_INCREASE_ANIM_NAME = "specialIncrease";
	private const string MAX_TIER_BET_DECREASE_ANIM_NAME = "specialDecrease";

	private const string PIP_ALREADY_ON_ANIM_NAME = "hold";
	private const string PIP_FLASH_ANIM_NAME = "flash";
	private const string PIP_ACTIVATE_ANIM_NAME = "show";

	private const string STATE_LABEL_TEXT = "Chances {0}\nMore + Rare Cards";
	private const string INCREASED_STATE_TEXT = "INCREASED";
	private const string DECREASED_STATE_TEXT = "DECREASED";

	public void init(int _currentBetIndex, int _totalNumberOfWagers)
	{
		currentBetIndex = _currentBetIndex;
		totalNumberOfWagers = _totalNumberOfWagers;
		updateBetPips(false, currentBetIndex);
	}

	public void onBetChanged (int newIndex)
	{
		if (!gameObject.activeSelf)
		{
			gameObject.SetActive(true);
		}

		bool isOpening = !isOpen;
		bool isIncreasing = false;
		bool isDecreasing = false;

		int lastPipIndex = currentPipIndex;
		updateBetPips(isOpening, newIndex);

		if (newIndex > currentBetIndex)
		{
			isIncreasing = true;
			stateLabel.text = string.Format(STATE_LABEL_TEXT, INCREASED_STATE_TEXT);
		}
		else if (newIndex < currentBetIndex)
		{
			isDecreasing = true;
			stateLabel.text = string.Format(STATE_LABEL_TEXT, DECREASED_STATE_TEXT);
		}

		if (!isOpen)
		{
			openPanel();

			if (currentBetIndex != newIndex)
			{
				StartCoroutine(waitBeforeBetChange(isIncreasing, lastPipIndex));
			}
		}
		else if (closeTimer != null && !closeTimer.isExpired)
		{
			if (currentBetIndex != newIndex)
			{
				playBetChange(isIncreasing, lastPipIndex);
			}
			closeTimer.updateEndTime(Collectables.Instance.betIndicatorTimeout);
		}

		currentBetIndex = newIndex;
	}

	private IEnumerator waitBeforeBetChange(bool isIncreasing, int lastPipIndex)
	{
		yield return new WaitForSeconds(0.1f);

		playBetChange(isIncreasing, lastPipIndex);
	}

	private void playBetChange(bool isIncreasing, int lastPipIndex)
	{
		//Reset the timer if its already open and we just changed our bet
		if (isIncreasing)
		{
			if (currentPipIndex == betPips.Length-1)
			{
				//Play the special animation if we're moving up to the highest pip
				panelAnimator.Play(MAX_TIER_BET_INCREASE_ANIM_NAME);
			}
			else
			{
				panelAnimator.Play(BET_INCREASE_ANIM_NAME);
			}
		}
		else
		{
			if (lastPipIndex == betPips.Length-1)
			{
				//Play the special animation if we're moving down from the highest pip
				panelAnimator.Play(MAX_TIER_BET_DECREASE_ANIM_NAME);
			}
			else
			{
				panelAnimator.Play(BET_DECREASE_ANIM_NAME);
			}
		}
	}

	private void updateBetPips(bool isOpening, int newBetIndex)
	{
		if (isOpening && currentPipIndex >= 0) //Make sure to also turn on all the lower pips if we're starting to open the panel
		{
			for (int i = 0; i <= currentPipIndex; i++)
			{
				if (i < betPips.Length)
				{
					betPips[i].gameObject.SetActive(true);
					betPips[i].Play(PIP_ALREADY_ON_ANIM_NAME);
				}
			}
		}

		++newBetIndex;
		int updatedPipIndex = (int)Mathf.Clamp((newBetIndex * (betPips.Length))/totalNumberOfWagers, 0.0f, (float)betPips.Length); //Pip index in the array
		--updatedPipIndex;
		if (updatedPipIndex != currentPipIndex)
		{
			if (updatedPipIndex > currentPipIndex)
			{
				betPips[updatedPipIndex].gameObject.SetActive(true);
				betPips[updatedPipIndex].Play(PIP_ACTIVATE_ANIM_NAME);
			}
			else
			{
				betPips[currentPipIndex].gameObject.SetActive(false);
			}

			currentPipIndex = updatedPipIndex;
		}
		else if (currentPipIndex != -1) //Just flash the current pip if we didn't move tiers. Only -1 when we're not showing any lit pips
		{
			betPips[currentPipIndex].Play(PIP_FLASH_ANIM_NAME);
		}
	}

	public void openPanel()
	{
		isOpen = true;
		StartCoroutine(playIntro());
	}

	public void closePanel(Dict data = null, GameTimerRange sender = null)
	{
		if (this != null && isOpen && gameObject.activeSelf)
		{
			StartCoroutine(playOutro());
		}
	}

	private IEnumerator playIntro()
	{
		string introAnim = currentPipIndex == betPips.Length-1 ? MAX_TIER_INTRO_ANIM_NAME : INTRO_ANIM_NAME;
		yield return StartCoroutine(CommonAnimation.playAnimAndWait(panelAnimator, introAnim));
		if (closeTimer == null)
		{
			closeTimer = GameTimerRange.createWithTimeRemaining(Collectables.Instance.betIndicatorTimeout);
			closeTimer.registerFunction(closePanel);
		}
		else
		{
			closeTimer.updateEndTime(Collectables.Instance.betIndicatorTimeout);
			if (!closeTimer.isEventRegisteredOnActiveTimer(closePanel))
			{
				closeTimer.registerFunction(closePanel);
			}
		}
	}

	private IEnumerator playOutro()
	{
		isOpen = false;
		string outroAnim = currentPipIndex == betPips.Length-1 ? MAX_TIER_OUTRO_ANIM_NAME : OUTRO_ANIM_NAME;
		yield return StartCoroutine(CommonAnimation.playAnimAndWait(panelAnimator, outroAnim));
		if (!isOpen) //Its possible we reopened this in the middle of the animation
		{
			gameObject.SetActive(false);
		}
	}
}
