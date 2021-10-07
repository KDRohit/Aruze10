using NUnit.Framework;
using System.Collections.Generic;
using Com.Rewardables;
using UnityEngine.TestTools;
using UnityEngine;


[TestFixture]
public class ChallengeUnitTests : UnitTestDependencyInitializer, IPrebuildSetup
{
	private const string RICH_PASS_TEST_DATA_DIRECTORY = "Test Data/RichPass/";
	private const string RICH_PASS_LOGIN_FILE = "login";
	
	private JSON getRichPassLoginData()
	{
		string testDataPath = RICH_PASS_TEST_DATA_DIRECTORY + RICH_PASS_LOGIN_FILE;
		TextAsset textAsset = (TextAsset)Resources.Load(testDataPath,typeof(TextAsset));
		string text = textAsset.text;
		//update start/end time to be current
		text = text.Replace("\"start_time\" : 1574714218", "\"start_time\" : " +  GameTimer.currentTime);
		text = text.Replace("\"end_time\" : 1574715218", "\"end_time\" : " +   (GameTimer.currentTime + (60 * 60)));
		return new JSON(text);
	}
	
	public void Setup()
	{
		JSON data = getRichPassLoginData();
		CampaignDirector.populateAll(new JSON[] { data } );
	}

	protected override void onInit()
	{
		RewardablesManager.init();
	}
	
	/******************
	Valid Data Tests
	******************/

	[Test]
	[PrebuildSetup(typeof(ChallengeUnitTests))]
	public static void Challenge_RichPass_incrementOnePoint()
	{
		long pointsToAdd = 1;
		long expectedPoints = CampaignDirector.richPass.pointsAcquired + pointsToAdd;
		CampaignDirector.richPass.incrementPoints(pointsToAdd);
		Assert.AreEqual(expectedPoints,CampaignDirector.richPass.pointsAcquired);
	}
	
	[Test]
	[PrebuildSetup(typeof(ChallengeUnitTests))]
	public static void Challenge_RichPass_incrementMultiplePoints()
	{
		int pointsToAdd = 100;
		long expectedPoints = CampaignDirector.richPass.pointsAcquired + pointsToAdd;
		CampaignDirector.richPass.incrementPoints(pointsToAdd);
		Assert.AreEqual(expectedPoints,CampaignDirector.richPass.pointsAcquired);
	}
	
	[Test]
	[PrebuildSetup(typeof(ChallengeUnitTests))]
	public static void Challenge_RichPass_negativePointAddition()
	{
		int pointsToAdd = -99;
		long expectedPoints = CampaignDirector.richPass.pointsAcquired;
		CampaignDirector.richPass.incrementPoints(pointsToAdd);
		Assert.AreEqual(expectedPoints,CampaignDirector.richPass.pointsAcquired);
	}
	
	[Test]
	[PrebuildSetup(typeof(ChallengeUnitTests))]
	public static void Challenge_RichPass_redeemAward()
	{
		RichPassCampaign.RewardTrack silver = CampaignDirector.richPass.silverTrack;
		List<PassReward> silverRewards = silver.getSingleRewardsList(30);
		Assert.NotNull(silverRewards);
		Assert.Greater(silverRewards.Count, 0);
		string rewardJson = DevGUIMenuRichPass.buildRewardJSON("silver", silverRewards[0], 30);
		RewardablesManager.onRPRewardGranted(new JSON(rewardJson));
		silverRewards = silver.getSingleRewardsList(30);
		Assert.IsTrue(silverRewards[0].claimed);		
	}
}


