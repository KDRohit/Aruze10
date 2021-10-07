using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;

public class PhotoSourceUnitTests
{
	[Test]
	public static void photoSourceTest_fbNoProfile_getLarge()
	{
		SocialMember testMember = SocialTestingHelper.generateSocialMember();
		PhotoSource testSource = createSource(testMember, "", "facebook.png", "facebook_large.png");
		Assert.AreEqual("facebook_large.png", testSource.getPrimaryUrl(true));
	}

	[Test]
	public static void photoSourceTest_fbNoProfile_getSmall()
	{
		SocialMember testMember = SocialTestingHelper.generateSocialMember();
		PhotoSource testSource = createSource(testMember, "", "facebook.png", "facebook_large.png");
		Assert.AreEqual("facebook.png", testSource.getPrimaryUrl(false));
	}

	[Test]
	public static void photoSourceTest_profileWithFb_getLarge()
	{
		SocialMember testMember = SocialTestingHelper.generateSocialMember();
		PhotoSource testSource = createSource(testMember, "profile.png", "facebook.png", "facebook_large.png");
		Assert.AreEqual("profile.png", testSource.getPrimaryUrl(true));
	}

	[Test]
	public static void photoSourceTest_profileWithFb_getSmall()
	{
		SocialMember testMember = SocialTestingHelper.generateSocialMember();
		PhotoSource testSource = createSource(testMember, "profile.png", "facebook.png", "facebook_large.png");
		Assert.AreEqual("profile.png", testSource.getPrimaryUrl());
	}

	[Test]
	public static void photoSourceTest_profileWithNoFb_getLarge()
	{
		SocialMember testMember = SocialTestingHelper.generateSocialMember();
		PhotoSource testSource = createSource(testMember, "profile.png", "", "");
		Assert.AreEqual("profile.png", testSource.getPrimaryUrl(true));
	}

	[Test]
	public static void photoSourceTest_profileWithNoFb_getSmall()
	{
		SocialMember testMember = SocialTestingHelper.generateSocialMember();
		PhotoSource testSource = createSource(testMember, "profile.png", "", "");
		Assert.AreEqual("profile.png", testSource.getPrimaryUrl(false));
	}

	private static PhotoSource createSource(SocialMember member, string profileUrl, string fbUrl, string fbLargeUrl)
	{
		PhotoSource result = new PhotoSource(member);
		result.setUrl(profileUrl, PhotoSource.Source.PROFILE);
		result.setUrl(fbUrl, PhotoSource.Source.FB);
		result.setUrl(fbLargeUrl, PhotoSource.Source.FB_LARGE);
		return result;
	}
	
}
