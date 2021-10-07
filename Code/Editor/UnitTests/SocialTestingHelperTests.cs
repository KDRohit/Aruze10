using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;

namespace HelperClassTesting
{
	public class SocialTestingHelperTests
	{
		[Test]
		public static void testSocialMemberCreation()
		{
			SocialMember member = SocialTestingHelper.generateSocialMember();
			Assert.AreNotEqual("", member.id);
			Assert.AreNotEqual("", member.zId);
			Assert.AreNotEqual("", member.firstName);
			Assert.AreNotEqual("", member.lastName);
			Assert.Greater(member.vipLevel, -1);
			Assert.Less(member.vipLevel, SocialTestingHelper.NUM_VIP_LEVELS + 1);
		}

		[Test]
		public static void testAnonymousSocialMemberCreation()
		{
			SocialMember member = SocialTestingHelper.generateSocialMember(true);
			Assert.AreEqual("", member.id);
			Assert.AreNotEqual("", member.zId);
			Assert.AreNotEqual("", member.firstName);
			Assert.AreNotEqual("", member.lastName);
			Assert.Greater(member.vipLevel, -1);
			Assert.Less(member.vipLevel, SocialTestingHelper.NUM_VIP_LEVELS + 1);
		}

		[Test]
		public static void testSocialMemberListGeneration()
		{
			int MEMBER_COUNT = 10;
			List<SocialMember> memberList = SocialTestingHelper.getFakeMemberList(MEMBER_COUNT, false);
			Assert.AreEqual(memberList.Count, MEMBER_COUNT);
			
			int numAnonyous = 0;
			for (int i = 0; i < memberList.Count; i++)
			{
				if (string.IsNullOrEmpty(memberList[i].id))
				{
					numAnonyous++;
				}
			}

			Assert.AreEqual(numAnonyous, 0);
		}

		[Test]
		public static void testAnonymousSocialMemberListGeneration()
		{
			int MEMBER_COUNT = 10;
			List<SocialMember> memberList = SocialTestingHelper.getFakeMemberList(MEMBER_COUNT, true);
			Assert.AreEqual(memberList.Count, MEMBER_COUNT);

			int numAnonyous = 0;

			for (int i = 0; i < memberList.Count; i++)
			{
				if (string.IsNullOrEmpty(memberList[i].id))
				{
					numAnonyous++;
				}
			}
			Assert.Greater(numAnonyous, 0);
		}		
	}
}
