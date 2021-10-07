using UnityEngine;
using System.Collections;

public class Bride01Dress : MonoBehaviour 
{
	[SerializeField] private UISprite[] revealSprites = null;			// Sprites that are part of reveals, grayed out if this is non-pick reveal
	[SerializeField] private UILabel creditText = null;					// Text to fill in the jackpot credit amount -  To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent creditTextWrapperComponent = null;					// Text to fill in the jackpot credit amount

	public LabelWrapper creditTextWrapper
	{
		get
		{
			if (_creditTextWrapper == null)
			{
				if (creditTextWrapperComponent != null)
				{
					_creditTextWrapper = creditTextWrapperComponent.labelWrapper;
				}
				else
				{
					_creditTextWrapper = new LabelWrapper(creditText);
				}
			}
			return _creditTextWrapper;
		}
	}
	private LabelWrapper _creditTextWrapper = null;
	
	[SerializeField] private ParticleSystem sparkles = null;			// Sparkle animation used when a pick reveal happens
	[SerializeField] private Animator revealAnim = null;				// Animator for displaying the reveal of a character
	[SerializeField] private GameObject iconSparkleObj = null;			// GameObject that contains animation stuff for revealing the jackpot icon

	[SerializeField] private bool useAnimations = false;
	[SerializeField] private string pickmeAnimation = null;
	[SerializeField] private string revealJackpotAnimation = null;
	[SerializeField] private string grayRevealJackpotAnimation = null;
	[SerializeField] private string revealSaveAnimation = null;
	[SerializeField] private string grayRevealSaveAnimation = null;
	[SerializeField] private string revealCreditAnimation = null;
	[SerializeField] private string grayRevealCreditAnimation = null;
	[SerializeField] private GameObject[] jackpotIcons = null;

	private bool _isRevealed = false;									// Flag that tells if this stall has been revealed
	public bool isRevealed
	{
		get { return _isRevealed; }
	}

	private const string PICKME_ANIM_NAME = "bride01_curtainPickme";	// Pick me animation name

	private const float SPARKLE_DELAY_TIME = 1.016f;					// Time delay after reveal animation starts before the sparkle plays, controlling from code so it doesn't show on non-picks
	private const float REVEAL_ANIMATION_LENGTH = 1.333f;				// Time the reveal animation takes
	private const float PICK_ME_ANIM_LENGTH = 0.5f;						// Length of the pick me animation

	/// Gray out the elements
	public void grayOut(UILabelStyle grayedOutRevealStyle)
	{
		foreach (UISprite character in revealSprites)
		{
			character.color = Color.gray;
		}

		UILabelStyler labelStyle = creditTextWrapper.gameObject.GetComponent<UILabelStyler>();
		if (labelStyle != null)
		{
			labelStyle.style = grayedOutRevealStyle;
			labelStyle.updateStyle();
		}
	}

	/// Set the credits text
	public void setCreditText(long credits)
	{
		creditTextWrapper.text = CreditsEconomy.convertCredits(credits);
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

	public IEnumerator reveal(string animName, bool isPick, long creditAmount, UILabelStyle grayedOutRevealStyle, bool isPlayingIconReveal, string outcomeType = null, int starNumber = 0)
	{
		_isRevealed = true;

		setCreditText(creditAmount);

		if (!useAnimations)
		{
			if (!isPick)
			{
				// not the picked stall, so grey it out and hide sparkles
				grayOut(grayedOutRevealStyle);
				setSparklesVisible(false);
			}

			revealAnim.Play(animName);

			if (isPick)
			{
				// enable dress sparkles right away
				setSparklesVisible(true);

				yield return new WaitForSeconds(SPARKLE_DELAY_TIME);
			}
			else
			{
				yield return new WaitForSeconds(REVEAL_ANIMATION_LENGTH);
			}

			if (isPlayingIconReveal)
			{
				iconSparkleObj.SetActive(true);
			}
		}
		else
		{
			if(outcomeType != null)
			{
				if(outcomeType == "JACKPOT")
				{
					if (isPick)
					{
						jackpotIcons[starNumber].SetActive(true);
						revealAnim.Play(revealJackpotAnimation);
						
						// wait and then reveal a sparkle if this is the user's pick
						yield return new WaitForSeconds(SPARKLE_DELAY_TIME);
						
						setSparklesVisible(true);
						
						yield return new WaitForSeconds(REVEAL_ANIMATION_LENGTH - SPARKLE_DELAY_TIME);
					}
					
					if (!isPick)
					{
						// not the picked stall, so grey it out and hide sparkles
						revealAnim.Play(grayRevealJackpotAnimation);
						setSparklesVisible(false);
						yield return new WaitForSeconds(REVEAL_ANIMATION_LENGTH);
					}
				}
				else if (outcomeType == "SAVE")
				{
					if (isPick)
					{
						revealAnim.Play(revealSaveAnimation);
					}
					else
					{
						revealAnim.Play(grayRevealSaveAnimation);
					}
					yield return new WaitForSeconds(REVEAL_ANIMATION_LENGTH);
				}
				else
				{
					if (isPick)
					{
						revealAnim.Play(revealCreditAnimation);
					}
					else
					{
						revealAnim.Play(grayRevealCreditAnimation);
					}
					yield return new WaitForSeconds(REVEAL_ANIMATION_LENGTH);
				}
			}

		}
	}
}

