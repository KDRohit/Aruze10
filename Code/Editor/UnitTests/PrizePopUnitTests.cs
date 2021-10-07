using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine;

namespace PrizePop
{

	[TestFixture]
	public class PrizePopUnitTests : IPrebuildSetup, IPostBuildCleanup
	{
		private const string TEST_DATA_DIRECTORY = "Test Data/PrizePop/";
		private const string LOGIN_FILE = "login";
	
		private JSON getPrizePopLoginData()
		{
			string testDataPath = TEST_DATA_DIRECTORY + LOGIN_FILE;
			TextAsset textAsset = (TextAsset)Resources.Load(testDataPath,typeof(TextAsset));
			string text = textAsset.text;
			//update start/end time to be current
			text = text.Replace("\"start_time\" : 1574714218", "\"start_time\" : " +  GameTimer.currentTime);
			text = text.Replace("\"end_time\" : 1574715218", "\"end_time\" : " +   (GameTimer.currentTime + (60 * 60)));
			return new JSON(text);
		}
	
		public void Setup()
		{
			JSON data = getPrizePopLoginData();
			PrizePopFeature.instantiateFeature(data);
		}

		public void Cleanup()
		{
			
		}
		
		
		[Test]
		[PrebuildSetup(typeof(PrizePopUnitTests))]
		public static void PrizePop_Points_Increment()
		{
			PrizePopFeature.instance.incrementPoints(1);
			Assert.AreEqual(1, PrizePopFeature.instance.currentPoints);
		}
		
		[Test]
		[PrebuildSetup(typeof(PrizePopUnitTests))]
		public static void PrizePop_Points_Decrement()
		{
			//we should not be able to add a negative value
			PrizePopFeature.instance.incrementPoints(-1);
			Assert.AreEqual(0, PrizePopFeature.instance.currentPoints);
		}
		
		[Test]
		[PrebuildSetup(typeof(PrizePopUnitTests))]
		public static void PrizePop_Pick_Increment()
		{
			Assert.AreEqual(0, PrizePopFeature.instance.numPicksAvailable);
		}
		
		[Test]
		[PrebuildSetup(typeof(PrizePopUnitTests))]
		public static void PrizePop_Pick_Win()
		{
			Assert.AreEqual(0, PrizePopFeature.instance.numPicksAvailable);
		}
		
		[Test]
		[PrebuildSetup(typeof(PrizePopUnitTests))]
		public static void PrizePop_Pick_Jackpot()
		{
			//TODO: make jackpot pick
			Assert.AreEqual(0, PrizePopFeature.instance.currentRound);
		}
		
		[Test]
		[PrebuildSetup(typeof(PrizePopUnitTests))]
		public static void PrizePop_Pick_Empty()
		{
			Assert.AreEqual(0, PrizePopFeature.instance.numPicksAvailable);
		}
		
		[Test]
		[PrebuildSetup(typeof(PrizePopUnitTests))]
		public static void PrizePop_Pick_LastJackpot()
		{
			Assert.AreEqual(0, PrizePopFeature.instance.numPicksAvailable);
		}
		

	}
}
