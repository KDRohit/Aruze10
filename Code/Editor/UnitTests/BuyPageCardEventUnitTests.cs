using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;

public class BuyPageCardEventUnitTests
{
	#region Highest Card Event Lift Tests
	/******************
	Valid Data Tests
	******************/

	[Test]
	public static void BuyPageCardEvent_HighestCardEventLift_MixedPercentageAndXAndNothing_PercentageHighest()
	{
		Assert.AreEqual("590%", BuyPageCardEvent.instance.getHighestCardEventLift("4X,590%,20%,nothing,2X,40%"));
	}

	[Test]
	public static void BuyPageCardEvent_HighestCardEventLift_MixedPercentageAndXAndNothing_XHighest()
	{
		Assert.AreEqual("12X", BuyPageCardEvent.instance.getHighestCardEventLift("7X,590%,20%,nothing,12X,40%"));
	}

	[Test]
	public static void BuyPageCardEvent_HighestCardEventLift_MixedPercentageAndX()
	{
		Assert.AreEqual("12X", BuyPageCardEvent.instance.getHighestCardEventLift("7X,590%,20%,10X,12X,40%"));
	}

	[Test]
	public static void BuyPageCardEvent_HighestCardEventLift_PercentageOnly()
	{
		Assert.AreEqual("90%", BuyPageCardEvent.instance.getHighestCardEventLift("40%,50%,10%,30%,90%,20%"));
	}

	[Test]
	public static void BuyPageCardEvent_HighestCardEventLift_PercentageAndNothing()
	{
		Assert.AreEqual("50%", BuyPageCardEvent.instance.getHighestCardEventLift("40%,50%,nothing,30%,nothing,20%"));
	}

	[Test]
	public static void BuyPageCardEvent_HighestCardEventLift_NothingOnly()
	{
		Assert.AreEqual("", BuyPageCardEvent.instance.getHighestCardEventLift("nothing,nothing,nothing,nothing,nothing,nothing"));
	}

	[Test]
	public static void BuyPageCardEvent_HighestCardEventLift_XAndNothing()
	{
		Assert.AreEqual("12X", BuyPageCardEvent.instance.getHighestCardEventLift("nothing,nothing,12X,nothing,nothing,5X"));	
	}

	[Test]
	public static void BuyPageCardEvent_HighestCardEventLift_AllZeroValue()
	{
		Assert.AreEqual("", BuyPageCardEvent.instance.getHighestCardEventLift("0,0,0,0,0,0"));
	}

	[Test]
	public static void BuyPageCardEvent_HighestCardEventLift_ZeroAndXValues()
	{
		Assert.AreEqual("5X", BuyPageCardEvent.instance.getHighestCardEventLift("0,0,0,0,5X,0"));
	}

	[Test]
	public static void BuyPageCardEvent_HighestCardEventLift_ZeroAndXModifyingZero()
	{
		Assert.AreEqual("", BuyPageCardEvent.instance.getHighestCardEventLift("0,0,0,0,0X,0"));
	}

	[Test]
	public static void BuyPageCardEvent_HighestCardEventLift_ZeroAndPercentage()
	{
		Assert.AreEqual("100%", BuyPageCardEvent.instance.getHighestCardEventLift("0,0,0,100%,0,0"));
	}

	[Test]
	public static void BuyPageCardEvent_HighestCardEventLift_ZeroMixed()
	{
		Assert.AreEqual("5X", BuyPageCardEvent.instance.getHighestCardEventLift("0,10%,3X,170%,0,5X"));
	}

	[Test]
	public static void BuyPageCardEvent_HighestCardEventLift_EmptyCommas()
	{
		Assert.AreEqual("", BuyPageCardEvent.instance.getHighestCardEventLift(",,,,,"));
	}

	[Test]
	public static void BuyPageCardEvent_HighestCardEventLift_TooFewValues()
	{
		Assert.AreEqual("20X", BuyPageCardEvent.instance.getHighestCardEventLift("20X,50%,2X"));
	}

	[Test]
	public static void BuyPageCardEvent_HighestCardEventLift_TooFewValuesTrailingComma()
	{
		Assert.AreEqual("20X", BuyPageCardEvent.instance.getHighestCardEventLift("20X,50%,2X,"));
	}

	[Test]
	public static void BuyPageCardEvent_HighestCardEventLift_TooFewValuesLeadingComma()
	{
		Assert.AreEqual("20X", BuyPageCardEvent.instance.getHighestCardEventLift(",20X,50%,2X"));
	}
	/******************
	Invalid Data Tests
	******************/

