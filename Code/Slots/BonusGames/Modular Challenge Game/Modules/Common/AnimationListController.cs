using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * A wrapper class play a collection of animations concurrently.
 */
public static class AnimationListController
{
	[System.Serializable]
	public class AnimationInformation
	{
		public Animator targetAnimator;
		public string ANIMATION_NAME = "intro";
		public float delay = 0.0f;
		[UnityEngine.Serialization.FormerlySerializedAs("soundsPlayedAtAnimationStart")] public AudioListController.AudioInformationList soundsPlayedDuringAnimation = new AudioListController.AudioInformationList();
		public bool isBlockingModule = false;

		public float durationOverride = 0.0f; // Wait for this duration instead of waiting for the animation duration.
		public int stateLayer = 0; // if using a different animator layer, play from it instead of the default

#region CrossFade
		public bool isCrossFading = false;
		public float fixedCrossFadeTransitionDuration = 0.0f;
		[Range(0.0f, 1.0f)] public float normalizedCrossFadeTransitionTime = 0.0f;
#endregion

		public bool destroyGameObjectOnComplete = false;

		// Creates a deep copy of this AnimationInformation. Note that target animator will need to be
		// set in order to apply this to a different animator.
		public AnimationInformation clone()
		{
			AnimationInformation clone = (AnimationInformation) this.MemberwiseClone();
			clone.soundsPlayedDuringAnimation = soundsPlayedDuringAnimation.clone();
			return clone;
		}
	}

	// Class to allow serialization of a List of Lists of AnimationInformation
	[System.Serializable]
	public class AnimationInformationList
	{
		public bool isAllowingTapToSkip = false;
		public List<AnimationListController.AnimationInformation> animInfoList = new List<AnimationListController.AnimationInformation>();
		public List<AnimationListController.AnimationInformation> skippedAnimsFinalStatesList = new List<AnimationListController.AnimationInformation>();

		[System.NonSerialized] public CoroutineObjectTracker coroutineTracker = new CoroutineObjectTracker();

		public int Count
		{
			get { return animInfoList.Count; }
		}
	}

	[System.Serializable]
	public class RandomizedAnimationInformationLists
	{
		[SerializeField] private List<AnimationInformationListWithProbability> animationProbabilityList;

		public AnimationInformationList getRandomAnimationInformationList()
		{
			int totalProb = 0;
			foreach (AnimationInformationListWithProbability animWithProb in animationProbabilityList)
			{
				totalProb += animWithProb.probability;
			}
			
			// Generate a number between 1 and the max range, we are going to ignore anything with probability set to 0
			int randomNum = Random.Range(1, totalProb + 1);
			
			// Now find the animation list associated with the random number
			int currentValue = 0;
			foreach (AnimationInformationListWithProbability animWithProb in animationProbabilityList)
			{
				if (animWithProb.probability > 0)
				{
					currentValue += animWithProb.probability;
					if (randomNum <= currentValue)
					{
						return animWithProb.animList;
					}
				}
			}
			
			return null;
		}
		
		[System.Serializable]
		private class AnimationInformationListWithProbability
		{
			[Tooltip("Probability setup similar to how we define them on the server.  All probabilities will be added together, and then a random number will be generated for the range, and based on where that number falls in the total list of probabilities will determine what plays.")]
			public int probability;
			public AnimationInformationList animList;
		}
	}

	// Plays a randomized animation based on a probability list
	public static IEnumerator playRandomizedAnimationInformationList(RandomizedAnimationInformationLists randomizer, List<TICoroutine> runningCoroutines = null, bool includeAudio = true)
	{
		AnimationInformationList animListToPlay = randomizer.getRandomAnimationInformationList();
		if (animListToPlay != null)
		{
			yield return RoutineRunner.instance.StartCoroutine(playListOfAnimationInformation(animListToPlay, runningCoroutines, includeAudio));
		}
	}

