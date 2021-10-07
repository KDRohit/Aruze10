using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Google.Apis.Json;

/**
Manager class to handle custom splunk events (like desync events) and to wrap the Userflow transmission
code in the same location so that all splunk events can be batched and transmitted at the same time.

NOTE: Do not attach this MonoBehaviour to anything, it will create and manage itself when
its functions are called. This class should be treated like a singleton.

Original Author: Scott Lepthien
Creation Date: 12/12/2017
*/
public class SplunkEventManager : MonoBehaviour 
{
	// Data storage for userflow session data.
	public class SplunkEventSession
	{
		public string sessionKey = null;
		public string userId = null;
		
		public SplunkEventSession()
		{
			// Intentionally left blank.
		}
	}

	// Data storage for individual userflow data.
	public class SplunkEvent
	{
		public string eventType;
		public string eventName;
		public SplunkEventSession session;
		public System.DateTime timeOccuredTimeStamp;
		public Dictionary<string, string> extraFields = null;

#if UNITY_EDITOR
		public static string[] RESTRICTED_FIELD_NAMES = new string[] {"session_key", "user_id", "target_fps", "version", "client_type", "time_occured"};
#endif

		public SplunkEvent(string eventType, string eventName, Dictionary<string, string> extraFields)
		{
			this.eventType = eventType;
			this.eventName = eventName;
			this.extraFields = extraFields;
			this.session = SplunkEventManager.currentSession;
			this.timeOccuredTimeStamp = System.DateTime.UtcNow;
		}

		// Get the fields for the splunk event
		protected Dictionary<string, string> getSplunkEventFields()
		{
			Dictionary<string, string> fields = new Dictionary<string, string>();
			
			populateUserSessionFields(fields, session);
			
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

			fields["time_occured"] = Common.dateTimeToUnixSecondsAsInt(timeOccuredTimeStamp).ToString();

#if UNITY_EDITOR
			SplunkEventManager.verifyAndMergeExtraFieldsToDefaultFields(fields, extraFields, RESTRICTED_FIELD_NAMES, eventType, eventName);
#else
			// Not going to verify restricted names on device builds to avoid log spam, 
			// hopefully these issues are caught in the editor before they get on a live device build
			SplunkEventManager.verifyAndMergeExtraFieldsToDefaultFields(fields, extraFields, null, eventType, eventName);
#endif

			return fields;
		}

		// Convert the event to the format which can be transmitted to the server
		public Dictionary<string, object> convertToTransmissionFormat()
		{
			Dictionary<string, object> splunkEventTransmissionFormat = new Dictionary<string, object>();
			splunkEventTransmissionFormat["type"] = eventType;
			splunkEventTransmissionFormat["name"] = eventName;
			splunkEventTransmissionFormat["fields"] = getSplunkEventFields();
			return splunkEventTransmissionFormat;
		}
	}
	
	// How long we wait between transmissions.
	// Set the default value 67f = 4 frames if we are running at 60 frames / second
	private const float TRANSMISSION_DELAY_DEFAULT = 67f;
	private const string TRANSMISSION_DELAY_LIVE_DATA_KEY = "SPLUNK_TRANSMISSION_DELAY";

	// Max field length of the string, enforced by the server
	public const int MAX_FIELD_LENGTH = 127;

	// Fields max length enforced by the server
	public const int MAX_FIELD_COUNT = 50;

	// Cap out how many events we will send in our batch call, if we exceed this
	// it means that there is probably some kind of transmission error occurring
	// we will just delete old unsent events to keep us under this cap until we can
	// send again
	public const int EVENT_BATCH_CAP = 150;
	
