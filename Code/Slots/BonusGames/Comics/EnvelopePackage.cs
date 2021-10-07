using UnityEngine;
using System.Collections;

/// Needed to separate out into a different script so Unity can pick this up.  
/// The primary purpose of this class is to be able to package a UI Sprite and a text field together
/// and provide some useful functionality.
public class EnvelopePackage : TICoroutineMonoBehaviour
{
	public UILabel winAmount;	// To be removed when prefabs are updated.
	public LabelWrapperComponent winAmountWrapperComponent;

	public LabelWrapper winAmountWrapper
	{
		get
		{
			if (_winAmountWrapper == null)
			{
				if (winAmountWrapperComponent != null)
				{
					_winAmountWrapper = winAmountWrapperComponent.labelWrapper;
				}
				else
				{
					_winAmountWrapper = new LabelWrapper(winAmount);
				}
			}
			return _winAmountWrapper;
		}
	}
	private LabelWrapper _winAmountWrapper = null;
	
	public UILabel endLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent endLabelWrapperComponent;

	public LabelWrapper endLabelWrapper
	{
		get
		{
			if (_endLabelWrapper == null)
			{
				if (endLabelWrapperComponent != null)
				{
					_endLabelWrapper = endLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_endLabelWrapper = new LabelWrapper(endLabel);
				}
			}
			return _endLabelWrapper;
		}
	}
	private LabelWrapper _endLabelWrapper = null;
	
	public UILabel raiseLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent raiseLabelWrapperComponent;

	public LabelWrapper raiseLabelWrapper
	{
		get
		{
			if (_raiseLabelWrapper == null)
			{
				if (raiseLabelWrapperComponent != null)
				{
					_raiseLabelWrapper = raiseLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_raiseLabelWrapper = new LabelWrapper(raiseLabel);
				}
			}
			return _raiseLabelWrapper;
		}
	}
	private LabelWrapper _raiseLabelWrapper = null;
	
	public UISprite envelopeSprite;
	public UISprite shineEffect;
	public GameObject effectCorrector; // Art submitted assets that are positioned incorrectly, so in order to have animations do things correctly, the effects are parented under this.
	public bool enablingSheen;
	public bool isPlayingPickMeAnim = false;

	private const float ENVELOP_SHEEN_ANIM_TIME = 0.75f;
	
	//Start up the oscillating fade effect
	void Awake()
	{
		//iTween.ValueTo(gameObject, iTween.Hash("from", 0.0f, "to", 0.6f, "onupdate", "onUpdateOfSheen", "time", 0.75f, "looptype", "pingPong"));
		enablingSheen = true;
	}
	
	//This function can be used to handle revealing the contents of an envelope, including playing an animation.
	public void revealWinAmount(long valueToReveal)
	{
		shineEffect.gameObject.SetActive(false);
		winAmountWrapper.text = CreditsEconomy.convertCredits(valueToReveal);
		winAmountWrapper.gameObject.SetActive(true);
		StartCoroutine(waitUntilApex());
	}

	public IEnumerator playPickMeAnimation()
	{
		// fade the sheen up
		isPlayingPickMeAnim = true;
		iTween.ValueTo(gameObject, iTween.Hash("from", 0.0f, "to", 0.6f, "onupdate", "onUpdateOfSheen", "time", ENVELOP_SHEEN_ANIM_TIME, "oncomplete", "pickMeAnimComplete", "oncompletetarget", this.gameObject));
		while (isPlayingPickMeAnim)
		{
			yield return null;
		}

		// fade the sheen down
		isPlayingPickMeAnim = true;
		iTween.ValueTo(gameObject, iTween.Hash("from", 0.6f, "to", 0.0f, "onupdate", "onUpdateOfSheen", "time", ENVELOP_SHEEN_ANIM_TIME, "oncomplete", "pickMeAnimComplete", "oncompletetarget", this.gameObject));
		while (isPlayingPickMeAnim)
		{
			yield return null;
		}
	}

	/// Called when the pick me animation finishes, will be called twice per pickme since we pingPong the animation
	private void pickMeAnimComplete()
	{
		isPlayingPickMeAnim = false;
	}
	
	private void onUpdateOfSheen(float newAlpha)
	{
		if(enablingSheen)
		{
			shineEffect.alpha = newAlpha;
		}
		else
		{
			shineEffect.alpha = 0;
		}
	}
	
	private IEnumerator waitUntilApex()
	{
		//Temporarily removing the yield to have the envelope appear because as of right now the paper's material isn't changing, and it overlays the
		//envelope it comes from.  It actually looks pretty good, and the number needs to be displayed properly as soon as the paper appears.
		//yield return new WaitForSeconds(0.5f);
		
//		winAmount.depth = 2;	// Disabled due to removing support for UILabel. Not sure how this game works.
	 							// See Todd if help is needed doing the same effect with TextMeshPro.
		yield return null;
	}
	
	public void stripAnimation()
	{
		Destroy(winAmountWrapper.gameObject.GetComponent<Animation>());
		winAmountWrapper.color = Color.grey;
//		winAmount.depth = 2;	// Disabled due to removing support for UILabel. Not sure how this game works.
	 							// See Todd if help is needed doing the same effect with TextMeshPro.
		winAmountWrapper.transform.localPosition = new Vector3(0, 0, 0);
	}
}