	[Test]
	public static void BuyPageCardEvent_HighestCardEventLift_Invalid_NonNothingText()
	{
		Assert.AreEqual("12X", BuyPageCardEvent.instance.getHighestCardEventLift("nothing,nothing,12X,abc,nothing,5X"));
	}

	[Test]
	public static void BuyPageCardEvent_HighestCardEventLift_Invalid_NegativeX()
	{
		Assert.AreEqual("12X", BuyPageCardEvent.instance.getHighestCardEventLift("nothing,nothing,12X,abc,nothing,-5X"));
	}

	[Test]
	public static void BuyPageCardEvent_HighestCardEventLift_Invalid_NegativePercentage()
	{
		Assert.AreEqual("12X", BuyPageCardEvent.instance.getHighestCardEventLift("-20%,nothing,12X,abc,nothing,5X"));
	}

	[Test]
	public static void BuyPageCardEvent_HighestCardEventLift_Invalid_EmptyCommasTooMany()
	{
		Assert.AreEqual("", BuyPageCardEvent.instance.getHighestCardEventLift(",,,,,,,,,,"));
	}

	[Test]
	public static void BuyPageCardEvent_HighestCardEventLift_Invalid_EmptyString()
	{
		Assert.AreEqual("", BuyPageCardEvent.instance.getHighestCardEventLift(""));
	}

	[Test]
	public static void BuyPageCardEvent_HighestCardEventLift_Invalid_NonsenseStringNoCommas()
	{
		Assert.AreEqual("", BuyPageCardEvent.instance.getHighestCardEventLift("asdfjkbaskdlfjl"));
	}

	[Test]
	public static void BuyPageCardEvent_HighestCardEventLift_Invalid_NonsenseStringCommas()
	{
		Assert.AreEqual("", BuyPageCardEvent.instance.getHighestCardEventLift("asdf,jkba,skd,lfjl,werd,qwer"));
	}

	[Test]
	public static void BuyPageCardEvent_HighestCardEventLift_Invalid_NonsenseStringMixedWithValidFields()
	{
		Assert.AreEqual("5X", BuyPageCardEvent.instance.getHighestCardEventLift("asdf,qwer,zxcv,0X,5X,1X"));
	}

	[Test]
	public static void BuyPageCardEvent_HighestCardEventLift_Invalid_NonsenseStringCommasTooMany()
	{
		Assert.AreEqual("", BuyPageCardEvent.instance.getHighestCardEventLift("asdf,r3rdd,qwer3,asdf,jlkio,kljo,87u89d,1234asd,1234d"));
	}

	[Test]
	public static void BuyPageCardEvent_HighestCardEventLift_Invalid_NonsenseStringCommasTooFew()
	{
		Assert.AreEqual("", BuyPageCardEvent.instance.getHighestCardEventLift("87u89d,1234asd,1234d"));
	}
	#endregion

	#region findMixedCardEvent Tests
	[Test]
	public static void BuyPageCardEvent_FindMixedCardEvent_AllMixed()
	{
		Assert.AreEqual(true, BuyPageCardEvent.instance.findMixedCardEventBonuses("20%,nothing,12X,nothing,nothing,5X") );
	}

	[Test]
	public static void BuyPageCardEvent_FindMixedCardEvent_PercentAndNothing()
	{
		Assert.AreEqual(true, BuyPageCardEvent.instance.findMixedCardEventBonuses("20%,nothing,40%,nothing,nothing,50%"));
	}

	[Test]
	public static void BuyPageCardEvent_FindMixedCardEvent_AllNothing()
	{
		Assert.AreEqual(false, BuyPageCardEvent.instance.findMixedCardEventBonuses("nothing,nothing,nothing,nothing,nothing,nothing,nothing"));
	}

	[Test]
	public static void BuyPageCardEvent_FindMixedCardEvent_AllSamePercentagesAndNothing()
	{
		Assert.AreEqual(false, BuyPageCardEvent.instance.findMixedCardEventBonuses("20%,nothing,20%,nothing,20%"));
	}

	[Test]
	public static void BuyPageCardEvent_FindMixedCardEvent_AllSamesXs()
	{
		Assert.AreEqual(false, BuyPageCardEvent.instance.findMixedCardEventBonuses("2X,2X,2X,2X,2X,2X"));
	}

