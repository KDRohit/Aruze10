
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MultiTimeRangeEventFeature : FeatureBase
{
	public LongestTimer featureTimer { get; private set; }

	// Storing timer initialization information. This is needed for turning a feature back on
	// after it has been manually disabled.
	private int _beginTime = -1;
	private int _endTime = -1;
	private int _timeRemaining = -1;
	private int _initializedTime = -1;
	
	public override bool isEnabled
	{
		// Should be overridden by subclasses.
		get
		{
			return base.isEnabled && featureTimer != null && featureTimer.isEnabled;
		}
	}

	public MultiTimeRangeEventFeature()
	{
		featureTimer = new LongestTimer();
	}
	public override void initFeature(JSON data  = null)
	{
		// Don't call enable/disable here, this will
		// happen from the timer creation funcitons.
		initializeWithData(data);
		registerEventDelegates();
		OnInit();
	}

	protected void setTimestamps(int beginTime, int endTime, string source = LongestTimer.unknownSource)
	{
		
		featureTimer.addTimeRange(beginTime, endTime,source);   // new GameTimerRange(beginTime, endTime, true, onEventStarted);
		featureTimer.disableDelegate = onEventEnded;
		_beginTime = featureTimer.combinedActiveTimeRange != null
			? featureTimer.combinedActiveTimeRange.startTimestamp
			: beginTime;
		_endTime = featureTimer.combinedActiveTimeRange != null
			? featureTimer.combinedActiveTimeRange.endTimestamp
			: endTime;
	}

	protected void startWithTimeRemaining(int timeRemaining, string source)
	{
		_timeRemaining = timeRemaining;
		_initializedTime = GameTimer.currentTime;
		if (featureTimer != null && featureTimer.isEnabled)
		{
			Debug.LogFormat("EventFeatureBase.cs -- startWithTimeRemaining -- featureTimer already started and running, you are initializing it twice.");
		}

		if (timeRemaining > 0)
		{
			
			featureTimer.addTimeRangeDuration(timeRemaining,source);//GameTimerRange.createWithTimeRemaining(timeRemaining);
			featureTimer.disableDelegate = onEventEnded;
			if (isEnabled)
			{
				// If we create the timer without a start time (only with time remaining)
				// and everything else about the feature says it is on then send
				// out the enabled event.
				OnEnabled();
			}
		}
	}

	protected void onEventStarted()
	{
		OnEnabled();
	}

	/*protected void onEventEnded(Dict args = null, GameTimerRange originalTimer = null)
	{
		OnDisabled();
	}*/
	protected void onEventEnded()
	{
		OnDisabled();
	}
	public override void disableFeature()
	{
		base.disableFeature();
		featureTimer = null;
	}

	public override void reenableFeature()
	{
		const string source = "CRM";
		base.reenableFeature();
		if (_beginTime > 0 && _endTime > 0)
		{
			setTimestamps(_beginTime, _endTime,source);
		}
		else if (_timeRemaining > 0)
		{
			int timeElapsed = (GameTimer.currentTime - _initializedTime);
			int adjustedTimeRemaining = _timeRemaining - timeElapsed;
			startWithTimeRemaining(adjustedTimeRemaining,source);
		}
		else
		{
			Debug.LogErrorFormat("EventFeatureBase.cs -- reenableFeature() -- calling re-enable on feature without initializing it first.");
		}
	}
}
