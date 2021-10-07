using UnityEngine;
using System.Collections;
using NUnit.Framework;

public class UnitTestFrameReporter
{
	/*=========================================================================================
	TEST STABLE FRAMERATES
	=========================================================================================*/
	//[Test]
	public static void test60Stable()
	{
		FrameReporter.instance.clear();
		FrameReporter.instance.overrideCurrentTarget(60);

		for (int k = 0; k < FrameReporter.THROTTLE_TRACK_LIMIT; k++)
		{
			FrameReporter.instance.updateDynamicFPS(1 / 60f, true);
		}

		Assert.GreaterOrEqual(FrameReporter.instance.currentFPSTarget, 60);
	}

	//[Test]
	public static void test30Stable()
	{
		FrameReporter.instance.clear();
		FrameReporter.instance.overrideCurrentTarget(30);

		for (int k = 0; k < FrameReporter.THROTTLE_TRACK_LIMIT; k++)
		{
			FrameReporter.instance.updateDynamicFPS(1 / 30f, true);
		}

		Assert.GreaterOrEqual(FrameReporter.instance.currentFPSTarget, 30);
	}

	//[Test]
	public static void test20Stable()
	{
		FrameReporter.instance.clear();
		FrameReporter.instance.overrideCurrentTarget(20);

		for (int k = 0; k < FrameReporter.THROTTLE_TRACK_LIMIT; k++)
		{
			FrameReporter.instance.updateDynamicFPS(1 / 20f, true);
		}

		Assert.GreaterOrEqual(FrameReporter.instance.currentFPSTarget, 20);
	}

	//[Test]
	public static void test10Stable()
	{
		FrameReporter.instance.clear();
		FrameReporter.instance.overrideCurrentTarget(20);

		for (int k = 0; k < FrameReporter.THROTTLE_TRACK_LIMIT; k++)
		{
			FrameReporter.instance.updateDynamicFPS(1 / 10f, true);
		}

		Assert.GreaterOrEqual(FrameReporter.instance.currentFPSTarget, 20);
	}

	/*=========================================================================================
	TEST UNSTABLE FRAMERATES
	=========================================================================================*/
	//[Test]
	public static void test120Unstable()
	{
		FrameReporter.instance.clear();
		FrameReporter.instance.overrideCurrentTarget(60);

		for (int k = 0; k < FrameReporter.THROTTLE_TRACK_LIMIT; k++)
		{
			if (k >= 15)
			{
				FrameReporter.instance.updateDynamicFPS(0.1f, true);
			}
			else
			{
				FrameReporter.instance.updateDynamicFPS(1 / 120f, true);
			}
		}

		Assert.GreaterOrEqual(FrameReporter.instance.currentFPSTarget, 60);
	}

	//[Test]
	public static void test60Unstable()
	{
		FrameReporter.instance.clear();
		FrameReporter.instance.overrideCurrentTarget(60);

		for (int k = 0; k < FrameReporter.THROTTLE_TRACK_LIMIT; k++)
		{
			if (k >= 15)
			{
				FrameReporter.instance.updateDynamicFPS(0.1f, true);
			}
			else
			{
				FrameReporter.instance.updateDynamicFPS(1 / 60f, true);
			}
		}

		Assert.GreaterOrEqual(FrameReporter.instance.currentFPSTarget, 60);
	}

	//[Test]
	public static void test30Unstable()
	{
		FrameReporter.instance.clear();
		FrameReporter.instance.overrideCurrentTarget(30);

		for (int k = 0; k < FrameReporter.THROTTLE_TRACK_LIMIT; k++)
		{
			if (k >= 15)
			{
				FrameReporter.instance.updateDynamicFPS(0.1f, true);
			}
			else
			{
				FrameReporter.instance.updateDynamicFPS(1 / 30f, true);
			}
		}

		Assert.GreaterOrEqual(FrameReporter.instance.currentFPSTarget, 30);
	}

	//[Test]
	public static void test20Unstable()
	{
		FrameReporter.instance.clear();
		FrameReporter.instance.overrideCurrentTarget(20);

		for (int k = 0; k < FrameReporter.THROTTLE_TRACK_LIMIT; k++)
		{
			if (k >= 15)
			{
				FrameReporter.instance.updateDynamicFPS(0.1f, true);
			}
			else
			{
				FrameReporter.instance.updateDynamicFPS(1 / 20f, true);
			}
		}

		Assert.GreaterOrEqual(FrameReporter.instance.currentFPSTarget, 20);
	}

	//[Test]
	public static void test0Injection()
	{
		FrameReporter.instance.clear();
		FrameReporter.instance.overrideCurrentTarget(0);

		for (int k = 0; k < FrameReporter.THROTTLE_TRACK_LIMIT; k++)
		{
			FrameReporter.instance.updateDynamicFPS(0, true);
		}

		Assert.GreaterOrEqual(FrameReporter.instance.currentFPSTarget, 20);
	}
}