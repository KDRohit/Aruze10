using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Module originally made for Cesar01 to override the wait time per cluster to speed
up the pay box display before tumbling continues
*/
public class LegacyPlopTumbleWaitTimePerClusterOverrideModule : SlotModule 
{
	[SerializeField] private float waitTimePerClusterOverride = 1.5f;

// needsToOverrideLegacyPlopTumbleWaitTimePerCluster() section
// special module hook made to allow altering this value from the default in
// legacy plop and tumble games in order to alter the timing of those games
	public override bool needsToOverrideLegacyPlopTumbleWaitTimePerCluster()
	{
		return true;
	}

	public override float getLegacyPlopTumbleWaitTimePerClusterOverride()
	{
		// just going to try making this the length of the symbol animations themselves,
		// since that should be all cesar01 is doing when animating
		return waitTimePerClusterOverride;
	}
}
