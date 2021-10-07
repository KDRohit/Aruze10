using UnityEngine;
using System.Collections;
using System.Collections.Generic;

 
/**
 * This is the picking game item class of the modular PickingGame system.
 */
public enum PickItemType
{
	CREDITS,
	MULTIPLIER,
	EXTRA
}

/**
 * Base Pick Item class with common data fields for all types
 */
public class PickingGameBasePickItem : TICoroutineMonoBehaviour
{
	public GameObject pickButton;
	public Animator pickAnimator;

	public string soundMappingPrefix;
	// override in derived classes
	public string voMappingPrefix;

	public string BAD_REVEAL_SOUND = "pickem_reveal_bad";
	public string BAD_REVEAL_VO = "pickem_reveal_bad_vo";

	public string PICKME_ANIMATION = "pickme_custom";
	[SerializeField] protected AnimationListController.AnimationInformationList PRE_REVEAL_ANIMS;
	[SerializeField] protected AnimationListController.AnimationInformationList POST_REVEAL_ANIMS;
	[Tooltip("If this flag is true (default) the POST_REVEAL_ANIMS will be played at the end of the revealPick() function.  If it is false, then they can be played manually using playPostRevealAnims().")]
	[SerializeField] protected bool _isPlayingPostRevalAnimsImmediatelyAfterReveal = true;
	public bool isPlayingPostRevalAnimsImmediatelyAfterReveal
	{
		get { return _isPlayingPostRevalAnimsImmediatelyAfterReveal; }
	}
	
	[HideInInspector] public string REVEAL_ANIMATION;
	[HideInInspector] public float REVEAL_ANIM_OVERRIDE_DUR = -1.0f;
	[HideInInspector] public string REVEAL_ANIMATION_GRAY;
	
	public bool isRevealed = false;

	public UIButtonMessage buttonMessage;
	[SerializeField] private bool isAutoCreatingButtonMessage = true;
	public Collider buttonCollider;

	private GameObject roundTarget;
	private bool didInit = false;

	// Initialize the pick item, collider, and linkage
	public virtual void init(GameObject roundTarget)
	{
		if (!didInit)
		{
			this.roundTarget = roundTarget;

			//If our pickbutton isn't set use this object
			if (pickButton == null)
			{
				pickButton = this.gameObject;
			}

			//Check for an animator class
			if (pickAnimator == null)
			{
				pickAnimator = (Animator)CommonGameObject.getComponent<Animator>(pickButton);
			}

			// grab or create the button message
			if (buttonMessage == null && isAutoCreatingButtonMessage)
			{
				//Check for a ui button message
				buttonMessage = (UIButtonMessage)CommonGameObject.getComponent<UIButtonMessage>(pickButton, true);
				buttonMessage.functionName = "pickItemPressed"; // This will need to need to be check as to not overwrite the already entered version

				//This needs a target if it hasn't been set	
				buttonMessage.target = roundTarget;
			}


			//Check for a box collider - first if one is set manually, second if one is set on the current object
			if (buttonCollider == null)
			{
				buttonCollider = CommonGameObject.getComponent<BoxCollider>(pickButton);
				if (buttonCollider == null)
				{
					//If we don't have a colider add one.
					BoxCollider collider = pickButton.AddComponent<BoxCollider>();

					//If the object that got a colider doesn't have a renderer Unity won't size it correctly
					if (pickButton.GetComponent<Renderer>() == null)
					{
						//Fix the the box colliders center and size to match the pickButtons children	
						Bounds bounds = CommonGameObject.getObjectBounds(pickButton);
						collider.size = gameObject.transform.InverseTransformVector(bounds.size);
						collider.center = gameObject.transform.InverseTransformPoint(bounds.center);
					}

					buttonCollider = collider;
				}
			}

			didInit = true;
		}

		// make sure if this gets initialized again it can be clicked, i.e. if a game is reused without being recreated
		isRevealed = false;

		// ensure the box collider is enabled
		if (buttonCollider != null)
		{
			buttonCollider.enabled = true;
		}
	}

	// Event handler for the UIButtonMessage sent up from the collider
	public void pickItemPressed(GameObject pickObject)
	{
		// pass on the message, but substitute the *actual* PickItem object
		roundTarget.SendMessage("pickItemPressed", gameObject);
	}

	// Toggle the buttons clickable state
	public void setClickable(bool isClickable)
	{
		buttonCollider.enabled = isClickable;
	}

	public virtual void setRevealAnim(string animName, float overrideDur = -1.0f)
	{
		REVEAL_ANIMATION = animName;
		REVEAL_ANIM_OVERRIDE_DUR = overrideDur;		
	}

	// Execute the reveal action for this pick item - need implementation for each type
	public virtual IEnumerator revealPick(ModularChallengeGameOutcomeEntry pick)
	{
		// disable this item
		setClickable(false);
		isRevealed = true;

		// Play pre-reveal animations
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(PRE_REVEAL_ANIMS));

		// Play reveal
		if (REVEAL_ANIM_OVERRIDE_DUR >= 0.0f)
		{
			pickAnimator.Play(REVEAL_ANIMATION);

			if (REVEAL_ANIM_OVERRIDE_DUR > 0.0f)
			{
				yield return new WaitForSeconds(REVEAL_ANIM_OVERRIDE_DUR);
			}
		}
		else
		{
			yield return StartCoroutine(
				CommonAnimation.playAnimAndWait(pickAnimator, REVEAL_ANIMATION));
		}

		// Play post-reveal animations
		if (_isPlayingPostRevalAnimsImmediatelyAfterReveal && hasPostRevealAnims())
		{
			yield return StartCoroutine(playPostRevealAnims());
		}
	}

	public bool hasPostRevealAnims()
	{
		return POST_REVEAL_ANIMS.Count > 0;
	}

	// Function to play the post reveal animations, these will either be played automatically as part of revealPick()
	// or must be played by some module if isPlayingPostRevalAnimsImmediatelyAfterReveal is false
	public IEnumerator playPostRevealAnims()
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(POST_REVEAL_ANIMS));
	}

	// Play through an animation list in parallel or sequence
	private IEnumerator playAnimList(Animator target, string[] animNames, bool sequence)
	{
		if (animNames.Length > 0)
		{
			foreach (string anim in animNames)
			{
				target.Play(anim);
				if (sequence)
				{
					yield return StartCoroutine(CommonAnimation.waitForAnimDur(target));
				}
			}
		}
	}

	// Reveal leftover items (derived classes will handle normal / grey display
	public virtual IEnumerator revealLeftover(ModularChallengeGameOutcomeEntry leftover)
	{
		setClickable(false);
		yield return StartCoroutine(CommonAnimation.playAnimAndWait(pickAnimator, REVEAL_ANIMATION_GRAY));
	}
	
	public void clearPostRevealAnimations()
	{
		POST_REVEAL_ANIMS.animInfoList.Clear();
	}
}
