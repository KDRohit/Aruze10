using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MagicCauldronButton : TICoroutineMonoBehaviour 
{
	public UILabel amountText = null;			// Text for the amount recieved -  To be removed when prefabs are updated.
	public LabelWrapperComponent amountTextWrapperComponent = null;			// Text for the amount recieved

	public LabelWrapper amountTextWrapper
	{
		get
		{
			if (_amountTextWrapper == null)
			{
				if (amountTextWrapperComponent != null)
				{
					_amountTextWrapper = amountTextWrapperComponent.labelWrapper;
				}
				else
				{
					_amountTextWrapper = new LabelWrapper(amountText);
				}
			}
			return _amountTextWrapper;
		}
	}
	private LabelWrapper _amountTextWrapper = null;
	
	public UISprite ingredientSprite = null;	// Sprite for the ingredient, randomly set during reveal, GameObject is also passed out to be motion tweened to cauldron

	[SerializeField] private GameObject colliderObj = null;				// Object that contains the collider that triggers presses for this button
	[SerializeField] private GameObject bookObj = null;					// Object for the book visual
	[SerializeField] private UISpriteAnimator revealAnimator = null;	// Animator played when this button is revealed
	[SerializeField] private UISprite backgroundSprite = null;			// Sprite for the background image that goes behind the book and reveals
	[SerializeField] private UISprite catSprite = null;					// Icon for the cat which ends the game
	[SerializeField] private Animation pickMeAnim = null;				// Animation for the pick me
	
	private static readonly string[] INGREDIENT_SPRITE_NAMES = {"pickBottle_m", "pickChalice_m", "pickWing_m"};

	/// Tells if this button has been revealed
	public bool isRevealed
	{
		get { return !colliderObj.activeSelf; }
	}

	/// Hide the book
	public void hideBook()
	{
		bookObj.SetActive(false);
	}

	/// Disable the button collider
	public void disableCollider()
	{
		colliderObj.SetActive(false);
	}

	/// Shows the ingredient
	public void showRandomIngredient()
	{
		ingredientSprite.spriteName = INGREDIENT_SPRITE_NAMES[Random.Range(0, 3)];
		ingredientSprite.MakePixelPerfect();
		ingredientSprite.gameObject.SetActive(true);
	}

	/// Reveals an unpicked value that is greyed out
	public void revealValue(long credits, bool isPick)
	{
		amountTextWrapper.text = CreditsEconomy.convertCredits(credits);
		amountTextWrapper.gameObject.SetActive(true);

		if (!isPick)
		{
			backgroundSprite.color = Color.gray;
			amountTextWrapper.color = Color.gray;
		}
	}

	/// Reveals an unpicked multiplier increase that is greyed out
	public void revealMultiplier(bool isPick)
	{
		amountTextWrapper.text = Localize.text("{0}X", 1);
		amountTextWrapper.gameObject.SetActive(true);

		if (!isPick)
		{
			backgroundSprite.color = Color.gray;
			amountTextWrapper.color = Color.gray;
		}
	}

	/// Reveals an unpicked cat that is greyed out
	public void revealCat(bool isPick)
	{
		catSprite.gameObject.SetActive(true);

		if (!isPick)
		{
			backgroundSprite.color = Color.gray;
			catSprite.color = Color.gray;
		}
	}

	/// Play the reveal animator
	public IEnumerator playReveal()
	{
		revealAnimator.gameObject.SetActive(true);
		yield return StartCoroutine(revealAnimator.play());
		revealAnimator.gameObject.SetActive(false);
	}

	/// Play the pick me animation
	public IEnumerator playPickMeAnimation()
	{
		pickMeAnim.Play();

		// wait for the animation to end
		while (pickMeAnim.isPlaying)
		{
			yield return null;
		}
	}
}

