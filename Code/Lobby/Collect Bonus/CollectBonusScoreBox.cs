using System;
using UnityEngine;
using TMPro;
using System.Collections;

public class CollectBonusScoreBox : TICoroutineMonoBehaviour
{
	public TextMeshPro bottomInfo;
	public TextMeshPro score;
	public GameObject multiplierBurst;
	public GameObject middleContents;
	public GameObject boxNormal;
	public GameObject boxRed;

	private static float DELAY = 0.4f;

	void Awake()
	{
		// Maybe some day we'll use this, but not right now.
		SafeSet.gameObjectActive(multiplierBurst, false);
		
		// Make sure the normal box is shown by default.
		SafeSet.gameObjectActive(boxNormal, true);
		SafeSet.gameObjectActive(boxRed, false);
		
		score.text = "";	// Start with nothing so the rollup is from scratch.
	}

	public IEnumerator rollup(long scoreToRoll, bool shouldPlayStreakSounds = false)
	{
		if (scoreToRoll == 0L)
		{
			yield return new WaitForSeconds(DELAY);
		}
		else
		{
			if (shouldPlayStreakSounds)
			{
				yield return StartCoroutine(SlotUtils.rollup(0L, scoreToRoll, score, true, DELAY, true, true, "DBTallyLoop", "DBDayStreakDayTotal"));
			}
			else
			{
				yield return StartCoroutine(SlotUtils.rollup(0L, scoreToRoll, score, false, DELAY));
			}
		}
	}
	
	public void setValueImmediately(long finalScore)
	{
		score.text = CreditsEconomy.convertCredits(finalScore);
	}
}

