using System;
using System.Collections;
using UnityEngine;

// This is a data containter for the information about a spin
// that is then submitted to Splunk
#if ZYNGA_TRAMP
public class TRAMPSpinEventData
{
	public DateTime slotSpinClicked = DateTime.MinValue;
	public DateTime slotSpinRequest = DateTime.MinValue;
	public DateTime slotSpinReceive = DateTime.MinValue;
	public DateTime slotSpinReelsStop = DateTime.MinValue;
	public DateTime slotSpinComplete = DateTime.MinValue;
	public DateTime slotSpinSlamStopped = DateTime.MinValue;

	public double getTotalSpinTime()
	{
		return (slotSpinComplete - slotSpinClicked).TotalSeconds;
	}

	public double getTotalSpinTimeMinusNetworkLatency()
	{
		return getTotalSpinTime() - getSpinRequestToReceiveTime();
	}

	public double getSpinClickToRequestTime()
	{
		return (slotSpinRequest - slotSpinClicked).TotalSeconds;
	}

	public double getSpinRequestToReceiveTime()
	{
		return (slotSpinReceive - slotSpinRequest).TotalSeconds;
	}

	public double getSpinReceiveToReelsStopTime()
	{
		return (slotSpinReelsStop - slotSpinReceive).TotalSeconds;
	}

	public double getReelsStopToSpinCompleteTime()
	{
		return (slotSpinComplete - slotSpinReelsStop).TotalSeconds;
	}
}
#endif