	// Plays a list of Animation information, if running coroutines are passed to this it waits for all blocking animations in the infos list
	// to finish running and all the coroutines in the runningAnimaitons list to finish running too.
	public static IEnumerator playListOfAnimationInformation(AnimationInformationList infos, List<TICoroutine> runningCoroutines = null, bool includeAudio = true)
	{
		if (runningCoroutines == null)
		{
			// If we were not passed any coroutines that were already running then we don't need to worry about waiting for stuff to finish.
			runningCoroutines = new List<TICoroutine>();
		}

		foreach (AnimationInformation info in infos.animInfoList)
		{
			// Play the animation
			if (info.isBlockingModule || AudioListController.isAnyOfListBlocking(info.soundsPlayedDuringAnimation))
			{
				runningCoroutines.Add(RoutineRunner.instance.StartCoroutine(playAnimationInformation(info, includeAudio, infos)));
			}
			else
			{
				TICoroutine playAnimCoroutine = RoutineRunner.instance.StartCoroutine(playAnimationInformation(info, includeAudio, infos));

				if (infos.isAllowingTapToSkip)
				{
					// Need to add each of these one at a time, since they aren't going to be part of runningCoroutines
					infos.coroutineTracker.addTrackedCoroutine(RoutineRunner.instance, playAnimCoroutine);
				}
			}
		}

		if (infos.isAllowingTapToSkip)
		{
			infos.coroutineTracker.addTrackedCoroutineList(RoutineRunner.instance, runningCoroutines);
			TICoroutine skippableCoroutine = RoutineRunner.instance.StartCoroutine(Common.waitForTapToSkipCoroutinesToEnd(infos.coroutineTracker));

			bool wasSkipped = false;
			// Need to capture if the player tapped to skip at this level as well as inside the common function
			// so we can handle playing animations to force the game into the correct state when the player
			// taps the screen
			while (!skippableCoroutine.finished)
			{
				yield return null;
				wasSkipped = wasSkipped || TouchInput.didTap;
			}

			if (wasSkipped)
			{
				List<TICoroutine> endStateAnimCoroutines = new List<TICoroutine>();
				foreach (AnimationInformation info in infos.skippedAnimsFinalStatesList)
				{
					// Play the animation
					if (info.isBlockingModule || AudioListController.isAnyOfListBlocking(info.soundsPlayedDuringAnimation))
					{
						endStateAnimCoroutines.Add(RoutineRunner.instance.StartCoroutine(playAnimationInformation(info, includeAudio, infos)));
					}
					else
					{
						RoutineRunner.instance.StartCoroutine(playAnimationInformation(info, includeAudio, infos));
					}
				}

				if (endStateAnimCoroutines.Count > 0)
				{
					yield return RoutineRunner.instance.StartCoroutine(Common.waitForCoroutinesToEnd(endStateAnimCoroutines));
				}
			}
		}
		else
		{
			if (runningCoroutines.Count > 0)
			{
				yield return RoutineRunner.instance.StartCoroutine(Common.waitForCoroutinesToEnd(runningCoroutines));
			}
		}
	}

	public static IEnumerator playAnimationInformation(AnimationInformation info, bool includeAudio = true, AnimationInformationList animInfoList = null)
	{
		if (info != null)
		{
			if (info.delay > 0)
			{
				yield return new TIWaitForSeconds(info.delay);
			}

			List<TICoroutine> runningCoroutines = new List<TICoroutine>();
			// Start Animaiton
			if (info.isBlockingModule)
			{
				runningCoroutines.Add(RoutineRunner.instance.StartCoroutine(playAnimation(info, animInfoList)));
			}
			else
			{
				RoutineRunner.instance.StartCoroutine(playAnimation(info, animInfoList));
			}
			// Start the audio
			if (includeAudio)
			{
				if (AudioListController.isAnyOfListBlocking(info.soundsPlayedDuringAnimation))
				{
					runningCoroutines.Add(RoutineRunner.instance.StartCoroutine(playAudio(info, animInfoList)));
				}
				else
				{
					RoutineRunner.instance.StartCoroutine(playAudio(info, animInfoList));
				}
			}
			
			// if a animInfoList was passed in, and it is allowing click to cancel we need to track
			// the coroutines created in here
			if (animInfoList != null && animInfoList.isAllowingTapToSkip)
			{
				animInfoList.coroutineTracker.addTrackedCoroutineList(RoutineRunner.instance, runningCoroutines);
			}
			
			yield return RoutineRunner.instance.StartCoroutine(Common.waitForCoroutinesToEnd(runningCoroutines));
		}
	}

