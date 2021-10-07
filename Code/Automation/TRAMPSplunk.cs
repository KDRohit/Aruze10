using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

#if ZYNGA_TRAMP
public class TRAMPSplunk 
{	
	public const string LOG_NAME = "TRAMP";
	public static TRAMPSpinEventData spinDataEventData = new TRAMPSpinEventData();

	public static TRAMPGameTestStartEventData gameTestStartEventData
	{
		get;
		private set;
	}

	public static TRAMPGameTestEndEventData gameTestEndEventData
	{
		get;
		private set;
	}

	public static void startNewGameTest()
	{
		gameTestStartEventData = new TRAMPGameTestStartEventData();
		gameTestEndEventData = new TRAMPGameTestEndEventData();

		gameTestStartEvent();
	}

	public static void sampleMemoryEvent(AutomatedGameIteration automatedGame, int memMono, 
		int countTextures, int memTextures, 
		int countMeshes, int memMeshes, 
		int countMaterials, int memMaterials, 
		int countAnimationClips, int memAnimationClips, 
		int countAudioClips, int memAudioClips,
		int memTotal)
	{		
		Dictionary<string,string> fields = new Dictionary<string, string>();
		updateSplunkField(fields, "SessionId", AutomatedPlayer.SessionId.ToString());

		if (automatedGame != null)
		{
			updateSplunkField(fields, "GameKey", automatedGame.commonGame.gameKey);
			updateSplunkField(fields, "SpinId", automatedGame.stats.spinsDone);
		}

		updateSplunkField(fields, "MonoMemorySize", memMono); 
		updateSplunkField(fields, "TextureCount", countTextures);
		updateSplunkField(fields, "TextureMemorySize", memTextures);
		updateSplunkField(fields, "MeshCount", countMeshes);
		updateSplunkField(fields, "MeshMemorySize", memMeshes);
		updateSplunkField(fields, "MaterialCount", countMaterials);
		updateSplunkField(fields, "MaterialMemorySize", memMaterials);
		updateSplunkField(fields, "AnimationClipsCount", countAnimationClips);
		updateSplunkField(fields, "AnimationClipsMemorySize", memAnimationClips);
		updateSplunkField(fields, "AudioClipsCount", countAudioClips);
		updateSplunkField(fields, "AudioClipsMemorySize", memAudioClips);
		updateSplunkField(fields, "TotalMemorySize", memTotal);

		Server.sendLogInfo(LOG_NAME, "SampleMemoryEvent", fields, false);

		UnityEngine.Debug.LogFormat("<color={0}>TRAMP> {1} SampleMemoryEvent {2:N3}</color>",
			AutomatedPlayer.TRAMP_DEBUG_COLOR, 
			automatedGame != null ? automatedGame.commonGame.gameKey : "UNKNOWN", 
			memTotal / (double)(1000000));
	}

	public static void spinEvent(AutomatedGameIteration automatedGame)
	{
		Dictionary<string,string> fields = new Dictionary<string, string>();
		updateSplunkField(fields, "SessionId", AutomatedPlayer.SessionId.ToString());

		if (automatedGame != null)
		{
			updateSplunkField(fields, "GameKey", automatedGame.commonGame.gameKey);
			updateSplunkField(fields, "SpinId", automatedGame.stats.spinsDone);
			updateSplunkField(fields, "ReelSetCount", automatedGame.getReelSetCount());
		}

		updateSplunkField(fields, "TimeScale", Time.timeScale);

		if (ReelGame.activeGame != null)
		{
			updateSplunkField(fields, "IsSlammed", ReelGame.activeGame.engine.isSlamStopPressed);

			if (ReelGame.activeGame.outcome != null)
			{
				updateSplunkField(fields, "Credits", ReelGame.activeGame.outcome.getCredits());
				updateSplunkField(fields, "IsAnticipation", ReelGame.activeGame.outcome.getAnticipationTriggers() == null);
			}
		}

		updateSplunkField(fields, "SpinClickToRequestTime", spinDataEventData.getSpinClickToRequestTime());
		updateSplunkField(fields, "SpinRequestToReceiveTime", spinDataEventData.getSpinRequestToReceiveTime());
		updateSplunkField(fields, "SpinReceiveToReelsStopTime", spinDataEventData.getSpinReceiveToReelsStopTime());
		updateSplunkField(fields, "SpinReelsStopToSpinCompleteTime", spinDataEventData.getReelsStopToSpinCompleteTime());
		updateSplunkField(fields, "TotalSpinTime", spinDataEventData.getTotalSpinTime());

		Server.sendLogInfo(LOG_NAME, "SpinEvent", fields, false);
		ForceEventsToServer();

		UnityEngine.Debug.LogFormat("<color={0}>TRAMP> {1} SpinEventData id={2}, TotalSpinTime ={3:N3}</color>",
			AutomatedPlayer.TRAMP_DEBUG_COLOR,
			automatedGame != null ? automatedGame.commonGame.gameKey : "UNKNOWN",
			automatedGame != null ? automatedGame.stats.spinsDone : -1,
			spinDataEventData.getTotalSpinTime());
	}

