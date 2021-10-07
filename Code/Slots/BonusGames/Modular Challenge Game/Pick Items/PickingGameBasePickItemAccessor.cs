using UnityEngine;
using System.Collections;


/**
Provides unified access to PickingGameBasePickItem for any 'derived' item type
*/
[ExecuteInEditMode]
[RequireComponent (typeof (PickingGameBasePickItem))]
public abstract class PickingGameBasePickItemAccessor : TICoroutineMonoBehaviour
{
	[SerializeField] protected PickingGameBasePickItem basePickItem = null;		// reference to the PickingGameBasePickItem component attached to this object

	protected virtual void Awake()
	{
		if (basePickItem == null)
		{
			basePickItem = gameObject.GetComponent<PickingGameBasePickItem>();
		}
	}

	protected virtual void Update()
	{
		if (!Application.isPlaying)
		{
			if (basePickItem == null)
			{
				basePickItem = gameObject.GetComponent<PickingGameBasePickItem>();
			}
		}
	}

	public GameObject pickButton
	{
		get { return basePickItem.pickButton; }
	}

	public Animator pickAnimator
	{
		get { return basePickItem.pickAnimator; }
	}

	public string soundMappingPrefix
	{
		get { return basePickItem.soundMappingPrefix; }
	}

	// override in derived classes
	public string voMappingPrefix
	{
		get { return basePickItem.voMappingPrefix; }
	}

	public string BAD_REVEAL_SOUND
	{
		get { return basePickItem.BAD_REVEAL_SOUND; }
	}

	public string BAD_REVEAL_VO
	{
		get { return basePickItem.BAD_REVEAL_VO; }
	}

	public string REVEAL_ANIMATION
	{
		get { return basePickItem.REVEAL_ANIMATION; }
		set { basePickItem.REVEAL_ANIMATION = value; }
	}

	public string REVEAL_ANIMATION_GRAY
	{
		get { return basePickItem.REVEAL_ANIMATION_GRAY; }
		set { basePickItem.REVEAL_ANIMATION_GRAY = value; }
	}
	
	public bool isRevealed
	{
		get { return basePickItem.isRevealed; }
	}

	// Initialize the pick item, collider, and linkage
	public virtual void init(GameObject roundTarget)
	{
		if (basePickItem == null)
		{
			basePickItem = gameObject.GetComponent<PickingGameBasePickItem>();
		}

		basePickItem.init(roundTarget);
	}

	// Event handler for the UIButtonMessage sent up from the collider
	public void pickItemPressed(GameObject pickObject)
	{
		basePickItem.pickItemPressed(pickObject);
	}

	// Toggle the buttons clickable state
	public void setClickable(bool isClickable)
	{		
		basePickItem.setClickable(isClickable);		
	}

	// Execute the reveal action for this pick item - need implementation for each type
	public IEnumerator revealPick(ModularChallengeGameOutcomeEntry pick)
	{
		yield return StartCoroutine(basePickItem.revealPick(pick));
	}

	// Reveal leftover items (derived classes will handle normal / grey display
	public IEnumerator revealLeftover(ModularChallengeGameOutcomeEntry leftover)
	{
		yield return StartCoroutine(basePickItem.revealLeftover(leftover));
	}
}
