using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/* 
 * Server action class for handling all the network profile actions
 */

public class NetworkProfileAction : ServerAction
{

	/****** Action Names *****/
	public const string UPDATE_PROFILE = "update_profile";
	public const string GET_PROFILE = "get_profile";
	public const string MULTI_GET_PROFILE = "multi_get_profile";
	public const string MULTI_GET_PROFILE_FB = "multi_get_profile_fb";
	public const string REPORT_PROFILE = "report_profile";
	public const string GET_PROFILE_AVATARS = "get_profile_avatars";
	public const string RESET_PROFILE = "reset_profile";
	/****** End Action Names *****/


	/****** Action Variables *****/
	private string networkId = ""; // The network id for profile actions.
	private string targetZid = ""; // The zid for profile actions.
	private string fbid = ""; // The fbId for the profile actions.
	private List<string> zids; // List of zids for multi_get_profile
	private List<string> fbids; // List of fbIds for the multi_get_profile.
	private List<string> fields; // The list of profile fields we want sent down to us.
	private Dictionary<string, string> profileUpdates; // Map of profile udpates we are sending up.
	private string reportedId = ""; // The id of the profile we are reporting.
	private string reason = ""; // The reason we are reporting a profile.
	private List<int> reportCategories; // The list of categories selected when reporting a profile.
	private string reportedField = ""; // The specific area of the profile the user is reporting. (name/status/location)
	public enum ReportField
	{
		NAME = 0,
		STATUS = 1,
		LOCATION = 2,
		PHOTO = 3
	}

	public enum ReportCategory
	{
		OTHER = 0,
		NUDITY = 1,
		HATE_SPEECH = 2,
		IMPERSONATION = 3,
		ILLEGAL = 4,
		SPAM = 5,
		SEXUALLY_EXPLICIT = 6
	}

	/***** End Action Variables *****/

	/***** Property Names *****/
	private const string NETWORK_ID = "network_id";
	private const string TARGET_ZID = "target_zid";
	private const string FB_ID = "fb_id";
	private const string FB_IDS = "fb_ids";
	private const string ZIDS = "zids";
	private const string FIELDS = "fields";
	private const string UPDATES = "updates";
	private const string REPORTED_ID = "reported_id";
	private const string REASON = "reason";
	private const string CATEGORY_CODES = "category_codes";
	private const string REPORTED_FIELD = "reported_field";	
	/***** End Property Names *****/


#if UNITY_EDITOR
	public static int multiGetProfileRequests = 0;
	public static int getProfileRequests = 0;
#endif
	
	/** Constructor */
	private NetworkProfileAction(ActionPriority priority, string type) : base(priority, type) {}

	/****** Static Methods *****/
	public static void getProfile(SocialMember member, EventDelegate callback)
	{
		if (string.IsNullOrEmpty(member.id) && string.IsNullOrEmpty(member.zId) && string.IsNullOrEmpty(member.networkID))
		{
			Debug.LogErrorFormat("NetworkProfileAction.cs -- getProfile -- could not get a networkId, zid or fbid from the socialmember, so we can't do shit with this user.");
			return;
		}
		
		NetworkProfileAction action = new NetworkProfileAction(ActionPriority.HIGH, GET_PROFILE);
		if (string.IsNullOrEmpty(member.networkID) || member.networkID == "-1")
		{
			action.networkId = "";
		}
		else
		{
			action.networkId = member.networkID;
		}

		if (string.IsNullOrEmpty(member.zId) || member.zId == "-1")
		{
			action.targetZid = "";		
		}
		else
		{
			action.targetZid = member.zId;			
		}

		if (string.IsNullOrEmpty(member.id) || member.id == "-1")
		{
			action.fbid = "";
		}
		else
		{
			action.fbid = member.id;
		}

		if (callback == null)
		{
			// If no callback is specified, then yell about it.
			Debug.LogWarningFormat("NetworkProfileAction.cs -- getProfile -- you provided a null callback, are you sure about that?");
		}
		else
		{
			Server.registerEventDelegate("profile_data", callback, false);
		}
#if UNITY_EDITOR
		getProfileRequests++;
#endif
		
		ServerAction.setFastUpdateMode("profile_data");
		ServerAction.processPendingActions(true);
	}

	public static void getProfilesFromZids(List<string> zids, EventDelegate callback = null, bool isPersistant = false)
	{
		NetworkProfileAction action = new NetworkProfileAction(ActionPriority.IMMEDIATE, MULTI_GET_PROFILE);
		action.zids = zids;

		if (callback != null)
		{
			Server.registerEventDelegate("multi_profile_data", callback, isPersistant);
		}
#if UNITY_EDITOR
		multiGetProfileRequests++;
#endif
		ServerAction.processPendingActions(true);
	}

