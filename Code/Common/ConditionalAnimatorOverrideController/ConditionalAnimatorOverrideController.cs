using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Class that allows for an Animator's controller to be overridden with an AnimatorOverrideController
 * which changes the animations when the object it is attached to meets certain conditions.  Override this class
 * to control what the conditions are.
 * See the following link for more info: https://docs.unity3d.com/2018.4/Documentation/Manual/AnimatorOverrideController.html?_ga=2.34165857.197747741.1582248448-945471438.1544752098
 *
 * Original Author: Scott Lepthien
 * Creation Date: 2/21/2020
 */
public abstract class ConditionalAnimatorOverrideController : MonoBehaviour
{
	[Tooltip("The Animator who's animations will be modified by this script if the conditions are met.")]
	[SerializeField] private Animator animator;
	[Tooltip("The AnimatorOverrideController which will be used to override the default animation of the animator.")]
	[SerializeField] private AnimatorOverrideController overrideController;

	// Override this to control if the AnimatorOverrideController is used or not
	protected abstract bool needsToExecuteOnAwake();

	private void Awake()
	{
		if (needsToExecuteOnAwake() && animator != null && overrideController != null)
		{
			animator.runtimeAnimatorController = overrideController;
			animator.Update(0.0f);
		}
	}
}