	// The current session of the game, which gets populated as needed
	public static SplunkEventSession currentSession
	{
		get
		{
			// Generate this storage object if it doesn't exist yet.
			// This is done to keep logging lightweight, since after we have these,
			// we don't need to recreate them or reallocate any memory to get them.
			if (_currentSession == null)
			{
				_currentSession = new SplunkEventSession();
			}
			
			// Make sure sessionKey is set if it can be.
			if (_currentSession.sessionKey == null)
			{
				string sessionKey = null;
				
				// Get the session key from the server if the server has one (WebGL).
				if (Data.canvasBasedConfig != null && Data.canvasBasedConfig.hasKey("USERFLOW_SESSION_KEY"))
				{
					sessionKey = Data.canvasBasedConfig.getString("USERFLOW_SESSION_KEY", "");
				}
				
				// Fallback to generating our own session key based on how many times we've asked for one.
				if (string.IsNullOrEmpty(sessionKey))
				{
					sessionKey = Glb.clientVersion + "-" + (++sessionCount).ToString() + "-" + Common.dateTimeToUnixSecondsAsDouble(System.DateTime.UtcNow).ToString("0.00");
				}
				
				// Assign, but only if there's something meaningful to assign.
				if (!string.IsNullOrEmpty(sessionKey))
				{
					_currentSession.sessionKey = sessionKey;
				}
			}
			
			// Make sure userId is set if it can be.
			if (_currentSession.userId == null || _currentSession.userId != ZdkManager.Instance.Zid.ToString())
			{
				string userId = null;
				
				// Get the user id from the server if the server has one (WebGL).
				if (Data.canvasBasedConfig != null && Data.canvasBasedConfig.hasKey("ZID"))
				{
					userId = Data.canvasBasedConfig.getString("ZID", "");
				}

				// Fallback to using the ZDK zid, which is the only option for mobile platforms.
				if (ZdkManager.Instance != null && ZdkManager.Instance.Zsession != null)
				{
					userId = ZdkManager.Instance.Zsession.Zid.ToString();
				}
				
				// Assign, but only if there's something meaningful to assign.
				if (!string.IsNullOrEmpty(userId))
				{
					_currentSession.userId = userId;
				}
			}
			
			return _currentSession;
		}
	}
	private static SplunkEventSession _currentSession = null;
	
	// Singleton reference.
	private static SplunkEventManager instance = null;

	// SplunkEventManager system state data.
	private static bool isInitialized = false;
	private static float transmissionTime = 0.0f;
	private static float? transmissionDelay = null;
	private static List<SplunkEvent> unsentSplunkEvents = null;
	private static int sessionCount = 0;
	private static List<object> pendingSplunkEventJsonQueue = null; // List of pending splunk events which are being sent, need this list so that if the SplunkEventManager is destroyed we can merge it with any new events and save them all to file

	// Any pending WWW request being processed, which is intentionally non-static.
	private WWW sendRequest = null;

	// Called externally to get everything ready for userflow tracking.
	public static void init()
	{
		if (Data.liveData != null)
		{
			transmissionDelay = Data.liveData.getFloat(TRANSMISSION_DELAY_LIVE_DATA_KEY, TRANSMISSION_DELAY_DEFAULT);
		}

		transmissionTime = 0;
		
		// If there's no singleton, make one.
		if (instance == null)
		{
			GameObject go = new GameObject();
			go.name = "SplunkEventManager";
			instance = go.AddComponent<SplunkEventManager>() as SplunkEventManager;
			DontDestroyOnLoad(go);
		}

		// init the userflows tracker
		Userflows.init(isAddingLogsTextToUserflows: false);
		
		// Check for open sessions after userflows so that the fail message is accurate.
		if (isInitialized)
		{
			sessionEnd();
		}

		if (unsentSplunkEvents == null)
		{
			unsentSplunkEvents = new List<SplunkEvent>();
		}

		if (pendingSplunkEventJsonQueue == null)
		{
			pendingSplunkEventJsonQueue = new List<object>();
		}
		
		isInitialized = true;

		// Check if we have unsent data from the last time SplunkEventManager was running,
		// and if so, try to send that out now
		string transmissionString = instance.readFullTransmissionFromFile();

		if (!string.IsNullOrEmpty(transmissionString))
		{
			// Convert what is in the transmission string into pendingSplunkEventJsonQueue so that it will handle correctly in case of errors
			List<object> savedEventList = JsonReader.Parse(transmissionString) as List<object>;

			// now that we've read the saved file and converted it into pending events, clear it out
			instance.writeFullTransmissionToFile("");

			// BY: 2019-03-07 this is unstable, I have seen it cause errors on startup periodically. After reading the logs
			// it's transmitting, half the time it's a splunk event for run_time ending which is logged in several places
			// most of the time it sends successfully. Due to this non critical component, I'm commenting it out to increase
			// stability
			//instance.sendSplunkEventBatch(savedEventList);
		}
	}