	[Test]
	public static void BuyPageCardEvent_FindMixedCardEvent_AllSamesXsAndNothing()
	{
		Assert.AreEqual(false, BuyPageCardEvent.instance.findMixedCardEventBonuses("2X,2X,nothing,2X,nothing,2X"));
	}

	[Test]
	public static void BuyPageCardEvent_FindMixedCardEvent_MixedPercentages()
	{
		Assert.AreEqual(true, BuyPageCardEvent.instance.findMixedCardEventBonuses("40%,10%,40%,30%,20%,40%"));
	}

	[Test]
	public static void BuyPageCardEvent_FindMixedCardEvent_AllSamePercentages()
	{
		Assert.AreEqual(false, BuyPageCardEvent.instance.findMixedCardEventBonuses("40%,40%,40%,40%,40%,40%"));
	}

	[Test]
	public static void BuyPageCardEvent_FindMixedCardEvent_MixedXs()
	{
		Assert.AreEqual(true, BuyPageCardEvent.instance.findMixedCardEventBonuses("4X,1X,4X,3X,2X,4X"));
	}

	[Test]
	public static void BuyPageCardEvent_FindMixedCardEvent_EmptyCommas()
	{
		Assert.AreEqual(false, BuyPageCardEvent.instance.findMixedCardEventBonuses(",,,,,"));
	}

	[Test]
	public static void BuyPageCardEvent_FindMixedCardEvent_EmptyString()
	{
		Assert.AreEqual(false, BuyPageCardEvent.instance.findMixedCardEventBonuses(""));
	}

	[Test]
	public static void BuyPageCardEvent_FindMixedCardEvent_EmptyCommasTooMany()
	{
		Assert.AreEqual(false, BuyPageCardEvent.instance.findMixedCardEventBonuses(",,,,,,,,"));
	}
	#endregion

	#region BuyPageHeaderText
	// More Rare Cards
	[Test]
	public static void BuyPageCardEvent_HeaderTextMoreRareCards_HasMixedAndNonEmptyBonus()
	{
		testHeaderLocalizationMoreRareCards("more_rare_cards_header_up_to_{0}", "20%", true);
	}

	[Test]
	public static void BuyPageCardEvent_HeaderTextMoreRareCards_HasMixedAndEmptyBonus()
	{
		testHeaderLocalizationMoreRareCards("more_rare_cards_header", "", true);
	}

	[Test]
	public static void BuyPageCardEvent_HeaderTextMoreRareCards_NotMixedAndNonEmptyBonus()
	{
		testHeaderLocalizationMoreRareCards("more_rare_cards_header_{0}", "20%", false);
	}

	[Test]
	public static void BuyPageCardEvent_HeaderTextMoreRareCards_NotMixedAndEmptyBonus()
	{
		testHeaderLocalizationMoreRareCards("more_rare_cards_header", "", false);
	}

	// More Cards
	[Test]
	public static void BuyPageCardEvent_HeaderTextMoreCards_HasMixedAndNonEmptyBonus()
	{
		testHeaderLocalizationMoreCards("more_cards_header_up_to_{0}", "20%", true);
	}

	[Test]
	public static void BuyPageCardEvent_HeaderTextMoreCards_HasMixedAndEmptyBonus()
	{
		testHeaderLocalizationMoreCards("more_cards_header", "", true);
	}

	[Test]
	public static void BuyPageCardEvent_HeaderTextMoreCards_NotMixedAndNonEmptyBonus()
	{
		testHeaderLocalizationMoreCards("more_cards_header_{0}", "20%", false);
	}

	[Test]
	public static void BuyPageCardEvent_HeaderTextMoreCards_NotMixedAndEmptyBonus()
	{
		testHeaderLocalizationMoreCards("more_cards_header", "", false);
	}

	private static void testHeaderLocalizationMoreCards(string expectedResult, string maxBonus, bool hasMixed)
	{
		Assert.AreEqual(expectedResult, BuyPageCardEvent.instance.getBuyCreditsHeaderLocalizationMoreCards(hasMixed, maxBonus));
	}

	private static void testHeaderLocalizationMoreRareCards(string expectedResult, string maxBonus, bool hasMixed)
	{
		Assert.AreEqual(expectedResult, BuyPageCardEvent.instance.getBuyCreditsHeaderLocalizationMoreRareCards(hasMixed, maxBonus));
	}
#endregion
}
