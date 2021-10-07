using UnityEngine;
using System.Collections;
using Com.Rewardables;

public abstract class UnitTestDependencyInitializer
{
	public static bool hasInitialized { get; private set; }

	protected UnitTestDependencyInitializer()
	{
		init();
	}

	private void init()
	{
		if (!hasInitialized)
		{
			hasInitialized = true;
			onInit();
		}
	}

	protected abstract void onInit();
}