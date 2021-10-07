using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Class meant to allow a set of animators to randomly set their state from a similar animator controller.
 * This means that all Animators should use the same controller (this will be verified).  Also, the number of states
 * in the Animator Controller must be greater than or equal to the number of animators (otherwise not every animator would be
 * unique).
 *
 * Original Author: Scott Lepthien
 * Creation Date: 4/6/2021
 */
public class PlayUniqueAnimationStateOnAnimators : TICoroutineMonoBehaviour
{
	[Tooltip("Array of animators that will be given a random state from the possibleAnimations list")]
	[SerializeField] private Animator[] animators;
	[Tooltip("Sort of hacking the animation list here in order to allow the setting of valid random states that the animators can be set to.  This is NOT a list that will be played.")]
	[SerializeField] private List<AnimatorStateInformation> possibleAnimations;

	// Start is called before the first frame update
	private void Awake()
	{
		// Validate that the animators all share the same controller
		bool isEveryControllerSame = true;
		if (animators.Length > 0)
		{
			RuntimeAnimatorController controller = animators[0].runtimeAnimatorController;
			for (int i = 1; i < animators.Length; i++)
			{
				if (animators[i].runtimeAnimatorController != controller)
				{
					Debug.LogError($"PlayUniqueAnimationStateOnAnimators.Awake() - Controller on animator: {i} does not match the controller on the first animator in the array!");
					isEveryControllerSame = false;
				}
			}
		}

		if (!isEveryControllerSame)
		{
			// Going to abort going forward since we might break something by trying to play states that don't exist in all the controllers
			// because they don't match
			return;
		}

		if (animators.Length > possibleAnimations.Count)
		{
			Debug.LogError($"PlayUniqueAnimationStateOnAnimators.Awake() - possibleAnimations.Count = {possibleAnimations.Count} was shorter than animators.Length = {animators.Length} will not be able to make animations unique. Aborting!");
			return;
		}

		List<AnimatorStateInformation> possibleAnimationsCopy = new List<AnimatorStateInformation>(possibleAnimations);
		foreach (Animator animatorToSet in animators)
		{
			AnimatorStateInformation randomAnimation = possibleAnimationsCopy[Random.Range(0, possibleAnimationsCopy.Count)];
			if (animatorToSet.gameObject.activeInHierarchy)
			{
				animatorToSet.Play(randomAnimation.ANIMATION_NAME, randomAnimation.stateLayer);
			}
			else
			{
				Debug.LogWarning($"PlayUniqueAnimationStateOnAnimators.Awake() - animatorToSet.gameObject.name = {animatorToSet.gameObject.name} was not enabled, so the animation state couldn't be played!");	
			}
			possibleAnimationsCopy.Remove(randomAnimation);
		}
	}
}
