using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BannerEnum = SlotBaseGame.BannerInfo.BannerTypeEnum;
using TextDirectionEnum =  SlotBaseGame.BannerTextInfo.TextDirectionEnum;
using TextLocationEnum =  SlotBaseGame.BannerTextInfo.TextLocationEnum;

public class Grumpy01ClickMePortalScript : PortalScript
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
			
			banner.transform.localScale = new Vector3(
				banner.transform.localScale.x * bannerInfo.bannerScaleAdjustment.x,
				banner.transform.localScale.y * bannerInfo.bannerScaleAdjustment.y,
				banner.transform.localScale.z * bannerInfo.bannerScaleAdjustment.z
			);
			
			banner.transform.position = new Vector3(
				root.transform.position.x + bannerInfo.bannerPosAdjustment.x,
				root.transform.position.y + bannerInfo.bannerPosAdjustment.y,
				root.transform.position.z + bannerInfo.bannerPosAdjustment.z
			);
			
			banner.transform.localRotation = Quaternion.identity;
			banner.GetComponent<Animator>().Play("pickme");
		}
		else
		{
			Debug.LogError("The Clickme banner wasn't defined.");
		}
	}
}