	private static IEnumerator playAnimation(AnimationInformation info, AnimationInformationList animInfoList = null)
	{
		if (info != null)
		{
			if (info.targetAnimator != null)
			{
				if (!info.targetAnimator.gameObject.activeSelf || !info.targetAnimator.gameObject.activeInHierarchy)
				{
					Debug.LogWarning(string.Format("Gameobject {0} not active when trying to play animation: {1}", info.targetAnimator.gameObject.name, info.ANIMATION_NAME));
					//destroy now because we can't start aninmation
					if (info.destroyGameObjectOnComplete)
					{
						if (info.targetAnimator != null)
						{
							Object.Destroy(info.targetAnimator.gameObject);
						}
						else
						{
							Debug.LogError("Trying to destroy gameObject using null targetAnimator. info.ANIMATION_NAME = " + info.ANIMATION_NAME);
						}

						info.targetAnimator = null;
					}
					yield break;
				}
				
				if (info.durationOverride > 0.0f)
				{
					if (info.isCrossFading)
					{
						info.targetAnimator.CrossFade(info.ANIMATION_NAME, info.fixedCrossFadeTransitionDuration, info.stateLayer, info.normalizedCrossFadeTransitionTime);
					}
					else
					{
						info.targetAnimator.Play(info.ANIMATION_NAME, info.stateLayer);
					}
					
					yield return new TIWaitForSeconds(info.durationOverride);
				}
				else
				{
					if (info.isCrossFading)
					{
						TICoroutine animCoroutine = RoutineRunner.instance.StartCoroutine
						(
							CommonAnimation.crossFadeAnimAndWait(info.targetAnimator, info.ANIMATION_NAME, info.fixedCrossFadeTransitionDuration, info.delay, info.normalizedCrossFadeTransitionTime, info.stateLayer)
						);

						if (animInfoList != null && animInfoList.isAllowingTapToSkip)
						{
							animInfoList.coroutineTracker.addTrackedCoroutine(RoutineRunner.instance, animCoroutine);
						}
						
						yield return animCoroutine;
					}
					else
					{
						TICoroutine animCoroutine = RoutineRunner.instance.StartCoroutine
						(
							CommonAnimation.playAnimAndWait(info.targetAnimator, info.ANIMATION_NAME, 0.0f, info.stateLayer)
						); // delay is 0f here as it gets handled in this case before this function is called
						
						if (animInfoList != null && animInfoList.isAllowingTapToSkip)
						{
							animInfoList.coroutineTracker.addTrackedCoroutine(RoutineRunner.instance, animCoroutine);
						}
						
						yield return animCoroutine;
					}
 				}

				if (info.destroyGameObjectOnComplete)
				{
					if (info.targetAnimator != null)
					{
						Object.Destroy(info.targetAnimator.gameObject);
					}
					else
					{
						Debug.LogError("Trying to destroy gameObject using null targetAnimator. info.ANIMATION_NAME = " + info.ANIMATION_NAME);
					}

					info.targetAnimator = null;
				}
 			}
			else if (!info.destroyGameObjectOnComplete)
			{
				Debug.LogWarning("Animation info not set properly. info.ANIMATION_NAME = " + info.ANIMATION_NAME);
			}
		}
	}
	
	private static IEnumerator playAudio(AnimationInformation info, AnimationInformationList animInfoList = null)
	{
		if (info != null && info.soundsPlayedDuringAnimation != null)
		{
			TICoroutine audioCoroutine = RoutineRunner.instance.StartCoroutine(AudioListController.playListOfAudioInformation(info.soundsPlayedDuringAnimation, null, animInfoList));
			
			if (animInfoList != null && animInfoList.isAllowingTapToSkip)
			{
				animInfoList.coroutineTracker.addTrackedCoroutine(RoutineRunner.instance, audioCoroutine);
			}
			
			yield return audioCoroutine;
		}
	}

	public static void changeSoundName(AnimationInformationList infos, string oldName, string newName)
	{
		foreach (AnimationInformation info in infos.animInfoList)
		{		
			AudioListController.changeSoundName(info.soundsPlayedDuringAnimation, oldName, newName);
		}	
	}	
}
