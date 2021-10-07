using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Do not attach this MonoBehaviour to anything, it will create and manage itself when
// its functions are called. This class should be treated like a singleton.
/**
Class for handling the Userflow Splunk events we send and record
Transmission is handled by SplunkEventManager which handles any type of Splunk event

Original Author: Jon Sylvan
*/
public class Userflows
{
	// Data storage for messages within a userflow.
	public class UserflowLog
	{
		public string message;
		public System.DateTime timestamp;
		
		public UserflowLog(string message)
		{
			this.message = message;
			timestamp = System.DateTime.UtcNow;
		}
	}
	
	// Data storage for individual userflow data.
	public class Userflow
	{
		public string flowKey;
		public SplunkEventManager.SplunkEventSession session;
		public int flowNumber;
		public bool isRecorded;
		public bool isSuccessful;
		public bool didStartDuringInitialLoading;
		public bool didEndDuringInitialLoading;
		public bool didPause;
		public string outcomeType;
		public Dictionary<string, string> extraFields = null;
		
		public System.DateTime startTimeStamp;
		public System.DateTime endTimeStamp;
		public float duration;
		public float frameRate;
		public int sampleCount;
		
		public float frameTimeMax;
		public float frameTimeSum;
		public float frameTimeMean;
		public float memoryMax;
		public float memorySum;
		public float memoryMean;
		
		public List<UserflowLog> steps;
		public List<UserflowLog> logs;
		public List<UserflowLog> warnings;
		public List<UserflowLog> errors;
		
		public string stepsSummary;
		public string logsSummary;
		public string warningsSummary;
		public string errorsSummary;
		
		// Constructor.
		public Userflow(string flowKey, int flowNumber, bool isRecorded)
		{
			this.flowKey = flowKey;
			this.session = SplunkEventManager.currentSession;
			this.flowNumber = flowNumber;
			this.isRecorded = isRecorded;
			isSuccessful = false;
			didStartDuringInitialLoading = !Userflows.isDoneWithInitialLoading;
			didEndDuringInitialLoading = false;
			didPause = false;
			outcomeType = "";
			
			startTimeStamp = System.DateTime.UtcNow;
			
			sampleCount = 0;
			
			frameTimeMax = 0.0f;
			frameTimeSum = 0.0f;
			memoryMax = 0.0f;
			memorySum = 0.0f;
			
			steps = new List<UserflowLog>();
			logs = new List<UserflowLog>();
			warnings = new List<UserflowLog>();
			errors = new List<UserflowLog>();
		}
		
		// Called once per Update() on the Userflows singleton MonoBehaviour.
		public void update(float time, float memory)
		{
			sampleCount++;
			frameTimeMax = Mathf.Max(frameTimeMax, time);
			frameTimeSum += time;
			memoryMax = Mathf.Max(memoryMax, memory);
			memorySum += memory;
		}
		
		// Called at the end of a userflow to finalize data calculations.
		public void calculateEnd(bool isSuccess, string outcome)
		{
			endTimeStamp = System.DateTime.UtcNow;
			float samples = (float)sampleCount;
			duration = (float)(endTimeStamp - startTimeStamp).TotalSeconds;
			
			if (duration <= 0.01f)
			{
				frameRate = 0.0f;
			}
			else
			{
				frameRate = samples / duration;
			}
			
			if (samples <= 1.0f)
			{
				frameTimeMean = 0.0f;
				memoryMean = memoryMax;
			}
			else
			{
				frameTimeMean = frameTimeSum / samples;
				memoryMean = memorySum / samples;
			}
			
			stepsSummary = summarizeLogs(steps);

			if (isIncludingLogsText)
			{
				logsSummary = summarizeLogs(logs);
				warningsSummary = summarizeLogs(warnings);
				errorsSummary = summarizeLogs(errors);
			}
			
			didEndDuringInitialLoading = !Userflows.isDoneWithInitialLoading;
			isSuccessful = isSuccess;
			outcomeType = outcome;
		}
		