	// End a session.
	public static void sessionEnd()
	{
		if (!isInitialized)
		{
			Debug.LogError("SplunkEventManager.sessionEnd() called before init().");
			return;
		}
		
		Userflows.sessionEnd();
		
		// Clear the current session reference, a new one will automatically be recreated when needed.
		_currentSession = null;
	}

	// Add a new splunk event which will be batched and transmitted during the Update loop
	public static void createSplunkEvent(string eventType, string eventName, Dictionary<string, string> extraFields, string liveDataKey = "")
	{
		bool logIsActive = true;
		if (!string.IsNullOrEmpty(liveDataKey))
		{
			logIsActive = Data.liveData.getBool(liveDataKey, false);
		}

		if (logIsActive)
		{
			SplunkEvent splunkEvent = new SplunkEvent(eventType, eventName, extraFields);
			unsentSplunkEvents.Add(splunkEvent);
		}
	}

	// Wait for a sent request to finish and then kill the pending list of events containing what was
	// trying to be sent, if something fails then we will leave it in the pending queue and try to
	// send it later
	private IEnumerator clearPendingSplunkEventJsonQueueOnServerRequestDone()
	{
		if (sendRequest != null)
		{
			yield return sendRequest;

			if (string.IsNullOrEmpty(sendRequest.error))
			{
				// since we had a success clear anything that was in the pending queue
				pendingSplunkEventJsonQueue.Clear();
			}
		}
	}

	// Save the data that would be transmitted to file so that if 
	// it doesn't end up getting sent we can recover and send it
	// when the SplunkEventManager initializes again
	private void writeFullTransmissionToFile(string transmissionString)
	{
		string saveLocation = FileCache.path + "splunk_event_manager_transmission.txt";
		File.WriteAllText(saveLocation, transmissionString, System.Text.Encoding.UTF8);
	}

	// Reads the saved file from the last set of data that should have been transmitted
	// if the file doesn't exist it will return an empty string, and if all data was
	// fully sent before then this should also return an empty string
	private string readFullTransmissionFromFile()
	{
		string saveLocation = FileCache.path + "splunk_event_manager_transmission.txt";
		if (File.Exists(saveLocation))
		{
			return File.ReadAllText(saveLocation);
		}
		else
		{
			return "";
		}
	}

	// Send a batch of splunk events which can include stand alone events and Userflows
	private void sendSplunkEventBatch(List<object> splunkEventQueue)
	{
		string transmissionString = JSON.createJsonString(null, splunkEventQueue);
		pendingSplunkEventJsonQueue = splunkEventQueue;

		// Build the POST form.
		Dictionary<string,string> postParams = new Dictionary<string, string>();
		postParams.Add("batch", transmissionString);
		postParams.Add("client_id", (int)StatsManager.ClientID + "");

		// Player is null if this happens before player is set or right after a reset
		if (SlotsPlayer.instance != null && SlotsPlayer.instance.socialMember != null)
		{
			postParams.Add("sc_id", SlotsPlayer.instance.socialMember.zId);
		}
		if (!string.IsNullOrEmpty(Glb.logEventUrl))
		{
			sendRequest = Server.getRequestWWW(Glb.logEventUrl, postParams);

			// Create a coroutine to wait on the request and clear the save file on success
			StartCoroutine(clearPendingSplunkEventJsonQueueOnServerRequestDone());
		}
	}

