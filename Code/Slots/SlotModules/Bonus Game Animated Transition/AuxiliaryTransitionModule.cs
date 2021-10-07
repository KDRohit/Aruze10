using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// This works in parallel with the other transition modules allows an extra unconnected animation to be
/// played at the same time.  You need to put this above the other transitions because they might block
/// aka yield return ...executeOnPreBonusGameCreated();
/// </summary>
public class AuxiliaryTransitionModule : BaseTransitionModule
{
	[Tooltip("Object of Prefab to animate.")]
	[SerializeField] private GameObject auxiliaryObject = null;

	[Tooltip("Set this to true if you are using a Prefab")]
	[SerializeField] private bool doInstantiate = false;

	[Tooltip("In case the animation needs to be nudged into position.")]
	[SerializeField] private Vector3 AUXILIARY_OBJECT_OFFSET = Vector3.zero;

	[Tooltip("Wait time before starting the animation, helps with sequencing.")]
	[SerializeField] private float AnimDelay = 0.0f;

	[SerializeField] private string animName = "";
	[SerializeField] private string idleAnimName = "";
	
	private GameObject internalAuxiliaryObject = null;
	private bool doCleanup = false;

	/// <summary>
	/// The Coroutine that is called through the SlotModule
	/// to preforms the transition.  It does a non-blocking call to
	/// doAuxiliaryTransition().
	/// </summary>
	/// <returns>Coroutine hook</returns>
	public override IEnumerator executeOnPreBonusGameCreated()
	{
		isTransitionStarted = true;
		StartCoroutine(doAuxiliaryTransition());

		yield return null;
	}

	/// <summary>
	/// The Coroutine that preforms the transition.
	/// </summary>
	/// <returns>The auxiliary transition.</returns>
	protected virtual IEnumerator doAuxiliaryTransition()
	{
		// Pause for a delay first.
		yield return new TIWaitForSeconds(AnimDelay);

		// How do we treat the auxiliaryObject given to us?
		if (doInstantiate)
		{
			// as a prefab
			internalAuxiliaryObject = CommonGameObject.instantiate(auxiliaryObject) as GameObject;
			internalAuxiliaryObject.transform.parent = transform;
		}
		else
		{
			// as a GameObject in the scene
			internalAuxiliaryObject = auxiliaryObject;
		}
		
		internalAuxiliaryObject.transform.localPosition = AUXILIARY_OBJECT_OFFSET;
		internalAuxiliaryObject.SetActive(true);
		doCleanup = true;		
		
		// Grab the animator
		Animator anim = internalAuxiliaryObject.GetComponent<Animator>();
		if (anim != null)
		{
			// Play the transition anim
			yield return StartCoroutine(CommonAnimation.playAnimAndWait(anim, animName));
			
			// Put the anim in idle mode
			anim.Play(idleAnimName);
		}
		else
		{
			Debug.LogError(string.Format("The GameObject [{0}]'s AuxiliaryTransitionAnimation expects an Animator on the auxiliaryObject.", gameObject.name));
		}
	}

	/// <summary>
	/// Used to figure out if a clean-up is needed
	/// </summary>
	/// <returns><c>true</c>, to clean up, <c>false</c> otherwise.</returns>
	public override bool needsToExecuteOnBonusGameEnded()
	{
		return doCleanup;
	}

	/// <summary>
	/// Cleans-up the objects we animated.
	/// </summary>
	/// <returns>Coroutine hooks</returns>
	public override IEnumerator executeOnBonusGameEnded()
	{
		if (doInstantiate)
		{
			Destroy(internalAuxiliaryObject);
		}
		else
		{
			internalAuxiliaryObject.SetActive(false);
		}
		
		doCleanup = false;
		
		yield return null;
	}
}