using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Controller for PickingGameMultiAdvanceWithMeterModule and PickingGameMultiAdvanceAndCreditsWithMeterModule
*/
[System.Serializable]
public class PickingGameMultiAdvanceWithMeterModuleController
{
	[SerializeField] private List<AnimationListController.AnimationInformationList> advanceMeterAnimations = new List<AnimationListController.AnimationInformationList>();
	[SerializeField] private Transform[] particleTrailTargets;
	// advanceRevealSounds covers unique sounds that need to trigger based on how many advance icons have been found, if you use this you probably don't want to use REVEAL_AUDIO
	[SerializeField] private List<AudioListController.AudioInformationList> advanceRevealSounds = new List<AudioListController.AudioInformationList>();

	protected int numAdvancesFound = 0;	// Tracks how many advance icons the player has revealed, tied to the list of meter animations

	public IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem, GameObject effectsParent)
	{
		if (advanceMeterAnimations.Count != 0 && numAdvancesFound < advanceMeterAnimations.Count)
		{
			if (advanceRevealSounds.Count != 0 && numAdvancesFound < advanceRevealSounds.Count)
			{
				yield return RoutineRunner.instance.StartCoroutine(AudioListController.playListOfAudioInformation(advanceRevealSounds[numAdvancesFound]));
			}

			if (particleTrailTargets.Length > 0) 
			{
				if (numAdvancesFound < particleTrailTargets.Length) 
				{
					ParticleTrailController particleTrailController = ParticleTrailController.getParticleTrailControllerForType(pickItem.gameObject, ParticleTrailController.ParticleTrailControllerType.Advance);
					yield return RoutineRunner.instance.StartCoroutine(particleTrailController.animateParticleTrail(particleTrailTargets[numAdvancesFound].position, effectsParent.transform));
				} 
				else 
				{
					Debug.LogWarning("Can't do particle trail because numAdvancesFound: " + numAdvancesFound + "; is less than particleTrailTargets.Length: " + particleTrailTargets.Length);
				}
			}

			// don't wait just play the animation and let it go since it shouldn't really need to block the game
			yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(advanceMeterAnimations[numAdvancesFound]));

			numAdvancesFound++;
		}

		yield break;
	}
}
