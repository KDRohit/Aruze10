using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
	public class ReevaluationMultiPersonalJackpotUnitTests
	{
		[Test]
		public static void DoesOrderByReelIndex()
		{
			string testJSONstring = @"{""jackpots"":[
				{""jackpot_key"":""minor"",""jackpot_events"":[
					{""type"":""win"",""win_credits"":""0"",""trigger_symbols"":[{""reel_index"":""2"",""pos"":""1""}]}
				]},
				{""jackpot_key"":""mini"",""jackpot_events"":[
					{""type"":""win"",""win_credits"":""0"",""trigger_symbols"":[{""reel_index"":""1"",""pos"":""1""}]}
				]}
			]}";

			JSON testJSON = new JSON(testJSONstring);
			ReevaluationMultiPersonalJackpot reevaluationMultiPersonalJackpot = new ReevaluationMultiPersonalJackpot(testJSON);
			List<ReevaluationMultiPersonalJackpot.JackpotEvent> jackpotEvents = reevaluationMultiPersonalJackpot.getJackpotEventsOrderedByFirstReelIncreaseWinResetPosition();
			Assert.AreEqual("mini", jackpotEvents[0].jackpotKey);
			Assert.AreEqual("minor", jackpotEvents[1].jackpotKey);
		}

		[Test]
		public static void DoesOrderByPosition()
		{
			string testJSONstring = @"{""jackpots"":[
				{""jackpot_key"":""minor"",""jackpot_events"":[
					{""type"":""win"",""win_credits"":""0"",""trigger_symbols"":[{""reel_index"":""1"",""pos"":""1""}]}
				]},
				{""jackpot_key"":""mini"",""jackpot_events"":[
					{""type"":""win"",""win_credits"":""0"",""trigger_symbols"":[{""reel_index"":""1"",""pos"":""2""}]}
				]}
			]}";

			JSON testJSON = new JSON(testJSONstring);
			ReevaluationMultiPersonalJackpot reevaluationMultiPersonalJackpot = new ReevaluationMultiPersonalJackpot(testJSON);
			List<ReevaluationMultiPersonalJackpot.JackpotEvent> jackpotEvents = reevaluationMultiPersonalJackpot.getJackpotEventsOrderedByFirstReelIncreaseWinResetPosition();
			Assert.AreEqual("mini", jackpotEvents[0].jackpotKey);
			Assert.AreEqual("minor", jackpotEvents[1].jackpotKey);
		}

		[Test]
		public static void DoesOrderByIncreaseWinReset()
		{
			string testJSONstring = @"{""jackpots"":[
				{""jackpot_key"":""major"",""jackpot_events"":[
					{""type"":""reset"",""trigger_symbols"":[{""reel_index"":""1"",""pos"":""2""}]}
				]},
				{""jackpot_key"":""mini"",""jackpot_events"":[
					{""type"":""increase"",""trigger_symbols"":[{""reel_index"":""1"",""pos"":""2""}]}
				]},
				{""jackpot_key"":""minor"",""jackpot_events"":[
					{""type"":""win"",""trigger_symbols"":[{""reel_index"":""1"",""pos"":""2""}]}
				]}
			]}";

			JSON testJSON = new JSON(testJSONstring);

			ReevaluationMultiPersonalJackpot reevaluationMultiPersonalJackpot = new ReevaluationMultiPersonalJackpot(testJSON);
			Assert.IsNotNull(reevaluationMultiPersonalJackpot);

			List<ReevaluationMultiPersonalJackpot.JackpotEvent> jackpotEvents = reevaluationMultiPersonalJackpot.getJackpotEventsOrderedByFirstReelIncreaseWinResetPosition();
			Assert.AreEqual("mini", jackpotEvents[0].jackpotKey);
			Assert.AreEqual("minor", jackpotEvents[1].jackpotKey);
			Assert.AreEqual("major", jackpotEvents[2].jackpotKey);
		}

		[Test]
		public static void DoesOrderByReelIncreaseWinResetPosition()
		{
			string testJSONstring = @"{""jackpots"":[
				{""jackpot_key"":""minor"",""jackpot_events"":[
					{""type"":""win"",""trigger_symbols"":[{""reel_index"":""2"",""pos"":""2""}]}
				]},
				{""jackpot_key"":""major"",""jackpot_events"":[
					{""type"":""reset"",""trigger_symbols"":[{""reel_index"":""3"",""pos"":""2""}]}
				]},
				{""jackpot_key"":""mini"",""jackpot_events"":[
					{""type"":""increase"",""trigger_symbols"":[{""reel_index"":""5"",""pos"":""2""}]}
				]},
				{""jackpot_key"":""mini"",""jackpot_events"":[
					{""type"":""increase"",""trigger_symbols"":[{""reel_index"":""1"",""pos"":""2""}]}
				]},
				{""jackpot_key"":""minor"",""jackpot_events"":[
					{""type"":""win"",""trigger_symbols"":[{""reel_index"":""1"",""pos"":""2""}]}
				]},
				{""jackpot_key"":""major"",""jackpot_events"":[
					{""type"":""reset"",""trigger_symbols"":[{""reel_index"":""1"",""pos"":""2""}]}
				]},
				{""jackpot_key"":""mini"",""jackpot_events"":[
					{""type"":""increase"",""trigger_symbols"":[{""reel_index"":""1"",""pos"":""3""}]}
				]}
			]}";

			JSON testJSON = new JSON(testJSONstring);

			ReevaluationMultiPersonalJackpot reevaluationMultiPersonalJackpot = new ReevaluationMultiPersonalJackpot(testJSON);
			Assert.IsNotNull(reevaluationMultiPersonalJackpot);

			List<ReevaluationMultiPersonalJackpot.JackpotEvent> jackpotEvents = reevaluationMultiPersonalJackpot.getJackpotEventsOrderedByFirstReelIncreaseWinResetPosition();

			Assert.AreEqual("mini", jackpotEvents[0].jackpotKey);
			Assert.AreEqual(1, jackpotEvents[0].triggerSymbols[0].reelIndex);
			Assert.AreEqual(3, jackpotEvents[0].triggerSymbols[0].pos);
			Assert.AreEqual("mini", jackpotEvents[1].jackpotKey);
			Assert.AreEqual(1, jackpotEvents[1].triggerSymbols[0].reelIndex);
			Assert.AreEqual(2, jackpotEvents[1].triggerSymbols[0].pos);
			Assert.AreEqual("minor", jackpotEvents[2].jackpotKey);
			Assert.AreEqual(1, jackpotEvents[2].triggerSymbols[0].reelIndex);
			Assert.AreEqual(2, jackpotEvents[2].triggerSymbols[0].pos);
			Assert.AreEqual("major", jackpotEvents[3].jackpotKey);
			Assert.AreEqual(1, jackpotEvents[3].triggerSymbols[0].reelIndex);
			Assert.AreEqual(2, jackpotEvents[3].triggerSymbols[0].pos);

			Assert.AreEqual("minor", jackpotEvents[4].jackpotKey);
			Assert.AreEqual(2, jackpotEvents[4].triggerSymbols[0].reelIndex);

			Assert.AreEqual("major", jackpotEvents[5].jackpotKey);
			Assert.AreEqual(3, jackpotEvents[5].triggerSymbols[0].reelIndex);

			Assert.AreEqual("mini", jackpotEvents[6].jackpotKey);
			Assert.AreEqual(5, jackpotEvents[6].triggerSymbols[0].reelIndex);
		}
	}
}
