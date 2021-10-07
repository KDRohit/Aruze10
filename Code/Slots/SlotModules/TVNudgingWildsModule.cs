using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
This module adds reel nudging based on multi-index spanning wild symbols (such as in tv15 Elvira).
Original Author: Chad McKinney
*/

/* Adding Spec for tv32
[5/3/16, 3:45:45 PM] Bennett Yeates: ok we have a tv32 coming, it’s a “franken clone” of tv01, tv14/15/16/17
[5/3/16, 3:45:58 PM] Bennett Yeates: it has a similar nudging mechanic, but it’s on the major symbols
[5/3/16, 3:46:25 PM] Bennett Yeates: the real difference in terms of data handling, is that tv32 nudging falls through the “reevaluation” type
[5/3/16, 3:46:43 PM] Bennett Yeates: it’s currently named “symbol_nudging”
[5/3/16, 3:47:10 PM] Bennett Yeates: here’s an example from the server response
[5/3/16, 3:47:12 PM] Bennett Yeates: "creation_time": 1462311948,
"reevaluations": [
		{
			"type": "symbol_nudging",
			"stops": [
				65,
				5,
				72
			],
*/
public class TVNudgingWildsModule : SlotModule 
{
	static readonly Quaternion FLIP_ROTATION = Quaternion.Euler(0, 0, 180);
	
	[System.Serializable]
	public struct PresentationObject
	{
		public GameObject gameObject;
		public bool isAlignedToReel;
		public bool isDirectional;
	}
	
	[System.Serializable]
	public struct PresentationAnimator
	{
		public Animator animator;
		public string STATE_NAME;
	}
	
	[SerializeField] protected List<string> wildSymbolNames;
	[SerializeField] protected List<PresentationObject> wildNudgePresentationObjects; // objects enabled when nudging
	[SerializeField] protected List<PresentationAnimator> presentationAnimators;
	[SerializeField] protected float presentationDuration = 0.4f;
	[SerializeField] protected float nudgePauseDuration = 0.25f;

	Dictionary<string, int> wildSymbolIndexMap = new Dictionary<string, int>(); // symbol name (key), extended wild symbol index (value)
	Dictionary<int, int> pendingNudges = new Dictionary<int, int>(); // reelID (key), reelOffset (value)
	int numSymbols;
/*
	protected override void OnEnable()
	{
		numSymbols = wildSymbolNames.Count;
		for (int i = 0; i < numSymbols; ++i)
		{
			wildSymbolIndexMap[wildSymbolNames[i]] = i;
		}
		wildSymbolNames.Clear();
		//SlotReel.nudgePauseDuration = nudgePauseDuration;
	}
	
// executeOnSpecificReelStopping() section
// Functions here are executed during the OnSpecificReelStopping (in reelGame) as soon as stop is called, but before the reels completely to a stop.
	public override bool needsToExecuteOnSpecificReelStopping(SlotReel stoppingReel)
	{
		return true;
	}
	public override void executeOnSpecificReelStopping(SlotReel stoppingReel)
	{
		string[] visibleSymbols = stoppingReel.getFinalReelStopsSymbolNames();
		for (int i = 0; i < visibleSymbols.Length; ++i)
		{
			//0 is the bottom in getfinalreelstopssymbolnames vs the top in visible symbols
			string symbol = visibleSymbols[visibleSymbols.Length - i - 1];
			int wildIndex;
			if (wildSymbolIndexMap.TryGetValue(symbol, out wildIndex))
			{
				int reelOffset = i - wildIndex;
				if (reelOffset != 0)
				{
					stoppingReel.addPendingNudge(reelOffset);
					pendingNudges.Add(stoppingReel.reelID, reelOffset);
				}
				break;
			}
		}
	}
	public override bool needsToExecuteOnSpecificReelNudging(SlotReel reel)
	{
		return true;
	}
	
	public override IEnumerator executeOnSpecificReelNudging(SlotReel reel)
	{
		int reelOffset;
		if (pendingNudges.TryGetValue(reel.reelID, out reelOffset))
		{
			foreach (PresentationObject presentationObject in wildNudgePresentationObjects)
			{
				presentationObject.gameObject.SetActive(true);
				if (presentationObject.isAlignedToReel)
				{
					Vector3 position = presentationObject.gameObject.transform.position;
					position.x = reel.getReelGameObject().transform.position.x;
					presentationObject.gameObject.transform.position = position;
				}
				
				if (presentationObject.isDirectional && reelOffset > 0)
				{
					presentationObject.gameObject.transform.localRotation = FLIP_ROTATION;
				}
				else
				{
					presentationObject.gameObject.transform.localRotation = Quaternion.identity;
				}
			}
			foreach (PresentationAnimator presentationAnimator in presentationAnimators)
			{
				const int anyLayer = -1;
				const float time = 0f;
				presentationAnimator.animator.Play(presentationAnimator.STATE_NAME, anyLayer, time);
			}
			if (Audio.canSoundBeMapped("nudge_reel"))
			{
				Audio.play(Audio.soundMap("nudge_reel"));
			}
			yield return new WaitForSeconds(presentationDuration);
			
			foreach (PresentationObject presentationObject in wildNudgePresentationObjects)
			{
				presentationObject.gameObject.SetActive(false);
			}
			pendingNudges.Remove(reel.reelID);
		}
		yield break;
	}
*/
}