using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class VIPNewIcon : TICoroutineMonoBehaviour
{
	public enum PurchasePercentLabelFormat
	{
		PLUS,
		PLUS_CREDITS,
		PLUS_MORE_CREDITS
	}
	
	public UISprite levelNameSprite; // Sprite version of VIP level text.
	public UILabel levelNameLabel;   // Label version of VIP level text. -  To be removed when prefabs are updated.
	public LabelWrapperComponent levelNameLabelWrapperComponent;   // Label version of VIP level text.

	public LabelWrapper levelNameLabelWrapper
	{
		get
		{
			if (_levelNameLabelWrapper == null)
			{
				if (levelNameLabelWrapperComponent != null)
				{
					_levelNameLabelWrapper = levelNameLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_levelNameLabelWrapper = new LabelWrapper(levelNameLabel);
				}
			}
			return _levelNameLabelWrapper;
		}
	}
	private LabelWrapper _levelNameLabelWrapper = null;
	
	public TextMeshPro levelNameTMPro; // TextMeshPro version of the Label version of VIP level text.
	public UISprite levelIcon;       // VIP level icon.
	public UISprite gemGlow;		// A sprite that goes behind gems and changes color based on level.
	public TextMeshPro purchasePercentLabel; // Label showing how much bonus percent 
	public bool isSmall = false;     // Use the small version of the texture.
	public bool isCard = true;       // Use the clipped version of the texture for the card (false to use the unclipped full gem).
	public bool isCaps = true; // Whether to use all caps or title case for the vip level name label.
	public PurchasePercentLabelFormat purchasePercentLabelFormat = PurchasePercentLabelFormat.PLUS;
	
	public virtual void setLevel(int level)
	{
		setLevel(VIPLevel.find(level));
	}
	
	public virtual void setLevel(VIPLevel vipLevel)
	{
		string small = isSmall ? " Small" : "";
		
		if (vipLevel != null)
		{
			if (levelNameSprite != null)
			{
				levelNameSprite.spriteName = string.Format("VIP Name{0} {1}", small, vipLevel.levelNumber);
				levelNameSprite.MakePixelPerfect();
			}
			
			if (levelNameLabelWrapper != null)
			{
				if (isCaps)
				{
					levelNameLabelWrapper.text = Localize.toUpper(vipLevel.name);					
				}
				else
				{
					levelNameLabelWrapper.text = Localize.toTitle(vipLevel.name);
				}
			}
			
			if (levelNameTMPro != null)
			{
				if (isCaps)
				{
				    levelNameTMPro.text = Localize.toUpper(vipLevel.name);					
				}
				else
				{
				    levelNameTMPro.text = Localize.toTitle(vipLevel.name);
				}
			}
			if (levelIcon != null)
			{
				if (isCard)
				{
					levelIcon.spriteName = string.Format("VIP Card Icon{0} {1}", small, vipLevel.levelNumber);
				}
				else
				{
					levelIcon.spriteName = string.Format("VIP Icon {0}", vipLevel.levelNumber);
				}
				
				// GK-This is causing SD assets to appear small rather than matching the prefab's size
				//levelIcon.MakePixelPerfect();
			}
			if (purchasePercentLabel != null)
			{
				if (vipLevel.purchaseBonusPct > 0)
				{
					// If the bonus is greater than 0, show it.
					string locKey = "";
					switch (purchasePercentLabelFormat)
					{
						case PurchasePercentLabelFormat.PLUS:
							locKey = "plus_{0}_percent";
							break;
						
						case PurchasePercentLabelFormat.PLUS_CREDITS:
							locKey = "plus_{0}_percent_coins";
							break;
						
						case PurchasePercentLabelFormat.PLUS_MORE_CREDITS:
							locKey = "plus_{0}_percent_more_coins";
							break;
					}

					purchasePercentLabel.text = Localize.textUpper(locKey, CommonText.formatNumber(vipLevel.purchaseBonusPct));

					purchasePercentLabel.gameObject.SetActive(true);
				}
				else
				{
					purchasePercentLabel.gameObject.SetActive(false);
				}
			}

			if (gemGlow != null)
			{
				float alpha = gemGlow.color.a;
				Color levelColor = Color.clear;
				switch(vipLevel.levelNumber)
				{
					case 0:
						levelColor = CommonColor.colorFromHex("2355D4");
						break;
					case 1:
						levelColor = CommonColor.colorFromHex("129429");
						break;
					case 2:
						levelColor = CommonColor.colorFromHex("B37F2E");
						break;
					case 3:
						levelColor = CommonColor.colorFromHex("767676");			
						break;
					case 4:
						levelColor = CommonColor.colorFromHex("BF1C42");	
						break;
					case 5:
						levelColor = CommonColor.colorFromHex("75718B");
						break;
					case 6:
						levelColor = CommonColor.colorFromHex("395365");
						break;
					case 7:
						levelColor = CommonColor.colorFromHex("A67500");
						break;
					case 8:
						levelColor = CommonColor.colorFromHex("167FA6");
						break;
					default:
						break;
				}
				levelColor.a = alpha;
				gemGlow.color = levelColor;
			}
			
		}
	}
}

