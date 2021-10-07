using UnityEngine;
using System.Collections;

/**
Class to handle controlling the assets contained within a tray of the brides maid pick game
*/
public class Bride01Tray : TICoroutineMonoBehaviour 
{
	[SerializeField] private UISprite[] revealSprites = null;		// Sprites that can be revealed, need access to them so they can be grayed out
	[SerializeField] private UILabel endText = null;				// Text to fill in the jackpot credit amount -  To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent endTextWrapperComponent = null;				// Text to fill in the jackpot credit amount

	public LabelWrapper endTextWrapper
	{
		get
		{
			if (_endTextWrapper == null)
			{
				if (endTextWrapperComponent != null)
				{
					_endTextWrapper = endTextWrapperComponent.labelWrapper;
				}
				else
				{
					_endTextWrapper = new LabelWrapper(endText);
				}
			}
			return _endTextWrapper;
		}
	}
	private LabelWrapper _endTextWrapper = null;
	
	private UILabelStyle oldLabelStyle = null;						// Store the old label style so we can restore it if need be

	
	[SerializeField] private GameObject round3GameOverRevealObject = null;
	[SerializeField] private GameObject round3ContinueRevealObject = null;

	/// Gray out the elements
	public void grayOut(UILabelStyle grayedOutRevealStyle)
	{
		foreach (UISprite character in revealSprites)
		{
			character.color = Color.gray;
		}

		UILabelStyler labelStyle = endTextWrapper.gameObject.GetComponent<UILabelStyler>();
		if (labelStyle != null)
		{
			oldLabelStyle = labelStyle.style;
			labelStyle.style = grayedOutRevealStyle;
			labelStyle.updateStyle();
		}
	}

	/// Reverse the gray out process
	public void unGrayOut()
	{
		foreach (UISprite character in revealSprites)
		{
			character.color = Color.white;
		}

		UILabelStyler labelStyle = endTextWrapper.gameObject.GetComponent<UILabelStyler>();
		if (labelStyle != null && oldLabelStyle != null)
		{
			labelStyle.style = oldLabelStyle;
			labelStyle.updateStyle();
		}
	}

	public void hideObjectsForOutcome(string outcomeType)
	{
		if(outcomeType == "GAMEOVER")
		{
			if(round3ContinueRevealObject != null && round3GameOverRevealObject != null)
			{
				round3ContinueRevealObject.SetActive(false);
				round3GameOverRevealObject.SetActive(true);
			}
		}
		else
		{
			if(round3GameOverRevealObject != null && round3ContinueRevealObject != null)
			{
				round3GameOverRevealObject.SetActive(false);
				round3ContinueRevealObject.SetActive(true);
			}
		}
	}
}

