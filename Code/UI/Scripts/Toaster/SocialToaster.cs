using UnityEngine;
using System.Collections;
using TMPro;

/*
Class Name: SocialToaster.cs
Author: Michael Christensen-Calvin <mchristensencalvin@zynga.com>
Description: A subclass of Toaster specifically dealing with toaster that show a user's face and/or name.
*/

public class SocialToaster : Toaster
{
	public FacebookFriendInfo friendInfo;
	private SocialMember member;

	public override void init(ProtoToaster proto)
	{
		// MCC --  NOT calling base.init() since we want to wait before showing.
		gameObject.SetActive(false); // Make sure we don't show it until we are ready.
		member = proto.member;
		if (member == null || !member.isValid)
		{
			// Just call the image set if there is no member to use.
			onImageSet(true);
		}
		else
		{
			if (friendInfo != null)
			{
				friendInfo.onImageSet += onImageSet;
				friendInfo.member = member;
			}
			else
			{
				Debug.LogErrorFormat("SocialToaster.cs -- init -- no friendInfo setup, this Toaster should always have one of those.");
			}
		}	
	}

	protected virtual void onImageSet(bool didSucceed)
	{
		if (this != null)
		{
			// Now trigger the intro Animation since we have downloaded the textures.
			gameObject.SetActive(true);
			introAnimation();
		}
	}
}