	public static void updateProfile(string networkId, Dictionary<string, string> profileUpdates, EventDelegate callback)
	{
		NetworkProfileAction action = new NetworkProfileAction(ActionPriority.HIGH, UPDATE_PROFILE);
		action.networkId = networkId;
		action.profileUpdates = profileUpdates;
		Server.registerEventDelegate("update_profile", callback);
		ServerAction.processPendingActions(true);
	}

	public static void reportProfile(string networkId, string reason, ReportField reportedField, ReportCategory category, EventDelegate callback = null)
	{
		reportProfile(networkId, reason, reportedField, new List<int>(){System.Convert.ToInt32(category)}, callback);
	}
	
	public static void reportProfile(string networkId, string reason, ReportField reportedField, List<int> categoryIds, EventDelegate callback = null)
	{
		NetworkProfileAction action = new NetworkProfileAction(ActionPriority.HIGH, REPORT_PROFILE);
		action.reportedId = networkId;
		action.reason = reason;
		action.reportCategories = categoryIds;
		action.reportedField = fieldToString(reportedField);
		Server.registerEventDelegate("report_filed", callback);
		ServerAction.processPendingActions(true);
	}

	public static void getAvatarURLs(EventDelegate callback = null)
	{
		NetworkProfileAction action = new NetworkProfileAction(ActionPriority.HIGH, GET_PROFILE_AVATARS);
		Server.registerEventDelegate("profile_avatars_data", callback);
		ServerAction.processPendingActions(true);
	}

	public static void resetProfile(List<string> fields)
	{
		NetworkProfileAction action = new NetworkProfileAction(ActionPriority.HIGH, RESET_PROFILE);
		action.fields = fields;
		ServerAction.processPendingActions(true);
	}

	private static string fieldToString(ReportField field)
	{
		switch (field)
		{
			case ReportField.NAME:
				return "name";
			case ReportField.LOCATION:
				return "location";
			case ReportField.STATUS:
				return "status";
			default:
				return "unknown";
		}
	}

	/****** End Static Methods *****/


	/// A dictionary of string properties associated with this
	public static Dictionary<string, string[]> propertiesLookup
	{
		get
		{
			if (_propertiesLookup == null)
			{
				_propertiesLookup = new Dictionary<string, string[]>();
				_propertiesLookup.Add(UPDATE_PROFILE, new string[] {NETWORK_ID, UPDATES});
				_propertiesLookup.Add(GET_PROFILE, new string[] {NETWORK_ID, FIELDS, TARGET_ZID, FB_ID});
				_propertiesLookup.Add(MULTI_GET_PROFILE, new string[] {ZIDS, FB_IDS, FIELDS});
				_propertiesLookup.Add(REPORT_PROFILE, new string[] {REPORTED_ID, REASON, CATEGORY_CODES, REPORTED_FIELD});
				_propertiesLookup.Add(GET_PROFILE_AVATARS, new string[] {});
				_propertiesLookup.Add(RESET_PROFILE, new string[] {FIELDS});
			}
			return _propertiesLookup;
		}
	}

	/// Implements IResetGame hook, clears out properties as well as clearing based on superclass's view
	new public static void resetStaticClassData()
	{
		_propertiesLookup = null;
	}

	private static Dictionary<string, string[]> _propertiesLookup = null;

	/// Appends all the specific action properties to json
	public override void appendSpecificJSON(System.Text.StringBuilder builder)
	{
		if (!propertiesLookup.ContainsKey(type))
		{
			Debug.LogError("No properties defined for action: " + type);
			return;
		}
		
		foreach (string property in propertiesLookup[type])
		{
			switch (property)
			{
				case NETWORK_ID:
					appendPropertyJSON(builder, property, networkId);
					break;
				case TARGET_ZID:
					appendPropertyJSON(builder, property, targetZid);
					break;
				case FB_ID:
					appendPropertyJSON(builder, property, fbid);
					break;
				case FB_IDS:
					appendPropertyJSON(builder, property, fbids);
					break;					
				case FIELDS:
					appendPropertyJSON(builder, property, fields);
					break;
				case ZIDS:
					appendPropertyJSON(builder, property, zids);
					break;
				case REPORTED_ID:
					appendPropertyJSON(builder, property, reportedId);
					break;
				case REASON:
					appendPropertyJSON(builder, property, reason);
					break;
				case CATEGORY_CODES:
					appendPropertyJSON(builder, property, reportCategories);
					break;
				case REPORTED_FIELD:
					appendPropertyJSON(builder, property, reportedField);
					break;
				case UPDATES:
					appendPropertyJSON(builder, property, profileUpdates);
					break;	
			default:
				Debug.LogWarning("Unknown property for action: " + type + ", " + property);
				break;
			}
		}
	}	
}
