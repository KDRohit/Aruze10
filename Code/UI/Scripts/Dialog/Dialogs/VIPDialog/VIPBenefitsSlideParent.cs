using UnityEngine;
using System.Collections;

public class VIPBenefitsSlideParent : MonoBehaviour
{
	[SerializeField] private VIPBenefitsSlider slider;
	[SerializeField] private SwipeArea swipeArea;
	
	public void moveToVIPTier()
	{
		swipeArea.gameObject.SetActive(true);
		VIPLevel currentLevel = VIPLevel.find(SlotsPlayer.instance.vipNewLevel);
		slider.moveToVIPTier(currentLevel);
	}

	public void disableScrollbar()
	{
		if (slider.scrollBar != null)
		{
			slider.scrollBar.gameObject.SetActive(false);
		}
	}

	public void enableScrollbar()
	{
		if (slider.scrollBar != null)
		{
			slider.scrollBar.gameObject.SetActive(true);
		}
	}
}