		// Turns a list of logs into a single string that is no more than 128 characters long.
		// The string formats messages in the form of a comma delimited list of timestamp+message tuples.
		private string summarizeLogs(List<UserflowLog> fullLogs)
		{
			if (fullLogs.Count == 0)
			{
				return null;
			}
			
			System.Text.StringBuilder summary = new System.Text.StringBuilder();
			
			foreach(UserflowLog log in fullLogs) 
			{
				string relativeTimestamp = (log.timestamp - startTimeStamp).TotalSeconds.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
				summary.AppendFormat("{0} {1}, ", relativeTimestamp, log.message);
				
				// If we already know we are too long, bail here.
				if (summary.Length > SplunkEventManager.MAX_FIELD_LENGTH)
				{
					break;
				}
			}
			
			// Get rid of trailing comma and space.
			if (summary.Length > 2)
			{
				summary.Length = summary.Length - 2;
			}
			
			// Check if this is too long and truncate with indicator.
			if (summary.Length > SplunkEventManager.MAX_FIELD_LENGTH)
			{
				summary.Length = SplunkEventManager.MAX_FIELD_LENGTH - 3;
				summary.Append("...");
			}
			
			return summary.ToString();
		}
	}

#if UNITY_EDITOR
	public static string[] RESTRICTED_FIELD_NAMES = new string[] {"flow_key", "flow_state", "flow_number", "session_key", "user_id", 
																"start_time", "end_time", "duration", "samples", "mem_max", 
																"mem_mean", "frame_max", "frame_mean", "fps", "step_count", 
																"log_count", "warning_count", "error_count", "steps", "logs", 
																"warnings", "errors", "target_fps", "version", "client_type", 
																"success", "outcome", "initial_load", "was_paused",
																"device_model_name", "webgl_dynamic_memory", "webgl_reserved_memory"};
#endif
	
	// Some strings that get used for messaging in logs.
	private const string MSG_MISSING_ENDFLOW = "missing_end_flow";
	private const string MSG_REINITIALIZED = "reinitialized";
	private const string MSG_SESSION_ENDED = "session_ended";
	
	// Singleton reference.
	private static Userflows instance = null;
	
	// Userflow system state data.
	private static Dictionary<string, Userflow> userflows = null;
	private static Dictionary<string, float> samplings = null;
	private static float previousTime = 0.0f;
	private static int userflowCount = 0;
	private static bool isSimulatedMode = false;
	private static bool isInitialized = false;
	private static bool isIncludingLogsText = true;
	private static List<Userflow> startedUserflows = null;
	private static List<Userflow> completedUserflows = null;
	
	// Mutex to prevent infinite loop logging as a result of bad use of this class by others.
	private static bool logMutex = false;
	
	// Flag to specifically mark all userflows as initial_load=true until told otherwise.
	public static bool isDoneWithInitialLoading { get; private set; }
	
	public static void finishedInitialLoading()
	{
		isDoneWithInitialLoading = true;
	}

	public static bool isUserflowActive(string flowKey)
	{
		return userflows != null && userflows.ContainsKey(flowKey);
	}
	
	// Called externally to get everything ready for userflow tracking.
	public static void init(bool isAddingLogsTextToUserflows)
	{
		isDoneWithInitialLoading = false;
		isSimulatedMode = false;
		logMutex = false;
		isIncludingLogsText = isAddingLogsTextToUserflows;
		previousTime = Time.realtimeSinceStartup;
		
		// If there's no singleton, make one.
		if (instance == null)
		{
			instance = new Userflows();
		}
		
		// Check for open userflows before sessions so that the fail message is accurate.
		if (userflows == null)
		{
			userflows = new Dictionary<string, Userflow>();
		}
		else
		{
			// Fail all userflows that aren't done yet.
			List<string> toRemove = new List<string>();
			foreach (string key in userflows.Keys)
			{
				toRemove.Add(key);
			}
			foreach (string key in toRemove)
			{
				// End the flow with failure indicated and a reinitialized message.
				flowEnd(key, false, MSG_REINITIALIZED);
			}
		}
		
		// Fail out any old userflows from a previous session.
		if (isInitialized)
		{
			sessionEnd();
		}
		
		// Make sure we have sampling data, but don't reset it in case there were overrides.
		// The assumption is we want the most recent overrides at all times.
		if (samplings == null)
		{
			samplings = new Dictionary<string, float>();
		}
		
		// Make sure we have a started list, but don't reset it in case there is unsent data.
		if (startedUserflows == null)
		{
			startedUserflows = new List<Userflow>();
		}
		
		// Make sure we have a completed list, but don't reset it in case there is unsent data.
		if (completedUserflows == null)
		{
			completedUserflows = new List<Userflow>();
		}
		
		isInitialized = true;
	}
	
