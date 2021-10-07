using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class DynamicMOTDPageController : MonoBehaviour
{
	public GameObject backgroundSprite;
	public ButtonHandler nextButton;
	public ButtonHandler previousButton;
	public UISprite pagePip;

	public UIGrid grid;
	private List<UISprite> pips;
	private const string PIP_ON = "Pagination Pip On Stretchy";
	private const string PIP_OFF = "Pagination Pip Off Stretchy";

	public void setFrameCount(int frames)
	{
		
		pagePip.spriteName = PIP_ON;

		frames -= 1; // We always setup with one pip, so decrement 1

		CommonTransform.setWidth(backgroundSprite.transform, backgroundSprite.transform.localScale.x + (frames * 100));
		               
		if (frames > 0)
		{
			pips = new List<UISprite>();
			pips.Add(pagePip);
		}

		GameObject loadingTarget = null;
		UISprite sprite = null;
		for (int i = 0; i < frames; i++)
		{
			loadingTarget = CommonGameObject.instantiate(pagePip.gameObject, grid.transform) as GameObject;
			sprite = loadingTarget.GetComponentInChildren<UISprite>();
			sprite.spriteName = PIP_OFF;
			pips.Add(sprite);
		}

		grid.repositionNow = true;
	}

	// frames start at "frame_1", so frame number 1 activates pip 0
	public void goToFrame(int frameNumber)
	{
		if (frameNumber >= 0 && pips != null)
		{
			for (int i = 0; i < pips.Count; i++)
			{
				pips[i].spriteName = PIP_OFF;
			}
			pips[frameNumber].spriteName = PIP_ON;
		}
	}

}
