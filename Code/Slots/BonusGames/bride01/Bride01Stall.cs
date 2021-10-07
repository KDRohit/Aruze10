using UnityEngine;
using System.Collections;

/**
Class to handle controlling the assets contained within a stall of the brides maid pick game
*/
public class Bride01Stall : TICoroutineMonoBehaviour 
{
	[SerializeField] private UISprite[] characterSprites = null;		// Character sprites, grayed out if this is non-pick reveal
	[SerializeField] private ParticleSystem sparkles = null;			// Sparkle animation used when a pick reveal happens
	[SerializeField] private Animator revealAnim = null;				// Animator for displaying the reveal of a character
	[SerializeField] private GameObject[] starSets = null;				// Collections of stars to show denoting the level of jackpot
	[SerializeField] private UILabel jackpotLabel = null;				// Text to tell that the stars mean jackpot level -  To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent jackpotLabelWrapperComponent = null;				// Text to tell that the stars mean jackpot level

	public LabelWrapper jackpotLabelWrapper
	{
		get
		{
			if (_jackpotLabelWrapper == null)
			{
				if (jackpotLabelWrapperComponent != null)
				{
					_jackpotLabelWrapper = jackpotLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_jackpotLabelWrapper = new LabelWrapper(jackpotLabel);
				}
			}
			return _jackpotLabelWrapper;
		}
	}
	private LabelWrapper _jackpotLabelWrapper = null;
	

	[SerializeField] private bool useAnimations = false;
	[SerializeField] private string pickmeAnimation = null;
	[SerializeField] private string[] revealAnimations = null;
	[SerializeField] private string[] grayRevealAnimations = null;

	private bool _isRevealed = false;					// Flag that tells if this stall has been revealed
	public bool isRevealed
	{
		get { return _isRevealed; }
	}

	private const string PICKME_ANIM_NAME = "bride01_curtainPickme";	// Pick me animation name

	private const float SPARKLE_DELAY_TIME = 0.666f;					// Time delay after reveal animation starts before the sparkle plays, controlling from code so it doesn't show on non-picks
	private const float REVEAL_ANIMATION_LENGTH = 1.0f;					// Time the reveal animation takes
	private const float PICK_ME_ANIM_LENGTH = 0.5f;						// Length of the pick me animation

	/// Gray out the elements
	public void grayOut(UILabelStyle grayedOutRevealStyle)
	{
		foreach (UISprite character in characterSprites)
		{
			character.color = Color.gray;
		}

		UILabelStyler labelStyle = jackpotLabelWrapper.gameObject.GetComponent<UILabelStyler>();
		if (labelStyle != null)
		{
			labelStyle.style = grayedOutRevealStyle;
			labelStyle.updateStyle();
		}
	}

	/// Controls visiblity of the sparkles particles
	public void setSparklesVisible(bool isVisible)
	{
		sparkles.gameObject.SetActive(isVisible);
	}

	public IEnumerator playPickMe()
	{
		if(!useAnimations)
		{
			revealAnim.Play(PICKME_ANIM_NAME);
		}
		else{
			revealAnim.Play(pickmeAnimation);
		}

		yield return new WaitForSeconds(PICK_ME_ANIM_LENGTH);
	}

	public IEnumerator reveal(string animName, bool isPick, long creditAmount,  UILabelStyle grayedOutRevealStyle, int starNumber)
	{
		_isRevealed = true;
		
		if (!useAnimations)
		{
			for (int i = 0; i < starSets.Length; ++i)
			{
				if((starNumber - 1) == i)
				{
					starSets[i].SetActive(true);
				}
				else
				{
					starSets[i].SetActive(false);
				}
			}

			if (!isPick)
			{
				// not the picked stall, so grey it out and hide sparkles
				grayOut(grayedOutRevealStyle);
				setSparklesVisible(false);
			}

			revealAnim.Play(animName);

			if (isPick)
			{
				// wait and then reveal a sparkle if this is the user's pick
				yield return new WaitForSeconds(SPARKLE_DELAY_TIME);

				setSparklesVisible(true);

				yield return new WaitForSeconds(REVEAL_ANIMATION_LENGTH - SPARKLE_DELAY_TIME);
			}
			else
			{
				yield return new WaitForSeconds(REVEAL_ANIMATION_LENGTH);
			}
		}
		else
		{
			if (isPick)
			{
				revealAnim.Play(revealAnimations[starNumber - 1]);
				
				// wait and then reveal a sparkle if this is the user's pick
				yield return new WaitForSeconds(SPARKLE_DELAY_TIME);
				
				setSparklesVisible(true);
				
				yield return new WaitForSeconds(REVEAL_ANIMATION_LENGTH - SPARKLE_DELAY_TIME);
			}
			
			if (!isPick)
			{
				// not the picked stall, so grey it out and hide sparkles
				revealAnim.Play(grayRevealAnimations[starNumber - 1]);
				setSparklesVisible(false);
				yield return new WaitForSeconds(REVEAL_ANIMATION_LENGTH);
			}
		}
	}
}

