using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Used to override the default scatter symbol landing sounds
public class ScatterSymbolLandingSoundModule : SlotModule
{
	[SerializeField] protected AudioListController.AudioInformationList scatterHitSounds;

	public override bool isOverridingAnticipationSounds(int stoppedReel, Dictionary<int, string> anticipationSymbols, int bonusHits, bool bonusHit = false, bool scatterHit = false, bool twHIT = false, int layer = 0)
	{
		return scatterHit;
	}

	public override bool needsToExecuteOnPlayAnticipationSound(int stoppedReel, Dictionary<int, string> anticipationSymbols, int bonusHits, bool bonusHit = false, bool scatterHit = false, bool twHIT = false, int layer = 0)
	{
		return true;
	}

	public override IEnumerator executeOnPlayAnticipationSound(int stoppedReel, Dictionary<int, string> anticipationSymbols, int bonusHits, bool bonusHit = false, bool scatterHit = false, bool twHIT = false, int layer = 0)
	{
		yield return StartCoroutine(AudioListController.playListOfAudioInformation(scatterHitSounds));
	}
}
