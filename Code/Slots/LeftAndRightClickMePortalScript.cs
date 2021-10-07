using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BannerEnum = SlotBaseGame.BannerInfo.BannerTypeEnum;
using TextDirectionEnum =  SlotBaseGame.BannerTextInfo.TextDirectionEnum;
using TextLocationEnum =  SlotBaseGame.BannerTextInfo.TextLocationEnum;

public class LeftAndRightClickMePortalScript : PortalScript
{

	[SerializeField] private SlotBaseGame.BannerInfo leftBannerInfo;
	[SerializeField] private SlotBaseGame.BannerInfo rightBannerInfo;

	protected override void setupClickMeBanners()
	{
		if (_bannerRoots.Length >= 2)
		{
			GameObject leftRoot = _bannerRoots[0];
			GameObject rightRoot = _bannerRoots[_bannerRoots.Length - 1];

			setupClickMeBanner(leftRoot, leftBannerInfo);
			setupClickMeBanner(rightRoot, rightBannerInfo);
		}
		else
		{
			Debug.LogError("There are not enough banner roots set to have a left and right banner.");
		}
	}

	private void setupClickMeBanner(GameObject root, SlotBaseGame.BannerInfo bannerInfo)
	{
		// Null checking.
		if (root == null)
		{
			Debug.LogError("Trying to attach a banner to a root that doesn't exist.");
			return;
		}
		if (bannerInfo == null)
		{
			Debug.LogError("No info defined for banner.");
			return;
		}

		// Set up the clickMe banner.
		GameObject banner = CommonGameObject.instantiate(bannerInfo.template) as GameObject;
		if (banner != null)
		{
			bannerObjects.Add(banner);
			banner.transform.parent = root.transform;
			banner.transform.localScale = Vector3.one;
			banner.transform.localPosition = bannerAdjustment + bannerInfo.bannerPosAdjustment;
			banner.transform.localRotation = Quaternion.identity;
		}
		else
		{
			Debug.LogError("The Clickme banner wasn't defined.");
		}
	}

}