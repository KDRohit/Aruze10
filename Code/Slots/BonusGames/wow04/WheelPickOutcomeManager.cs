using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 Handle the redistribution of outcomes to match the server side's results.  
 */
public class WheelPickOutcomeManager : TICoroutineMonoBehaviour
{
	//Result options are 7, 16, 18, 19, 26, 56, in that order
	public GameObject resultOfThreeBlue;
	public GameObject resultOfOneRed;
	public GameObject resultOfOneBlueOneRed;
	public GameObject resultOfTwoBlueOneRed;
	public GameObject resultOfOneBlueTwoRed;
	public GameObject resultOfThreeRed;
	
	// Used to randomize remaining picks
	private List<GameObject> remainingOutcomes;
	
	//Swap up positions appropriately based upon the number mask of the result and the position needed to teleport to.
	public void makePick(int outcomeToMatch, int positionClicked)
	{
		GameObject selectedResult = null;
		GameObject resultToSwap = null;
		
		remainingOutcomes = new List<GameObject>();
		remainingOutcomes.Add(resultOfThreeBlue);
		remainingOutcomes.Add(resultOfOneRed);
		remainingOutcomes.Add(resultOfOneBlueOneRed);
		remainingOutcomes.Add(resultOfTwoBlueOneRed);
		remainingOutcomes.Add(resultOfOneBlueTwoRed);
		remainingOutcomes.Add(resultOfThreeRed);
		
		switch (outcomeToMatch)
		{
			case 7: 
				selectedResult = resultOfThreeBlue;
				remainingOutcomes.Remove(resultOfThreeBlue);
			break;
			case 16: 
				selectedResult = resultOfOneRed;
				remainingOutcomes.Remove(resultOfOneRed);
			break;
			case 18: 
				selectedResult = resultOfOneBlueOneRed;
				remainingOutcomes.Remove(resultOfOneBlueOneRed);
			break;
			case 19: 
				selectedResult = resultOfTwoBlueOneRed;
				remainingOutcomes.Remove(resultOfTwoBlueOneRed);
			break;
			case 26: 
				selectedResult = resultOfOneBlueTwoRed;
				remainingOutcomes.Remove(resultOfOneBlueTwoRed);
			break;
			case 56: 
				selectedResult = resultOfThreeRed;
				remainingOutcomes.Remove(resultOfThreeRed);
			break;
		}
		
		//The easiest solution is to swap the positions of the target and what was clicked.  If they are equal, do nothing.
		//TODO: Shuffle the icons to make appear more random.
		//Starting positions are 0 = BBB, 1 = R, 2 = BR, 3 = BBR, 4 = BRR, 5 = RRR
		switch (positionClicked)
		{
			case 0: 
				resultToSwap = resultOfThreeBlue;
			break;
			case 1:
				resultToSwap = resultOfOneRed;
			break;
			case 2:
				resultToSwap = resultOfOneBlueOneRed;
			break;
			case 3:
				resultToSwap = resultOfTwoBlueOneRed;
			break;
			case 4:
				resultToSwap = resultOfOneBlueTwoRed;
			break;
			case 5:
				resultToSwap = resultOfThreeRed;
			break;
		}
		
		if (selectedResult != null && resultToSwap != null && resultToSwap != selectedResult) 
		{
			Vector3 positionOfSelected = selectedResult.transform.position;
			Vector3 positionOfSwapper = resultToSwap.transform.position;
			selectedResult.transform.position = positionOfSwapper;
			resultToSwap.transform.position = positionOfSelected;
		}
		
		// Randomize remaining pics by swapping positions randomly
		randomizeRemainingPicks(remainingOutcomes);
	}
	
	/// Randomize remaining picks, pass in remaining outcomes, boom.
	private void randomizeRemainingPicks(List<GameObject> remainingPicks)
	{
		// First darken call picks.
		foreach (GameObject go in remainingPicks)
		{
			// Darken Sprite.
			UISprite sprite = go.GetComponentInChildren<UISprite>();
			if (sprite != null)
			{
				sprite.color = new Color(0.25f, 0.25f, 0.25f);
			}
			
			// Set Numbers to a Disabled Color.
			UILabel label = go.GetComponentInChildren<UILabel>();
			if (label != null)
			{
				label.color = new Color(0.76f, 0.69f, 0.57f);
			}
		}
		
		// Do the shuffle.
		for (int i = 1; i < remainingPicks.Count; i++)
		{
			// Get random pick left in remaining picks
			int r = Random.Range(i, remainingPicks.Count);
			
			// Swap it.
			Vector3 positionCurrent = remainingPicks[0].transform.position;
			Vector3 positionOfSwapper = remainingPicks[r].transform.position;
			remainingPicks[0].transform.position = positionOfSwapper;
			remainingPicks[r].transform.position = positionCurrent;
			
			// Remove swapped element.
			remainingPicks.RemoveAt(0);
		}
	}
}