	public static void setSimulatedMode(bool simulate)
	{
		isSimulatedMode = simulate;
	}
	
	// End a session, arking all incomplete userflows as failed.
	public static void sessionEnd()
	{
		if (!isInitialized)
		{
			Debug.LogError("Userflows.sessionEnd() called before init().");
			return;
		}
		
		// Fail all userflows with this session that aren't ended yet.
		List<string> toRemove = new List<string>();
		foreach (Userflow userflow in userflows.Values)
		{
			toRemove.Add(userflow.flowKey);
		}
		foreach (string key in toRemove)
		{
			// End the flow with failure indicated and a session ended message.
			flowEnd(key, false, MSG_SESSION_ENDED);
		}
	}

	private static bool shouldRecordFlow(string flowKey, float samplingRatio)
	{
		// Check to see if we should be transmitting the data from this flow.
		// Sampling can be necessary for high-volume user flows.
		// Absent of an override or preset, record this userflow.
		bool shouldRecord = true;
		
		if (isSimulatedMode)
		{
			shouldRecord = false;
		}
		else if (samplings.ContainsKey(flowKey))
		{
			// Record based on preset sampling value, which is server driven
			// and therefore overrides any programmatic override value.
			shouldRecord = Random.value <= samplings[flowKey];
		}
		else if (!float.IsNaN(samplingRatio))
		{
			// Record based on provided override sampling.
			shouldRecord = Random.value <= samplingRatio;
		}

		return shouldRecord;
	}
	
	// Start a userflow, optionally provide override sampling criteria.
	public static void flowStart(string flowKey, float samplingRatio = float.NaN)
	{
		if (!isInitialized)
		{
			Debug.LogError("Userflows.flowStart() called before init().");
			return;
		}
		
		// If this flow was previously started and not ended, fail the previous instance.
		if (userflows.ContainsKey(flowKey))
		{
#if UNITY_EDITOR
			Debug.LogError("Userflows.flowStart() - flowKey = " + flowKey + "; was missing an end flow call!");
#endif
			flowEnd(flowKey, false, MSG_MISSING_ENDFLOW);
		}
		
		// Create the new userflow instance.
		bool isRecorded = shouldRecordFlow(flowKey, samplingRatio);
		Userflow userflow = new Userflow(flowKey, ++userflowCount, isRecorded);
		userflows.Add(flowKey, userflow);
		
		// Queue userflow start data to write out.
		if (userflow.isRecorded)
		{
			startedUserflows.Add(userflow);
		}
	}
	
	// End a userflow, optionally noting success/failure and some outcome string.
	public static Userflow flowEnd(string flowKey, bool isSuccess = true, string outcome = "")
	{
		if (!isInitialized)
		{
			Debug.LogError("Userflows.flowEnd() called before init().");
			return null;
		}
		
		// Silently end if ending a non-existent userflow (could have been auto-failed).
		if (!userflows.ContainsKey(flowKey))
		{
			return null;
		}
		
		// Queue userflow end data to write out, which includes performance metrics.
		Userflow userflow = userflows[flowKey];
		if (userflow.isRecorded)
		{
			userflow.calculateEnd(isSuccess, outcome);
			completedUserflows.Add(userflow);
		}
		
		userflows.Remove(flowKey);

		return userflow;
	}

	// Append additional fields to a currently tracked flow
	public static void addExtraFieldsToFlow(string flowKey, Dictionary<string, string> extraFields)
	{
		if (!isInitialized || !userflows.ContainsKey(flowKey))
		{
			return;
		}

		Userflow userflow = userflows[flowKey];
		userflow.extraFields = CommonDataStructures.mergeDictionary(userflow.extraFields, extraFields);
	}

	// Append additional fields to a currently tracked flow
	public static void addExtraFieldToFlow(string flowKey, string key, string value)
	{
		if (!isInitialized || !userflows.ContainsKey(flowKey))
		{
			return;
		}

		Userflow userflow = userflows[flowKey];

		if (userflow.extraFields == null)
		{
			userflow.extraFields = new Dictionary<string, string>();
		}
		if (userflow.extraFields.ContainsKey(key))
		{
			Debug.LogWarningFormat("Userflow already contains a value for {0} taking newest value.", key);
		}
		userflow.extraFields[key] = value;
	}
	
