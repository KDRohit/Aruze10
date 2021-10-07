using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Fades an array of labels to be displayed one at a time in a series.
*/

public class LabelCycler : TICoroutineMonoBehaviour
{
	public TextMeshPro[] labels;
	public float displayDuration;
	public float fadeDuration;
	
	private GameTimer labelTransitionTimer;
	private int currentVisibleLabelIndex = 0;
	private int previousVisibleLabelIndex = -1;
	private bool _shouldStopTransition = false;
	
	void Awake()
	{
		// Make sure only the first label is visible by default.
		for (int i = 0; i < labels.Length; i++)
		{
			labels[i].alpha = (i == 0 ? 1.0f : 0.0f);
			labels[i].gameObject.SetActive(true);
		}

		// Set up the timer to transition periodically.
		labelTransitionTimer = new GameTimer(displayDuration);
	}
	
	void Update()
	{
		if (labelTransitionTimer != null && labelTransitionTimer.isExpired && !_shouldStopTransition)
		{
			doLabelTransition();
		}
	}

	public void stopTransition(int labelIndex)
	{
		_shouldStopTransition = true;
		if (currentVisibleLabelIndex != labelIndex)
		{
			previousVisibleLabelIndex = currentVisibleLabelIndex;
			currentVisibleLabelIndex = labelIndex % labels.Length;
			updateLabelAlpha(1.0f);
		}
	}
	
	// Crossfade between two labels after a certain amount of time.
	public void doLabelTransition()
	{
		_shouldStopTransition = false;

		previousVisibleLabelIndex = currentVisibleLabelIndex;
		currentVisibleLabelIndex = (currentVisibleLabelIndex + 1) % labels.Length;
		
		iTween.ValueTo(
			gameObject,
			iTween.Hash(
				"from", 0.0f,
				"to", 1.0f,
				"time", fadeDuration,
				"onupdate", "updateLabelAlpha"
			)
		);
		
		labelTransitionTimer.startTimer(displayDuration);
	}
	
	// Update the two labels being cross-faded.
	private void updateLabelAlpha(float alpha)
	{
		labels[currentVisibleLabelIndex].alpha = alpha;
		labels[previousVisibleLabelIndex].alpha = 1.0f - alpha;
	}
}
