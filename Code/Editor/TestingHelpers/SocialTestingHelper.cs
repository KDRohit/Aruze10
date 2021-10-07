using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class SocialTestingHelper
{
	public static List<SocialMember> getFakeMemberList(int count = 2, bool shouldGenerateAnonymous = false)
	{
		bool hasGeneratedAnonymous = false;
		List<SocialMember> result = new List<SocialMember>();
		for (int i = 0; i < count; i++)
		{
			// If we want anonymous users, then always generate at least one at the start.
			// After that we should have a 50/50 shot at creating them.
			bool isAnon = shouldGenerateAnonymous &&
				(!hasGeneratedAnonymous || Random.Range(0, 2) == 0);

			result.Add(generateSocialMember(isAnon));
		}
		
		string listString = "{";
		for (int listIndex = 0; listIndex < result.Count; listIndex++)
		{
			if (listIndex != 0)
			{
				listString += ",";
			}
			listString += result[listIndex].zId;
		}
		listString += "}";
		return result;
	}

	public static SocialMember generateSocialMember(bool isAnon = false)
	{
		string id = isAnon ? "" : randomFbId();
		string nid = "";
		string zid = randomZid();
		string firstName = randomFirstName();
		string lastName = randomLastName();
		int vipLevel = randomVipLevel();
		SocialMember result = new SocialMember(id, zid, nid, firstName, lastName, vipLevel);
		return result;
	}

#region random_social_var_generation

	public const int ZID_LENGTH = 11;
	public const int FBID_LENGTH = 20; // TODO -- change this its a guess.
	public const int NUM_VIP_LEVELS = 9;
	
	public static readonly string[] TEST_FIRST_NAMES = {"John", "Alice", "Ben", "Sarah", "Todd", "Karen"};
	public static readonly string[] TEST_LAST_NAMES = {"Jacoby", "Gyllenhall", "Burton", "Sanders", "Obama", "Henson"};
	/*
		Series of methods to generate the different parts of a social member. 
		These should be expanded as needed.
	*/
	public static string randomFbId()
	{
		string result = "";
		for (int i = 0; i < FBID_LENGTH; i++)
		{
			result += Random.Range(0, 10).ToString();
		}
		return result;
	}
	
	public static string randomZid()
	{
		string result = "";
		for (int i = 0; i < ZID_LENGTH; i++)
		{
			result += Random.Range(0, 10).ToString();
		}
		return result;
	}
	
	public static string randomFirstName()
	{
		int index = Random.Range(0, TEST_FIRST_NAMES.Length);
		return TEST_FIRST_NAMES[index];
	}
	
	public static string randomLastName()
	{
		int index = Random.Range(0, TEST_LAST_NAMES.Length);
		return TEST_LAST_NAMES[index];
	}
	
	public static int randomVipLevel()
	{
		// Generate a random VIP level.
		int result = Random.Range(0, NUM_VIP_LEVELS + 1);
		return result;
	}

#endregion
}
