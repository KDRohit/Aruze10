using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Helper function for handling the animated buttons in the osa06 wheel game's pickem stage
*/
public class Osa06WheelPickCharacterItem : TICoroutineMonoBehaviour 
{
	[SerializeField] private GameObject[] characterObjects = new GameObject[5];
	[SerializeField] private Animator[] objectAnimators = new Animator[5];
	[SerializeField] private UILabel[] revealNumberText = new UILabel[5];	// To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent[] revealNumberTextWrapperComponent = new LabelWrapperComponent[5];

	public List<LabelWrapper> revealNumberTextWrapper
	{
		get
		{
			if (_revealNumberTextWrapper == null)
			{
				_revealNumberTextWrapper = new List<LabelWrapper>();

				if (revealNumberTextWrapperComponent != null && revealNumberTextWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in revealNumberTextWrapperComponent)
					{
						if (wrapperComponent != null)
						{
							_revealNumberTextWrapper.Add(wrapperComponent.labelWrapper);
						}
					}
				}
				
				if (revealNumberText != null && revealNumberText.Length > 0)
				{
					foreach (UILabel label in revealNumberText)
					{
						if (label != null)
						{
							_revealNumberTextWrapper.Add(new LabelWrapper(label));
						}
					}
				}
			}
			return _revealNumberTextWrapper;
		}
	}
	private List<LabelWrapper> _revealNumberTextWrapper = null;	
	
	[SerializeField] private UILabel[] notSelectedNumberText = new UILabel[5];	// To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent[] notSelectedNumberTextWrapperComponent = new LabelWrapperComponent[5];

	public List<LabelWrapper> notSelectedNumberTextWrapper
	{
		get
		{
			if (_notSelectedNumberTextWrapper == null)
			{
				_notSelectedNumberTextWrapper = new List<LabelWrapper>();

				if (notSelectedNumberTextWrapperComponent != null && notSelectedNumberTextWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in notSelectedNumberTextWrapperComponent)
					{
						if (wrapperComponent != null)
						{
							_notSelectedNumberTextWrapper.Add(wrapperComponent.labelWrapper);
						}
					}
				}
				
				if (notSelectedNumberText != null && notSelectedNumberText.Length > 0)
				{
					foreach (UILabel label in notSelectedNumberText)
					{
						if (label != null)
						{
							_notSelectedNumberTextWrapper.Add(new LabelWrapper(label));
						}
					}
				}
			}
			return _notSelectedNumberTextWrapper;
		}
	}
	private List<LabelWrapper> _notSelectedNumberTextWrapper = null;	
	

	private Osa06WheelPickingGame.Osa06WheelCharacterEnum currentCharacter = Osa06WheelPickingGame.Osa06WheelCharacterEnum.None;

	public enum Osa06CharacterItemAnimEnum
	{
		Idle = 0,
		PickMe,
		RevealIcon,
		RevealNumber,
		NotSelectedIcon,
		NotSelectedNumber
	}

	private readonly string[] ANIMATION_NAMES = new string[] { "idle", "pickme", "reveal_icon", "reveal_number", "not_selected_icon", "not_selected_number" };

	/// Control which character item is showing
	public void setCharacterObject(Osa06WheelPickingGame.Osa06WheelCharacterEnum character)
	{
		currentCharacter = character;

		for (int i = 0; i < characterObjects.Length; i++)
		{
			if (i == (int)currentCharacter)
			{
				characterObjects[i].SetActive(true);
			}
			else
			{
				characterObjects[i].SetActive(false);
			}
		}
	}

	/// Play an animation and set the number text, which could be shown depending on the animation you're playing
	public void playAnimation(Osa06CharacterItemAnimEnum anim, long creditValue = 0)
	{
		int characterIndex = (int)currentCharacter;

		objectAnimators[characterIndex].Play(ANIMATION_NAMES[(int)anim]);

		revealNumberTextWrapper[characterIndex].text = CreditsEconomy.convertCredits(creditValue);
		notSelectedNumberTextWrapper[characterIndex].text = CreditsEconomy.convertCredits(creditValue);
	}
}