	// Set the transmission sampling level for userflows with this key.
	// Sampling is a ratio between 0 (never transmit) and 1 (always transmit).
	public static void setFlowSampling(string flowKey, float samplingRatio)
	{
		if (!isInitialized)
		{
			Debug.LogError("Userflows.setFlowSampling() called before init().");
			return;
		}
		
		if (samplings.ContainsKey(flowKey))
		{
			samplings[flowKey] = Mathf.Clamp01(samplingRatio);
		}
		else
		{
			samplings.Add(flowKey, Mathf.Clamp01(samplingRatio));
		}
	}
	
	// Log a step in a userflow. Steps are used for funnel creation within a userflow.
	public static void logStep(string step, string flowKey)
	{
		if (logMutex)
		{
			return;
		}
		logMutex = true;
		
		if (!isInitialized)
		{
			Debug.LogError("Userflows.setFlowSampling() called before init().");
			logMutex = false;
			return;
		}
		
#if UNITY_EDITOR
		if (step.Length > 20)
		{
			Debug.LogErrorFormat("Step ids should be less than 20 characters. Note that event logger has a maximum of 128 characters to fit all steps. '{0}'.", step);
		}
#endif

		if (userflows.ContainsKey(flowKey))
		{
			userflows[flowKey].steps.Add(new UserflowLog(step));
		}
		
		logMutex = false;
	}
	
	// Log a non-threatening message.
	public static void logMessage(string message, string flowKey = null)
	{
		if (logMutex)
		{
			return;
		}
		logMutex = true;
		
		if (!isInitialized)
		{
			Debug.LogError("Userflows.setFlowSampling() called before init().");
			logMutex = false;
			return;
		}
		
		UserflowLog log = new UserflowLog(message);
		if (string.IsNullOrEmpty(flowKey))
		{
			foreach (Userflow userflow in userflows.Values)
			{
				userflow.logs.Add(log);
			}
		}
		else if (userflows.ContainsKey(flowKey))
		{
			userflows[flowKey].logs.Add(log);
		}
		
		logMutex = false;
	}
	
	// Log a warning message.
	public static void logWarning(string message, string flowKey = null)
	{
		if (logMutex)
		{
			return;
		}
		logMutex = true;
		
		if (!isInitialized)
		{
			Debug.LogError("Userflows.setFlowSampling() called before init().");
			logMutex = false;
			return;
		}
		
		UserflowLog log = new UserflowLog(message);
		if (string.IsNullOrEmpty(flowKey))
		{
			foreach (Userflow userflow in userflows.Values)
			{
				userflow.warnings.Add(log);
			}
		}
		else if (userflows.ContainsKey(flowKey))
		{
			userflows[flowKey].warnings.Add(log);
		}
		
		logMutex = false;
	}
	
	// Log an error message.
	public static void logError(string message, string flowKey = null)
	{
		if (logMutex)
		{
			return;
		}
		logMutex = true;
		
		if (!isInitialized)
		{
			Debug.LogError("Userflows.setFlowSampling() called before init().");
			logMutex = false;
			return;
		}
		
		UserflowLog log = new UserflowLog(message);
		if (string.IsNullOrEmpty(flowKey))
		{
			foreach (Userflow userflow in userflows.Values)
			{
				userflow.errors.Add(log);
			}
		}
		else if (userflows.ContainsKey(flowKey))
		{
			userflows[flowKey].errors.Add(log);
		}
		
		logMutex = false;
	}
	
	// When the application pauses/suspends make sure every userflow in progress notes it.
	public static void notePauseOccurred()
	{
		if (userflows != null)
		{
			foreach (Userflow userflow in userflows.Values)
			{
				userflow.didPause = true;
			}
		}
	}

	// Handle simple update of userflow data
	public static void staticUpdate()
	{
		if (!isInitialized)
		{
			Debug.LogError("Userflows.staticUpdate() called before init().");
			return;
		}

		instance.update();
	}