	// Use OnDestroy to catch if we have untransmitted data and save it out to file
	private void OnDestroy()
	{
		// merge pendingSplunkEventJsonQueue and any new userflows and unsent events and save to file
		// Get any Userflows that need to be transmitted
		List<object> splunkEventQueue = Userflows.staticGetUserflowTransmissionList();

		// Add any splunk events which aren't sent
		if (unsentSplunkEvents != null)
		{
			for (int i = 0; i < unsentSplunkEvents.Count; i++)
			{
				splunkEventQueue.Add(unsentSplunkEvents[i].convertToTransmissionFormat());
			}

			// Clear the unsent splunk event list since we are about to send them
			unsentSplunkEvents.Clear();
		}

		// Check if we have any queued splunkEvent data that wasn't sent due to an error the last time we tried sending
		if (pendingSplunkEventJsonQueue != null)
		{
			if (pendingSplunkEventJsonQueue.Count > 0)
			{
				// merge the previous pending ones into this one
				for (int i = 0; i < pendingSplunkEventJsonQueue.Count; i++)
				{
					splunkEventQueue.Add(pendingSplunkEventJsonQueue[i]);
				}
			}
		}

		string transmissionString = JSON.createJsonString(null, splunkEventQueue);
		writeFullTransmissionToFile(transmissionString);

		if (instance == this)
		{
			instance = null;
		}
	}

	// Check that extraFields will not cause an event to exceed MAX_FIELD_COUNT
	// and if in the editor that we aren't using a restricted field name for a extra field
	public static Dictionary<string, string> verifyAndMergeExtraFieldsToDefaultFields(Dictionary<string, string> fields, Dictionary<string, string> extraFields, string[] restrictedNames, string eventType, string eventName)
	{
		// check if fields has already exceeded the MAX_FIELD_COUNT, in which case we are
		// kinda screwed, so log an error and let it send it to the server which I assume will also error
		// add one to account for the field_overflow which might be added when dealing with extraFields
		if (fields.Count + 1 > SplunkEventManager.MAX_FIELD_COUNT)
		{
			Debug.LogError("SplunkEventManager.verifyAndMergeExtraFieldsToDefaultFields() - Default fields is exceeding the MAX_FIELD_COUNT of: " + SplunkEventManager.MAX_FIELD_COUNT + ".  This will cause issues!");
		}

		if (extraFields != null)
		{
			// check to see if adding the extraFields section will excceed the server limit for fields
			if (fields.Count + extraFields.Count > SplunkEventManager.MAX_FIELD_COUNT)
			{
				Debug.LogError("SplunkEventManager.verifyAndMergeExtraFieldsToDefaultFields() - extraFields of eventType = " + eventType + "; eventName = " + eventName + " will cause field length to be: " + (fields.Count + extraFields.Count) + " which exceeds server limit of MAX_FIELD_COUNT = " + SplunkEventManager.MAX_FIELD_COUNT);

				// we are over the count, so remove fields until we are under, 
				// and add a field_overflow field so we can mark that we are over the limit
				List<string> keyList = new List<string>(extraFields.Keys);
				while (extraFields.Count > 0 && (fields.Count + extraFields.Count > SplunkEventManager.MAX_FIELD_COUNT - 1))
				{
					extraFields.Remove(keyList[extraFields.Count - 1]);
				}

				extraFields.Add("field_overflow", "true");
			}

			foreach (KeyValuePair<string, string> kvp in extraFields)
			{
#if UNITY_EDITOR
				if (restrictedNames != null && SplunkEventManager.isRestrictedFieldName(kvp.Key, restrictedNames))
				{
					Debug.LogError("SplunkEventManager.verifyAndMergeExtraFieldsToDefaultFields() - extraFields included entry in RESTRICTED_FIELD_NAMES: " + kvp.Key + ". Ignoring it.");
					continue;
				}
#endif
				fields.Add(kvp.Key, kvp.Value);
			}
		}

		return fields;
	}

#if UNITY_EDITOR
	// Check if the passed field name is in the list of restricted names which is passed to this function
	private static bool isRestrictedFieldName(string name, string[] restrictedNames)
	{
		for (int i = 0; i < restrictedNames.Length; i++)
		{
			if (restrictedNames[i] == name)
			{
				return true;
			}
		}

		return false;
	}
#endif

