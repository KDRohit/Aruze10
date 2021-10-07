using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class XPUIState : MonoBehaviour
{
	public XPUI.State state;

	public UISprite xpMeter;
	public TextMeshPro levelLabel;
	public Transform xpMeterBG;
	public Transform xpMeterGlow;
	public TextMeshPro[] barLabels;
	public UIAnchor xpMeterAnchor;
	public UIAnchor xpMeterGlowAnchor;
	public bool shouldIncludeLevelLabel = true;

	// Variables for controlling the font swapping speeds.
	public float alphaDuration = 0.25f;
	public float showDuration = 4.0f;

	// In case this state needs to display multiple pieces of info
	public OneShotTextCycle textCycler;

	private int currentLabelIndex = 0;
	private int oldLabelIndex = -1;
	private GameTimer labelSwapTimer;
	private GameTimer timerTextUpdateTimer; // timer for updating timerText-related UI
	private Dictionary<TextMeshPro, GameTimer> labelTimers = new Dictionary<TextMeshPro, GameTimer>();

	private bool isTinyMode = false;	// true if xp bar is less than 400 pixels long forcing us to render a little different

	public void init()
	{		
		setupLabels();
		updateTimerLabels();
		setActive(false);
		timerTextUpdateTimer = new GameTimer(0);  // 0 because we want an immediate update on first call to update()
		if (barLabels.Length > 0 && shouldIncludeLevelLabel)
		{
			System.Array.Resize(ref barLabels, barLabels.Length+1);
			levelLabel.color = CommonColor.adjustAlpha(levelLabel.color, 0.0f);
			barLabels[barLabels.Length-1] = levelLabel;
		}
	}

	public void updateDefaultCycleText(string text)
	{
		if (textCycler != null)
		{
			textCycler.updateDefaultText(text);
		}
	}
	public void swapLabelBetweenText(string newText)
	{
		if (textCycler != null)
		{
			textCycler.cycleOnce(newText, levelLabel.text);
		}
	}
	
	// change the look if the bar is really small
	public void enterTinyMode()
	{
		if (isTinyMode)
		{
			return;
		}

		isTinyMode = true;	
	}
	
	public void update()
	{
		if (isValid())
		{
			if (barLabels.Length > 1 && (labelSwapTimer == null || labelSwapTimer.isExpired))
			{
				// If we have more than one label, then do label shenaigans
				if (oldLabelIndex < 0)
				{
					// If this is the first time we are running through the labels.
					StartCoroutine(fadeBetweenLabels());
					oldLabelIndex = currentLabelIndex;
				}
				else
				{
					oldLabelIndex = currentLabelIndex;
					// Go to the next label, and wrap around the array if at the end.
					currentLabelIndex = (currentLabelIndex + 1) % barLabels.Length;
					StartCoroutine(fadeBetweenLabels());
				}
				if (labelSwapTimer == null)
				{
					labelSwapTimer = new GameTimer(alphaDuration + showDuration);
				}
				else
				{
					labelSwapTimer.startTimer(alphaDuration + showDuration);
				}
			}

			if (timerTextUpdateTimer!=null && timerTextUpdateTimer.isExpired)
			{
				updateTimerLabels();
				timerTextUpdateTimer.startTimer(1.0f); // timertext needs to be updated once/second
			}
		}
	}
	
	public void click()
	{
		if (Overlay.instance.top.xpUI.isWaitingForClickProcessing)
		{
			// Already clicked and still waiting for that one to process, so don't do anything else.
			return;
		}
		Overlay.instance.top.xpUI.isWaitingForClickProcessing = true;
		
		// Use the XP UI as the coroutine host, to make sure it keeps running even if this state gets disabled.
		Overlay.instance.top.xpUI.StartCoroutine(processClick());
	}
	
	private IEnumerator processClick()
	{
		while (!Glb.isNothingHappening)
		{
			yield return null;
		}
		
		bool shouldCheckState = true;

		if (shouldCheckState && Overlay.instance != null && Overlay.instance.topV2 != null && Overlay.instance.topV2.xpUI != null)
		{
			Overlay.instance.top.xpUI.checkEventStates();
		}

		if (Overlay.instance != null && Overlay.instance.topV2 != null && Overlay.instance.topV2.xpUI != null)
		{
			Overlay.instance.top.xpUI.isWaitingForClickProcessing = false;
		}
	}
	
	// If ignoreSharedObjects is passed in, then any objects that this state shares with that state are ignored (not changed).
	public void setActive(bool isActive, XPUIState ignoreSharedObjects = null)
	{
		if (this == null || this.gameObject == null)
		{
			return;
		}
		// I have made it so that everything should be linked in each state, so no need to safeset, we want those errors.
		setElementActive(isActive, xpMeter.gameObject, (ignoreSharedObjects == null || ignoreSharedObjects.xpMeter == null ? null : ignoreSharedObjects.xpMeter.gameObject));
		setElementActive(isActive, levelLabel.gameObject, (ignoreSharedObjects == null || ignoreSharedObjects.levelLabel == null ? null : ignoreSharedObjects.levelLabel.gameObject));
		setElementActive(isActive, xpMeterBG.gameObject, (ignoreSharedObjects == null || ignoreSharedObjects.xpMeterBG == null ? null : ignoreSharedObjects.xpMeterBG.gameObject));

		for (int i = 0; i < barLabels.Length; i++)
		{
			// Always set the labels, because typically labels aren't shared between different states.
			if (barLabels[i] != null)
			{
				barLabels[i].gameObject.SetActive(isActive);

				if (isActive)
				{
					// If its not the first one, then make it invisible, we will cycle through them on update()
					float startingAlpha = (i == 0) ? 1f : 0f; 
					barLabels[i].color = CommonColor.adjustAlpha(barLabels[i].color, startingAlpha);
				}
			}
		}
		gameObject.SetActive(isActive); // Now toggle this state object.

		if (isActive)
		{
			// If we are turning it on, then lets tell the labels to check themselves again in case something
			// got turned on/changed since we did the initial init.
			setupLabels();
		}
		else
		{
			oldLabelIndex = -1;
			currentLabelIndex = 0;
			iTween.Stop(gameObject); // Make sure to stop any tweens currently chugging.
		}
	}

	public void updateActiveState()
	{
		setupLabels();
	}

	private void setElementActive(bool isActive, GameObject obj, GameObject ignoreObj)
	{
		if (obj != ignoreObj)
		{
			obj.SetActive(isActive);
		}
	}

	// State based check on the timer/timer labels for this state.
	// Because this can change during gameplay for some events (
	private void setupLabels(Dict args = null, GameTimerRange originalTimer = null)
	{
		labelTimers.Clear(); // Resetting this before add to it.

		if (state == XPUI.State.SPECIAL)
		{
			if (XPMultiplierEvent.instance.isEnabled)
			{
				barLabels[0].text = XPMultiplierDialog.getMultiplierString(XPMultiplierEvent.instance.xpMultiplier);
				if (XPMultiplierEvent.instance.featureTimer != null)
				{
					GameTimerRange timer = XPMultiplierEvent.instance.getTimer();
					if (timer != null)
					{
						labelTimers.Add(barLabels[1], timer.endTimer);
						timer.registerFunction(setupLabels);	
					}
				}
			}
			else if (LevelUpBonus.isBonusActive)
			{
				barLabels[0].text = Localize.textUpper(LevelUpBonus.patternKey + "_level_bonus");
				labelTimers.Add(barLabels[1], LevelUpBonus.timeRange.endTimer);
				LevelUpBonus.timeRange.registerFunction(setupLabels);
			}
		}
		else if (state != XPUI.State.MAX_LEVEL)
		{
			levelLabel.color = CommonColor.adjustAlpha(levelLabel.color, 1.0f);
		}
	}

	private void updateTimerLabels()
	{
		if (labelTimers.Count > 0)
		{
			foreach (KeyValuePair<TextMeshPro, GameTimer> pair in labelTimers)
			{
				// We could throw a log if one of these is null here, but it's pretty harmless so long as we catch it
				if (pair.Key != null && pair.Value != null)
				{
					pair.Key.text = pair.Value.timeRemainingFormatted;
				}
			}
		}
	}
	
	// Fades out the second label (if it exists) and fades in the new label.
	private IEnumerator fadeBetweenLabels()
	{
		if (oldLabelIndex >= 0)
		{
			// Only fade out the label if we were showing one before.
			iTween.ValueTo(
				gameObject,
				iTween.Hash(
					"from", 1f,
					"to", 0f,
					"time", alphaDuration,
					"onupdate", "updateOldLabelAlpha"
				)
			);
			yield return new WaitForSeconds(alphaDuration);
		}

		iTween.ValueTo(
			gameObject,
			iTween.Hash(
				"from", 0f,
				"to", 1f,
				"time", alphaDuration,
				"onupdate", "updateCurrentLabelAlpha"
			)
		);
	}

	private void updateOldLabelAlpha(float alpha)
	{
		barLabels[oldLabelIndex].color = CommonColor.adjustAlpha(barLabels[oldLabelIndex].color, alpha);
	}
	
	private void updateCurrentLabelAlpha(float alpha)
	{
		barLabels[currentLabelIndex].color = CommonColor.adjustAlpha(barLabels[currentLabelIndex].color, alpha);		
	}

	private bool isValid()
	{
		switch (state)
		{
		case XPUI.State.DEFAULT:
			return true;
		case XPUI.State.SPECIAL:
			return XPMultiplierEvent.instance.isEnabled || LevelUpBonus.isBonusActive;
		default:
			return true;
		}
	}

}
