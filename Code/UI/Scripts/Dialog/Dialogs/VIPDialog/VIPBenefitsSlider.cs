using UnityEngine;
using System.Collections;

public class VIPBenefitsSlider : SlideController
{
	[SerializeField] private VIPRevampTiers vipTiers;

	public static VIPBenefitsSlider instance = null;

	void Awake()
	{
		instance = this;

		// This function gets called as an init. So moving scroll registation here.
		if (scrollBar != null)
		{
			scrollBar.onChange -= scrollBarChanged;
			scrollBar.onChange += scrollBarChanged;
		}
	}
	
	public override void Update()
	{
		if (!Dialog.instance.isShowing || Dialog.instance.currentDialog.type.keyName == "vip_revamp_benefits")
		{
			base.Update();
		}
	}

	public void moveToVIPTier(VIPLevel currentLevel)
	{	
		VIPBenefitsPanel target = null;
		foreach (VIPRevampBenefitsPanel panel in vipTiers.panels)
		{
			if (panel.level == currentLevel)
			{
				target = panel;
				break;
			}
		}

		if (target != null)
		{
			Vector3 pos = target.gameObject.transform.localPosition;
			Vector3 startPos = vipTiers.panels[0].gameObject.transform.localPosition;
			Vector3 diff = pos - startPos;

			AnimateToXOffset(diff.x, 0.5f);
		}
		
	}
}