	// Standard Update() method on a MonoBehaviour, used for performance tracking.
	// This is automatically called once per frame, and thereby updates all active
	// userflows once per frame, in order to collect performance metrics centric to
	// each userflow.
	private void Update()
	{
		if (instance != this)
		{
			// Safety check to avoid issues if somehow the singleton pattern is broken.
			Destroy(gameObject);
			Debug.LogError("SplunkEventManager.Update() : Singleton pattern violated, there should only ever be one instance of Userflows.");
			return;
		}

		// Update the Userflows
		Userflows.staticUpdate();

		if (!transmissionDelay.HasValue && Data.liveData != null)
		{
			transmissionDelay = Data.liveData.getFloat(TRANSMISSION_DELAY_LIVE_DATA_KEY, TRANSMISSION_DELAY_DEFAULT);
		}

		if (Glb.logEventUrl != null &&
			Time.realtimeSinceStartup >= transmissionTime &&
			(sendRequest == null || sendRequest.isDone))
		{
			// Get any Userflows that need to be transmitted
			List<object> splunkEventQueue = Userflows.staticGetUserflowTransmissionList();

			// Add any splunk events which aren't sent
			for (int i = 0; i < unsentSplunkEvents.Count; i++)
			{
				splunkEventQueue.Add(unsentSplunkEvents[i].convertToTransmissionFormat());
			}

			// Clear the unsent splunk event list since we are about to send them
			unsentSplunkEvents.Clear();

			// Check if we have any queued splunkEvent data that wasn't sent due to an error the last time we tried sending
			if (pendingSplunkEventJsonQueue.Count > 0)
			{
				// if we are going to exceed our cap for how many events we will batch
				// we will just delete this old pending events until we will be under
				// the cap (this should only happen if their are transmission issues sending the events
				if (splunkEventQueue.Count + pendingSplunkEventJsonQueue.Count > EVENT_BATCH_CAP)
				{
					while (pendingSplunkEventJsonQueue.Count > 0 && (splunkEventQueue.Count + pendingSplunkEventJsonQueue.Count > EVENT_BATCH_CAP))
					{
						// remove the front so we get rid of the oldest events first
						pendingSplunkEventJsonQueue.RemoveAt(0);
					}
				} 

				// merge the previous pending ones into this one
				for (int i = 0; i < pendingSplunkEventJsonQueue.Count; i++)
				{
					splunkEventQueue.Add(pendingSplunkEventJsonQueue[i]);
				}
			}
				
			// If we have splunk event data to send, send it.
			if (splunkEventQueue.Count > 0)
			{
				sendSplunkEventBatch(splunkEventQueue);
			}

			float currentTransmissionDelay = transmissionDelay.HasValue ? transmissionDelay.Value : TRANSMISSION_DELAY_DEFAULT;
			transmissionTime = Time.realtimeSinceStartup + currentTransmissionDelay;
		}
	}
	
	// Populate common session and user id fields with fallback logic
	public static void populateUserSessionFields(Dictionary<string, string> fields, SplunkEventSession session)
	{
		if (session == null)
		{
			// Shouldn't happen, but if it does let's make sure to be able to know about it.
			fields["session_key"] = "lost-session";
			
			// Grab the current userId, even though it might not be the userId where it happened.
			if (currentSession.userId != null)
			{
				fields["user_id"] = currentSession.userId;
			}
		}
		else
		{
			if (session.sessionKey != null)
			{
				fields["session_key"] = session.sessionKey;
			}
		
			if (session.userId != null)
			{
				fields["user_id"] = session.userId;
			}
		}
	}
}
