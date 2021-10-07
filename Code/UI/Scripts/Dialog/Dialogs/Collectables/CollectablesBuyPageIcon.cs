using UnityEngine;
using System.Collections;

public class CollectablesBuyPageIcon : MonoBehaviour 
{
	[SerializeField] private LabelWrapperComponent minCardsLabel;
	[SerializeField] private LabelWrapperComponent headerLabel;
	[SerializeField] private GameObject[] stars;
	[SerializeField] private UISprite packSprite;
	[SerializeField] private GameObject purpleBackground;
	[SerializeField] private GameObject blueCardEventBackground;

	public void init(PackConstraint packInfo, string headerLocKey, bool isCollectibleCardEventActive = false)
	{
		int packColorIndex = -1;
		if (packInfo != null)
		{
			if (minCardsLabel != null)
			{
				minCardsLabel.text = Localize.text("min_{0}_of", packInfo.guaranteedPicks);
			}

			if (packSprite != null)
			{
				packSprite.spriteName = "Pack " + packInfo.minRarity;
			}

			packColorIndex = packInfo.minRarity-1;
		}

		if (headerLabel != null)
		{
			headerLabel.text = headerLocKey == null ? "" : Localize.text(headerLocKey);
		}

		if (packColorIndex >= 0 && stars !=null && packColorIndex < stars.Length && stars[packColorIndex] != null)
		{
			stars[packColorIndex].SetActive(true);
		}

		if (purpleBackground != null)
		{
			purpleBackground.SetActive(!isCollectibleCardEventActive);
		}
		if (blueCardEventBackground != null)
		{
			blueCardEventBackground.SetActive(isCollectibleCardEventActive);
		}
	}
}