	public static void sessionStartedEvent(AutomatedPlayerCompanion automatedTestPlan)
	{
		Dictionary<string,string> fields = new Dictionary<string, string>();
		updateSplunkField(fields, "SessionId", AutomatedPlayer.SessionId.ToString());
		updateSplunkField(fields, "TimeStarted", automatedTestPlan.timeStarted);
	}

	public static void gameTestStartEvent()
	{
		Dictionary<string,string> fields = new Dictionary<string, string>();
		updateSplunkField(fields, "SessionId", AutomatedPlayer.SessionId.ToString());
		updateSplunkField(fields, "GameKey", gameTestStartEventData.GameKey);
		updateSplunkField(fields, "StartTime", gameTestStartEventData.StartTime);

		Server.sendLogInfo(LOG_NAME, "SlotGameStartEvent", fields, false);
	}

	public static void gameTestEndEvent()
	{
		Dictionary<string,string> fields = new Dictionary<string, string>();
		updateSplunkField(fields, "SessionId", AutomatedPlayer.SessionId.ToString());

		// This can happen if a test is borked and trying to end.
		if (gameTestStartEventData != null)
		{
			updateSplunkField(fields, "GameKey", gameTestStartEventData.GameKey);
		}

		Server.sendLogInfo(LOG_NAME, "SlotGameEndEvent", fields, false);
	}

	// Summary Event:
	// GameKey: 
	// GameName:
	// Crashes: 
	// Errors:
	// Warnings:
	// Number of Spins:

	public static void gameTestSummaryEvent(AutomatedGameStats stats)
	{

		Dictionary<string,string> fields = new Dictionary<string, string>();
		updateSplunkField(fields, "SessionId", AutomatedPlayer.SessionId.ToString());

		// This can happen if a test is borked and trying to end.
		if (gameTestStartEventData != null)
		{
			updateSplunkField(fields, "GameKey", gameTestStartEventData.GameKey);
			updateSplunkField(fields, "GameName", gameTestStartEventData.GameName);
		}
		if (stats != null)
		{
			updateSplunkField(fields, "exceptions", stats.numberOfExceptions);
			updateSplunkField(fields, "errors", stats.numberOfErrors);
			updateSplunkField(fields, "warnings", stats.numberOfWarnings);
			updateSplunkField(fields, "spins", stats.spinsDone);
		}
		else
		{
			// Something went wrong here.
			updateSplunkField(fields, "exceptions", -1);
			updateSplunkField(fields, "errors", -1);
			updateSplunkField(fields, "warnings", -1);
			updateSplunkField(fields, "spins", -1);
		}

		Server.sendLogInfo(LOG_NAME, "gameTestSummaryEvent", fields, false);
	}


	// This will update (or add) a field to the context that is submitted to Splunk when you call Info, Fatal, Error or Warning
	public static void updateSplunkField(Dictionary<string,string> fields, string key, string newValue)
	{
		if (fields.ContainsKey(key.ToString()))
		{
			fields[key.ToString()] = newValue;
		}
		else
		{
			fields.Add(key.ToString(), newValue);
		}
	}

	// This will update (or add) a field to the context that is submitted to Splunk when you call Info, Fatal, Error or Warning
	public static void updateSplunkField(Dictionary<string,string> fields, string key, string formatString, params object[] args)
	{
		updateSplunkField(fields, key, string.Format(formatString, args));
	}

	// This will update (or add) a field to the context that is submitted to Splunk when you call Info, Fatal, Error or Warning
	public static void updateSplunkField(Dictionary<string,string> fields, string key, System.DateTime dateTime)
	{
		updateSplunkField(fields, key, string.Format("{0:yyyy-MM-dd_hh-mm-ss.fff-tt}", dateTime));
	}

	// This will update (or add) a field to the context that is submitted to Splunk when you call Info, Fatal, Error or Warning
	public static void updateSplunkField(Dictionary<string,string> fields, string key, long newValue)
	{
		updateSplunkField(fields, key, string.Format("{0}", newValue));
	}

	// This will update (or add) a field to the context that is submitted to Splunk when you call Info, Fatal, Error or Warning
	public static void updateSplunkField(Dictionary<string,string> fields, string key, float newValue)
	{
		updateSplunkField(fields, key, string.Format("{0}", newValue));
	}

	// This will update (or add) a field to the context that is submitted to Splunk when you call Info, Fatal, Error or Warning
	public static void updateSplunkField(Dictionary<string,string> fields, string key, double newValue)
	{
		updateSplunkField(fields, key, string.Format("{0}", newValue));
	}

	// This will update (or add) a field to the context that is submitted to Splunk when you call Info, Fatal, Error or Warning
	public static void updateSplunkField(Dictionary<string,string> fields, string key, bool newValue)
	{
		updateSplunkField(fields, key, string.Format("{0}", newValue));
	}

	// Log messages are batched, this forces them to the server
	public static void ForceEventsToServer()
	{
		Server.handlePendingSplunkEvents(true);
	}
}
#endif