	// Handle simple update of userflow data
	private void update()
	{
		float timeSinceStart = Time.realtimeSinceStartup;
		float time = timeSinceStart - previousTime;
		previousTime = timeSinceStart;
		
		// Zynga.Core.UnityUtil.DeviceInfo.CurrentMemoryMB is SLOW on Android (calls into Java, takes ~ 35 ms!)
		// So use the value that was sampled during the last cleanupMemoryAsync 
		float memory = Glb.memoryMBAtLastCleanup;
		if (memory < 1.0f)
		{
			// Memory readout using ZDK was bad, fallback attempted.
			memory = (float)(MemoryHelper.GetMemoryResidentBytes()) / (1024.0f * 1024.0f);
		}

		foreach (Userflow userflow in userflows.Values)
		{
			userflow.update(time, memory);
		}
	}

	// Get the list of transmittable Userflow events, SplunkEventManager will combine
	// these with any other events it has and then transmit everything together
	public static List<object> staticGetUserflowTransmissionList()
	{
		if (!isInitialized)
		{
			Debug.LogError("Userflows.staticGetUserflowTransmissionList() called before init().");
			return new List<object>();
		}

		return instance.getUserflowTransmissionList();
	}

	// Get the list of transmittable Userflow events, SplunkEventManager will combine
	// these with any other events it has and then transmit everything together
	private List<object> getUserflowTransmissionList()
	{
		List<object> splunkEventQueue = new List<object>();
			
		// Event logger supports up to 30 fields of up to 128 character length each.
			
		// Gather up all started userflows that haven't been sent.
		foreach (Userflow userflow in startedUserflows)
		{
			Dictionary<string, string> fields = new Dictionary<string, string>();
			fields["flow_key"] = userflow.flowKey;
			fields["flow_state"] = "start";
			fields["flow_number"] = userflow.flowNumber.ToString();
			
			SplunkEventManager.populateUserSessionFields(fields, userflow.session);
			
			fields["start_time"] = Common.dateTimeToUnixSecondsAsDouble(userflow.startTimeStamp).ToString("0.00");
#if !UNITY_WEBGL
			fields["target_fps"] = Application.targetFrameRate.ToString();
#endif
			fields["version"] = Glb.clientVersion;
			fields["client_type"] =
#if UNITY_ANDROID
#if ZYNGA_KINDLE
				"kindle";
#else
				"android";
#endif	// !ZYNGA_KINDLE
#elif UNITY_IPHONE
				"ios";
#elif UNITY_WSA_10_0
				"windows";
#elif UNITY_WEBGL
				"webgl";
#else
				"unity";
#endif

#if UNITY_EDITOR
			SplunkEventManager.verifyAndMergeExtraFieldsToDefaultFields(fields, userflow.extraFields, RESTRICTED_FIELD_NAMES, "Userflow", userflow.flowKey);
#else
			// Not going to verify restricted names on device builds to avoid log spam, hopefully these issues are caught in the editor before they get on a live device build
			SplunkEventManager.verifyAndMergeExtraFieldsToDefaultFields(fields, userflow.extraFields, null, "Userflow", userflow.flowKey);
#endif
				
			Dictionary<string, object> splunkEvent = new Dictionary<string, object>();
			splunkEvent["type"] = "Userflow";
			splunkEvent["name"] = "start-" + userflow.flowKey;
			splunkEvent["fields"] = fields;
			splunkEventQueue.Add(splunkEvent);
		}
	
		// Gather up all completed userflows that haven't been sent.
		foreach (Userflow userflow in completedUserflows)
		{
			Dictionary<string, string> fields = new Dictionary<string, string>();
			fields["flow_key"] = userflow.flowKey;
			fields["flow_state"] = "end";
			fields["flow_number"] = userflow.flowNumber.ToString();
			
			SplunkEventManager.populateUserSessionFields(fields, userflow.session);
			fields["device_model_name"] = StatsManager.DeviceModelNameInQuotes;
			
			fields["success"] = userflow.isSuccessful ? "true" : "false";
				
			if (!string.IsNullOrEmpty(userflow.outcomeType))
			{
				fields["outcome"] = userflow.outcomeType;
			}
			fields["start_time"] = Common.dateTimeToUnixSecondsAsDouble(userflow.startTimeStamp).ToString("0.00");
			fields["end_time"] = Common.dateTimeToUnixSecondsAsDouble(userflow.endTimeStamp).ToString("0.00");
			fields["duration"] = userflow.duration.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
			fields["samples"] = userflow.sampleCount.ToString();
			
			fields["was_paused"] = userflow.didPause ? "true" : "false";
			
			// Only include memory if the values are meaningful
			if (userflow.memoryMax > 1.0f)
			{
				fields["mem_max"] = userflow.memoryMax.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
			}
			if (userflow.memoryMean > 1.0f)
			{
				fields["mem_mean"] = userflow.memoryMean.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
			}
			
#if UNITY_WEBGL
			if (MemoryHelper.totalReservedMemorySize > 1.0f)
			{
				fields["webgl_reserved_memory"] = MemoryHelper.totalReservedMemorySize.ToString();
			}

			if (MemoryHelper.dynamicMemorySize > 1.0f)
			{
				fields["webgl_dynamic_memory"] = MemoryHelper.dynamicMemorySize.ToString();
			}
#endif	
			fields["frame_max"] = userflow.frameTimeMax.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
			fields["frame_mean"] = userflow.frameTimeMean.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
			fields["fps"] = userflow.frameRate.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
				
			fields["step_count"] = userflow.steps.Count.ToString();
			fields["log_count"] = userflow.logs.Count.ToString();
			fields["warning_count"] = userflow.warnings.Count.ToString();
			fields["error_count"] = userflow.errors.Count.ToString();

				
			if (userflow.stepsSummary != null)
			{
				fields["steps"] = userflow.stepsSummary;
			}

			if (userflow.logsSummary != null)
			{
				fields["logs"] = userflow.logsSummary;
			}

			if (userflow.warningsSummary != null)
			{
				fields["warnings"] = userflow.warningsSummary;
			}

			if (userflow.errorsSummary != null)
			{
				fields["errors"] = userflow.errorsSummary;
			}

#if !UNITY_WEBGL
			fields["target_fps"] = Application.targetFrameRate.ToString();
#endif
			fields["version"] = Glb.clientVersion;
			fields["client_type"] =
#if UNITY_ANDROID
#if ZYNGA_KINDLE
				"kindle";
#else
				"android";
#endif	// !ZYNGA_KINDLE
#elif UNITY_IPHONE
				"ios";
#elif UNITY_WSA_10_0
				"windows";
#elif UNITY_WEBGL
				"webgl";
#else
				"unity";
#endif
			if (userflow.didStartDuringInitialLoading)
			{
				if (userflow.didEndDuringInitialLoading)
				{
					fields["initial_load"] = "true";
				}
				else
				{
					fields["initial_load"] = "after";
				}
			}
			else
			{
				fields["initial_load"] = "false";
			}

#if UNITY_EDITOR
			SplunkEventManager.verifyAndMergeExtraFieldsToDefaultFields(fields, userflow.extraFields, RESTRICTED_FIELD_NAMES, "Userflow", userflow.flowKey);
#else
			SplunkEventManager.verifyAndMergeExtraFieldsToDefaultFields(fields, userflow.extraFields, null, "Userflow", userflow.flowKey);
#endif
	
			Dictionary<string, object> splunkEvent = new Dictionary<string, object>();
			splunkEvent["type"] = "Userflow";
			splunkEvent["name"] = "end-" + userflow.flowKey;
			splunkEvent["fields"] = fields;
			splunkEventQueue.Add(splunkEvent);
		}

		// Clear out userflow lists for the data that is about to be transmitted
		startedUserflows.Clear();
		completedUserflows.Clear();

		return splunkEventQueue;
	}

	// Special-case userflow logging events that we send to JS to track pre-client loading-webgl
	public static void logWebGlLoadingStep(string flowState)
	{
		Debug.Assert( !flowState.Contains('-'), "Dashes not allowed in loading-webgl flowstate names: " + flowState);

#if UNITY_WEBGL
		if (webGLFlowStatesSeen.Add(flowState))
		{
			Debug.Log("logWebGlLoadingStep: emitting flowstate = " + flowState);
			WebGLFunctions.emitJSFlowEvent("loading-webgl", flowState, "0", Glb.clientVersion);
		}
		else
		{
			Debug.LogWarning("logWebGlLoadingStep: ignoring duplicate flowState = " + flowState); 
		}
#endif
	}
	private static HashSet<string> webGLFlowStatesSeen = new HashSet<string>(); // to catch redundant states

}
