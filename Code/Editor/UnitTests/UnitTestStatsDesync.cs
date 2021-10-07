using UnityEngine;
using System.Collections;
using NUnit.Framework;

public class UnitTestStatsDesync
{
	/*=========================================================================================
	OVERPAYS
	=========================================================================================*/
	[Test]
	public static void testOverpayMidRange()
	{
		DesyncTracker.storeCoinFlow("test_overpay_mid", 1);
		DesyncTracker.storeCoinFlow("test_overpay_mid", 200);
		DesyncTracker.storeCoinFlow("test_overpay_mid", 400);
		DesyncTracker.storeCoinFlow("test_overpay_mid", 600);
		DesyncTracker.storeCoinFlow("test_overpay_mid", 800);
		DesyncTracker.storeCoinFlow("test_overpay_mid", 870);
		DesyncTracker.storeCoinFlow("test_overpay_mid", 931);
		DesyncTracker.storeCoinFlow("test_overpay_mid", 10000);
		DesyncTracker.storeCoinFlow("test_overpay_mid", 100000);
		DesyncTracker.storeCoinFlow("test_overpay_mid", 1000000);
		PlayerResource.DesyncCoinFlow flow = DesyncTracker.getClosestCoinFlow(-900);
		Assert.AreEqual(870, flow.amount);
	}

	[Test]
	public static void testOverpayAccurate()
	{
		DesyncTracker.storeCoinFlow("test_overpay", 1);
		DesyncTracker.storeCoinFlow("test_overpay", 200);
		DesyncTracker.storeCoinFlow("test_overpay", 400);
		DesyncTracker.storeCoinFlow("test_overpay", 600);
		DesyncTracker.storeCoinFlow("test_overpay", 800);
		DesyncTracker.storeCoinFlow("test_overpay", 1000);
		DesyncTracker.storeCoinFlow("test_overpay", 10000);
		DesyncTracker.storeCoinFlow("test_overpay", 100000);
		DesyncTracker.storeCoinFlow("test_overpay", 1000000);
		PlayerResource.DesyncCoinFlow flow = DesyncTracker.getClosestCoinFlow(-10000);
		Assert.AreEqual(10000, flow.amount);
	}

	/*=========================================================================================
	UNDERPAYS
	=========================================================================================*/
	[Test]
	public static void testUnderpayMidRange()
	{
		DesyncTracker.storeCoinFlow("test_underpay_mid", 1);
		DesyncTracker.storeCoinFlow("test_underpay_mid", 200);
		DesyncTracker.storeCoinFlow("test_underpay_mid", 400);
		DesyncTracker.storeCoinFlow("test_underpay_mid", 600);
		DesyncTracker.storeCoinFlow("test_underpay_mid", 800);
		DesyncTracker.storeCoinFlow("test_underpay_mid", 870);
		DesyncTracker.storeCoinFlow("test_underpay_mid", 931);
		DesyncTracker.storeCoinFlow("test_underpay_mid", 1000);
		DesyncTracker.storeCoinFlow("test_underpay_mid", 10000);
		DesyncTracker.storeCoinFlow("test_underpay_mid", 100000);
		DesyncTracker.storeCoinFlow("test_underpay_mid", 1000000);
		PlayerResource.DesyncCoinFlow flow = DesyncTracker.getClosestCoinFlow(900);
		Assert.AreEqual(870, flow.amount);
	}

	[Test]
	public static void testUnderpayAccurate()
	{
		DesyncTracker.storeCoinFlow("test_underpay", 1);
		DesyncTracker.storeCoinFlow("test_underpay", 200);
		DesyncTracker.storeCoinFlow("test_underpay", 400);
		DesyncTracker.storeCoinFlow("test_underpay", 600);
		DesyncTracker.storeCoinFlow("test_underpay", 800);
		DesyncTracker.storeCoinFlow("test_underpay", 1000);
		DesyncTracker.storeCoinFlow("test_underpay", 10000);
		DesyncTracker.storeCoinFlow("test_underpay", 100000);
		DesyncTracker.storeCoinFlow("test_underpay", 1000000);
		PlayerResource.DesyncCoinFlow flow = DesyncTracker.getClosestCoinFlow(10000);
		Assert.AreEqual(10000, flow.amount);
	}

	[Test]
	public static void testAccuracy()
	{
		PlayerResource.DesyncCoinFlow coinFlow = new PlayerResource.DesyncCoinFlow("accuracy_test", 10);
		Assert.AreEqual(10, (int)(DesyncTracker.getAccuracyToTarget(coinFlow, 100) * 100));

		coinFlow.amount = 5;
		Assert.AreEqual(5, (int)(DesyncTracker.getAccuracyToTarget(coinFlow, 100) * 100));

		coinFlow.amount = 50;
		Assert.AreEqual(50, (int)(DesyncTracker.getAccuracyToTarget(coinFlow, 100) * 100));

		coinFlow.amount = 75;
		Assert.AreEqual(75, (int)(DesyncTracker.getAccuracyToTarget(coinFlow, 100) * 100));

		coinFlow.amount = 100;
		Assert.AreEqual(100, (int)(DesyncTracker.getAccuracyToTarget(coinFlow, 100) * 100));

		Assert.AreEqual(0, (int)(DesyncTracker.getAccuracyToTarget(null, 100) * 100));
	}

	[Test]
	public static void testAccuracyOver()
	{
		PlayerResource.DesyncCoinFlow coinFlow = new PlayerResource.DesyncCoinFlow("accuracy_test", 105);
		Assert.AreEqual(95, (int)(DesyncTracker.getAccuracyToTarget(coinFlow, 100) * 100));

		coinFlow.amount = 110;
		Assert.AreEqual(90, Mathf.RoundToInt(DesyncTracker.getAccuracyToTarget(coinFlow, 100) * 100));

		coinFlow.amount = 115;
		Assert.AreEqual(85, Mathf.RoundToInt(DesyncTracker.getAccuracyToTarget(coinFlow, 100) * 100));

		coinFlow.amount = 150;
		Assert.AreEqual(50, Mathf.RoundToInt(DesyncTracker.getAccuracyToTarget(coinFlow, 100) * 100));
	}
}