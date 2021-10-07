using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;

public class CarouselDataUnitTests
{
	private enum CarouselDataTimeRange
	{
		PAST = -1,
		NONE = 0,
		FUTURE = 1,
	}
		
	[Test]
	public static void carouselData_isEnabled_StartTimePast_EndTimePast()
	{
		SlotsPlayer.isLoggedIn = true;
		CarouselData carouselData = generateCarouselData(false, CarouselDataTimeRange.PAST, CarouselDataTimeRange.PAST);
		Assert.IsFalse(carouselData.getIsValid());
	}
	
	[Test]
	public static void carouselData_isEnabled_StartTimePast_EndTimeFuture()
	{
		SlotsPlayer.isLoggedIn = true;
		CarouselData carouselData = generateCarouselData(false, CarouselDataTimeRange.PAST, CarouselDataTimeRange.FUTURE);
		Assert.IsTrue(carouselData.getIsValid());
	}
	
	[Test]
	public static void carouselData_isEnabled_StartTimeFuture_EndTimePast()
	{
		SlotsPlayer.isLoggedIn = true;
		CarouselData carouselData = generateCarouselData(false, CarouselDataTimeRange.FUTURE, CarouselDataTimeRange.PAST);
		Assert.IsFalse(carouselData.getIsValid());
	}
	
	[Test]
	public static void carouselData_isEnabled_StartTimeFuture_EndTimeFuture()
	{
		SlotsPlayer.isLoggedIn = true;
		CarouselData carouselData = generateCarouselData(false, CarouselDataTimeRange.FUTURE, CarouselDataTimeRange.FUTURE);
		Assert.IsFalse(carouselData.getIsValid());
	}
	
	[Test]
	public static void carouselData_isEnabled_StartTimeNone_EndTimePast()
	{
		SlotsPlayer.isLoggedIn = true;
		CarouselData carouselData = generateCarouselData(false, CarouselDataTimeRange.NONE, CarouselDataTimeRange.PAST);
		Assert.IsFalse(carouselData.getIsValid());
	}
	
	[Test]
	public static void carouselData_isEnabled_StartTimeNone_EndTimeFuture()
	{
		SlotsPlayer.isLoggedIn = true;
		CarouselData carouselData = generateCarouselData(false, CarouselDataTimeRange.NONE, CarouselDataTimeRange.FUTURE);
		Assert.IsTrue(carouselData.getIsValid());
	}
	
	[Test]
	public static void carouselData_isEnabled_StartTimePast_EndTimeNone()
	{
		SlotsPlayer.isLoggedIn = true;
		CarouselData carouselData = generateCarouselData(false, CarouselDataTimeRange.PAST, CarouselDataTimeRange.NONE);
		Assert.IsTrue(carouselData.getIsValid());
	}
	
	[Test]
	public static void carouselData_isEnabled_StartTimeFuture_EndTimeNone()
	{
		SlotsPlayer.isLoggedIn = true;
		CarouselData carouselData = generateCarouselData(false, CarouselDataTimeRange.FUTURE, CarouselDataTimeRange.NONE);
		Assert.IsFalse(carouselData.getIsValid());
	}
	
	[Test]
	public static void carouselData_isEnabled_StartTimeNone_EndTimeNone()
	{
		SlotsPlayer.isLoggedIn = true;
		CarouselData carouselData = generateCarouselData(false, CarouselDataTimeRange.FUTURE, CarouselDataTimeRange.NONE);
		Assert.IsFalse(carouselData.getIsValid());
	}
	
	[Test]
	public static void carouselData_isEnabled_AlwaysActive_StartTimeNone_EndTimeNone()
	{
		SlotsPlayer.isLoggedIn = true;
		CarouselData carouselData = generateCarouselData(true, CarouselDataTimeRange.NONE, CarouselDataTimeRange.NONE);
		Assert.IsTrue(carouselData.getIsValid());
	}
	
	[Test]
	public static void carouselData_isEnabled_AlwaysActive_StartTimePast_EndTimeFuture()
	{
		SlotsPlayer.isLoggedIn = true;
		CarouselData carouselData = generateCarouselData(true, CarouselDataTimeRange.PAST, CarouselDataTimeRange.FUTURE);
		Assert.IsTrue(carouselData.getIsValid());
	}
	
	[Test]
	public static void carouselData_isEnabled_AlwaysActive_StartTimePast_EndTimePast()
	{
		SlotsPlayer.isLoggedIn = true;
		CarouselData carouselData = generateCarouselData(true, CarouselDataTimeRange.PAST, CarouselDataTimeRange.PAST);
		Assert.IsTrue(carouselData.getIsValid());
	}
	
	private static CarouselData generateCarouselData(bool alwaysActive, CarouselDataTimeRange startTime, CarouselDataTimeRange endTime)
	{
		string startTimeString = null;
		string endTimeString = null;
		int startTimeInt = 0;
		int endTimeInt = 0;
		int currentTime = Common.utcTimeInSeconds();
		switch (startTime)
		{
			case CarouselDataTimeRange.PAST:
				startTimeInt = (currentTime - 1000);
				startTimeString = string.Format("{0}", startTimeInt);
				break;
			
			case CarouselDataTimeRange.FUTURE:
				startTimeInt = (currentTime + 1000);
				startTimeString = string.Format("{0}", startTimeInt);
				break;
			
			default:
				break;
		}
		
		switch (endTime)
		{
			case CarouselDataTimeRange.PAST:
				endTimeInt = (currentTime - 1000);
				endTimeString = string.Format("{0}", endTimeInt);
				break;
			
			case CarouselDataTimeRange.FUTURE:
				endTimeInt = (currentTime + 1000);
				endTimeString = string.Format("{0}", endTimeInt);
				break;
			
			default:
				break;
		}

		string jsonString = string.Format(
			"{{\"sku_key\":\"hir\",\"active\":{0},\"type\":\"8\",\"sort_index\":\"0\",\"enable_ios\":{1},\"enable_android\":{2},\"enable_kindle\":{3},\"enable_unityweb\":{4}}}",
			alwaysActive.ToString().ToLower(),
			"true",
			"true",
			"true",
			"true"
		);
		
		JSON carouselJson = new JSON(jsonString);
		carouselJson.jsonDict["start_time"] = startTimeString;
		carouselJson.jsonDict["end_time"] = endTimeString;
		CarouselData carouselData = new CarouselData(carouselJson);

		return carouselData;
	}
}
