using UnityEngine;
using System.Collections;

public class Oz02Hourglass : TICoroutineMonoBehaviour
{
	[SerializeField] private UISprite button;
	[SerializeField] private UILabel label;	// To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent labelWrapperComponent;

	public LabelWrapper labelWrapper
	{
		get
		{
			if (_labelWrapper == null)
			{
				if (labelWrapperComponent != null)
				{
					_labelWrapper = labelWrapperComponent.labelWrapper;
				}
				else
				{
					_labelWrapper = new LabelWrapper(label);
				}
			}
			return _labelWrapper;
		}
	}
	private LabelWrapper _labelWrapper = null;
	
	[SerializeField] private Animator showEffect;
	[SerializeField] private Animator sheenEffect;
	[SerializeField] private Animator glowEffect;
	public bool alreadyChosen
	{
		get { return this._alreadyChosen; }
		private set { this._alreadyChosen = value; }
	}
	private bool _alreadyChosen = false;

	// Constant Variable
	private const float TIME_PICKME_ANIMATION = 1.2f;								// How long the pickme animation takes to play.
	private const float TIME_HOURGLASS_ANIMATION = 3.1f;							// How long the hourglass animation takes to play
	private const float DESIRED_TIME_HOURGLASS_ANIMATION = 3.1f;					// How long you want the hourglass animation to take.
	private const string BAD_CLICK_SPRITE_NAME = "EndsBonus_m";						// The name of the sprite changed to when a bad pick is clicked.
	private readonly Vector3 LOSS_LABEL_POSITION = new Vector3(0, -100, -10);		// The postion to move the label when a bad pick is clicked.
	// Sound names
	private const string CLICK_HOURGLASS = "HG_select_hourglass";					// Name of sound played when an hourglass is selected.
	private const string REVEAL_LOSS_1 = "HG_Reveal_Witch";							// Name of first sound played when a bad pick is chosen.
	private const string REVEAL_LOSS_2 = "ww_laughs";								// Name of second sound played when a bad pick is chosen.
	private const string REVEAL_OTHERS = "reveal_others";							// Sound maped to revealing the picks not chosen.

	public void setup()
	{
		button.gameObject.SetActive(true);
		labelWrapper.gameObject.SetActive(false);
		sheenEffect.gameObject.SetActive(false);
		showEffect.gameObject.SetActive(false);
		glowEffect.gameObject.SetActive(true);		
	}

	// Plays the showEffect, seting the rest of the object into the required state to do so.
	private IEnumerator playShowEffect()
	{
		Audio.play(CLICK_HOURGLASS);
		this.GetComponent<Collider>().enabled = false;
		sheenEffect.gameObject.SetActive(false);
		glowEffect.gameObject.SetActive(false);
		showEffect.gameObject.SetActive(true);
		// Grab the animator so we can speed up the animation to the desired time.
		Animator showEffectAnimator = showEffect.GetComponent<Animator>();
		if (showEffectAnimator != null)
		{
			showEffectAnimator.speed = TIME_HOURGLASS_ANIMATION / DESIRED_TIME_HOURGLASS_ANIMATION;
		}
		yield return new TIWaitForSeconds(DESIRED_TIME_HOURGLASS_ANIMATION);
		showEffect.gameObject.SetActive(false);
	}

	// Changes the sprite and label position so the button looks like a witch.
	private void changeIntoWitch()
	{
		button.spriteName = BAD_CLICK_SPRITE_NAME;
		button.MakePixelPerfect();
		// Move down the label so we don't cover up the witch sprite? Which sprite was that?
		labelWrapper.transform.localPosition = LOSS_LABEL_POSITION;
	}

	public IEnumerator playPickMeAnimation()
	{
		if (!alreadyChosen)
		{
			sheenEffect.gameObject.SetActive(true);
			yield return new TIWaitForSeconds(TIME_PICKME_ANIMATION);
			if (sheenEffect != null)
			{
				sheenEffect.gameObject.SetActive(false);
			}
		}
	}

	public IEnumerator revealWin(long credits)
	{
		alreadyChosen = true;
		button.gameObject.SetActive(false);
		yield return StartCoroutine(playShowEffect());
		labelWrapper.text = CreditsEconomy.convertCredits(credits);
		labelWrapper.gameObject.SetActive(true);
	}
	public IEnumerator revealLoss(long credits)
	{
		alreadyChosen = true;
		button.gameObject.SetActive(false);
		yield return StartCoroutine(playShowEffect());
		button.gameObject.SetActive(true);
		Audio.play(REVEAL_LOSS_1);
		Audio.play(REVEAL_LOSS_2); 
		labelWrapper.text = CreditsEconomy.convertCredits(credits);
		labelWrapper.gameObject.SetActive(true);
		changeIntoWitch();
	}

	public void doDistractor(bool isWitch, long credits)
	{
		if (isWitch)
		{
			changeIntoWitch();
		}
		labelWrapper.text = CreditsEconomy.convertCredits(credits);
		labelWrapper.color = Color.gray;
		labelWrapper.gameObject.SetActive(true);
		button.color = Color.gray;
		Audio.play(REVEAL_OTHERS);
	}

	public void updateWinLabel(long credits)
	{
		labelWrapper.text = CreditsEconomy.convertCredits(credits);
	}

	public void setGlowActive(bool active)
	{
		if (!alreadyChosen)
		{
			glowEffect.gameObject.SetActive(active);
		}
	}
}

