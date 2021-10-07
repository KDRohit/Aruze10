using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Class to hold the data for each carousel slide.
*/

public class CarouselData : IResetGame
{
	public enum Type
	{
		DEFAULT = -1,
		HEADER_LEFT = 1,
		HEADER_RIGHT = 2,
		HEADER_SUBHEAD = 3,
		HEADER_FOOTER = 4,
		NEXT_UNLOCK = 5,
		POPCORN = 6,
		ZADE = 7,
		CUSTOM = 8
		// Whenever a new Type is added here, a corresponding panel prefab needs to be
		// created in Prefabs/Lobby/Carousel Panels. Make sure you choose the new Panel Type
		// in the dropdown list in the panel's script.
		// Then, you have to increment the array and link the new prefab in the "Lobby Main Panel New" prefab:
		// (path):Prefabs/Lobby/ (prefab):Lobby Main Panel/Midground/Feature Buttons/Feature Buttons Sizer/Carousel
	}
	
	private const string CAROUSEL_DATA_URL_EVENT = "carousel_data_url";
	private const string S3_VERSION_RESPONSE_KEY = "carousel_data_version";
	private const string S3_DATA_RESPONSE_KEY = "carousel_data";
	private const string S3_DATA_CACHE_FILE = "_carousel_data";

	public Type type;
	public int sortIndex;
	public int seconds;		// The number of seconds to show this slide when in auto-advance mode.
	public string[] texts;
	public JSON[] textData = null;	// Contains info about how to display the text in the associated texts array.
	public string[] imageUrls;		// URL's for displaying images. Some slides automatically create a background image in addition to these.
	public JSON[] imageData = null;	// Contains info about how to display images using the urls in the associated imageUrls array.
	public string eosExperiment = "";
	public string[] eosVariants = null;

	public string action
	{
		get { return actionString; }   // return full string, including both sides of : to preserve existing behavior

		set 
		{
			actionString = value;
			actionName = value;

			// split it now to avoid allocing substrings every time you call isValidString,
			// which may be called every frame
			// Need to use local variables to pass in by ref and out because actionName and actionParameter are private set.
			string newActionName = value;
			string newActionParameter = "";
			DoSomething.splitActionString(ref newActionName, out newActionParameter);
			actionName = newActionName;
			if (string.IsNullOrEmpty(actionParameter))
			{
				actionParameter = newActionParameter; //Only use the : actionParameter if one wasn't provided in the json parameter variable
			}
		}
	}

	private string actionString;                         // stores full action string, including ':'
	public string actionName { get; private set; }       // 1st half of action after it is split by ':'  (read-only outside class)
	public string actionParameter { get; private set; }  // 2nd half of action after it is split by ':'  (read-only outside class)

	public bool isActive = false; // Current status.
	public bool isDefault;	// Is this the default slide to show if there are no data-driven slides?
	public bool isAdminToolActive = false;	// Applies to the overall Active checkbox in the admin tool.
	public bool isPlatformEnabled = false;	// Applies to the platform-specific checkbox.
	
	public static bool isTestViewing = false;	// Set to true from the dev panel to activate all slides regardless of validation.
	public static string versionUrl = "";	// The url used to retrieve carousel data.
	public static string versionNumber = "";	// The url used to retrieve carousel data.
	public static List<CarouselData> looped = new List<CarouselData>();		// Contains looped data for all active slides (what is actually shown).
	private static List<CarouselData> inactive = new List<CarouselData>();	// Contains all data for inactive slides.

	public bool isShowingChallengesNotepad = false;
	public bool isShowingJackpotMeter = false;
	
	private int startTimeInt = -1;
	private int endTimeInt = -1;

	private int minLevel = -1;
	private int maxLevel = -1;
	
	private System.DateTime startTimeFormatted;
	private System.DateTime endTimeFormatted;
	
	private static List<CarouselData> _active;
	public static List<CarouselData> active
	{
		get
		{
			if (_active == null)
			{
				_active = new List<CarouselData>();
			}
			return _active;
		}
		private set
		{
			_active = value;
		}
	}	// Contains all data for active slides.

	// Called once upon login of a session to initialize the carousel data.
	public static void loginInit()
	{
		prepData();

		// We ask the server for the direct URL to the carousel data that's stored on S3,
		// then we request that data file directly from the client.
		// If this fails, then the data from ZRT is used as a fallback.
		RoutineRunner.instance.StartCoroutine(getDataFromS3());
	}
			
	// Use the URL we got from the server to go get the actual data from S3.
	private static IEnumerator getDataFromS3()
	{
		if (versionUrl == "")
		{
			Debug.LogError("No versionUrl provided for carousel data.");
		}
		else
		{
			string version = "";
			
			if (!Data.liveData.getBool("USE_PARTIAL_URLS", false))
			{
				// The first URL never changes, and is to the file that contains the version number to use.
				WWW www = new WWW(versionUrl); // Caching of this file causes problems for WebGL! (we add a cache-bust elsewhere)
				yield return www;

				if (!string.IsNullOrEmpty(www.error))
				{
					Debug.LogError("Error getting carousel data version from " + versionUrl + "\n" + www.error);
				}

				version = www.text;
			}
			else if (!string.IsNullOrEmpty(versionNumber))
			{
				version = versionNumber;
			}

			if (string.IsNullOrEmpty(version))
			{
				Debug.LogError("No version number for the carousel!!!!.");
			}
			else
			{
				string url = Data.getFullUrl(versionUrl.Replace("version.txt", version.Trim() + ".txt"));

				yield return RoutineRunner.instance.StartCoroutine(Server.attemptRequest(url, null, "", S3_DATA_RESPONSE_KEY, false, S3_DATA_CACHE_FILE, false));
	
				JSON jsonData = Server.getResponseData(S3_DATA_RESPONSE_KEY);

				if (jsonData == null)
				{
					Debug.LogError("No contents in carousel data request.");
				}
				else
				{
					Debug.Log("Received CarouselData.getDataFromS3()");
					populateAll(jsonData);

					if (!Data.liveData.getBool("USE_LIVE_DATA_LOADING_SCREEN", true))
					{
						Debug.Log("LoadingScreenData: checkForUpdates() for loading scheduler");
						LoadingScreenData.checkForUpdates(jsonData);
					}
#if UNITY_EDITOR
					// When in-editor (and not profiling), save a textual copy of the JSON as a convenience to devs...
					if (!UnityEngine.Profiling.Profiler.enabled)
					{
						string jsonText = jsonData.ToString();
						System.IO.File.WriteAllText("Temp/_carousel_data(raw).txt", jsonText);

						string prettyJsonText = Zynga.Core.JsonUtil.Json.SerializeHumanReadable(jsonData.jsonDict);
						System.IO.File.WriteAllText("Temp/_carousel_data(pretty).txt", prettyJsonText);
					}
#endif
				}
			}
		}
	}
	
	public static void populateAll(JSON data)
	{
		looped.Clear();
		active.Clear();
		inactive.Clear();
		
		foreach (JSON json in data.getJsonArray("slides"))
		{
			if (json != null)
			{
				bool isLobbyV3Slide = false;
				foreach (JSON imageData in json.getJsonArray("images"))
				{
					// these panel types are specific to lobby v3
					if (!string.IsNullOrEmpty(imageData.getString("panel_type", "")))
					{
						isLobbyV3Slide = true;
					}
				}
				if (isLobbyV3Slide)
				{
					new CarouselData(json);
				}
			}
		}

		prepData();
	}
	
	private static void createDefaultSlide()
	{
		new CarouselData();
	}
	
	// Sorts and loops all the data after any changes are made to the data.
	private static void prepData()
	{
		active.Sort(sortByIndex);

		// In order to achieve seamless looping on the carousel's page scroller,
		// we need to duplicate some items at the start and end of the list.
		// Only do this if there is more than one item since there is no
		// paging if there is only one item.
		// This must be done AFTER sorting.
		looped.Clear();
		looped.AddRange(active);
		if (looped.Count > 1)
		{
			CarouselData first = looped[0];
			CarouselData last = looped[looped.Count - 1];
			looped.Insert(0, last);
			looped.Add(first);
		}
		
		// If the lobby is currently loaded whenever data is prepped,
		// then re-initialize the carousel UI.
		if (LobbyCarousel.instance != null)
		{
			LobbyCarousel.instance.init();
		}
		else if (LobbyCarouselV3.instance != null)
		{
			LobbyCarouselV3.instance.init();
		}
	}
	
	// Constructor for the default slide to show if there is no data.
	public CarouselData()
	{	
		type = Type.CUSTOM;
		action = "buycoins";
		isDefault = true;
		string image = "HIR_Logo_BG";
		
		imageData = new JSON[] {
			new JSON("{\"x\":457, \"y\":117, \"z\":0, \"width\":914, \"height\":234, \"url\":\"" + image + ".jpg\"}")
		};
		active.Add(this);
		isActive = true;
	}
	
	public CarouselData(JSON json)
	{
#if UNITY_IPHONE
		isPlatformEnabled = json.getBool("enable_ios", false);
#elif ZYNGA_KINDLE
		isPlatformEnabled = json.getBool("enable_kindle", false);
#elif UNITY_ANDROID
		isPlatformEnabled = json.getBool("enable_android", false);
#elif UNITY_WEBGL
		isPlatformEnabled = json.getBool("enable_unityweb", false);

		// Fallback to use "enable_android" until the carousel exporter is done
		// We're noisy about it so we don't forget... (delete this block once exporter is done) -KK  
		if (!json.hasKey("enable_unityweb"))
		{
			Debug.LogWarning("CarouselData - missing key 'enable_unityweb', using 'enable_android' instead");
			isPlatformEnabled = json.getBool("enable_android", false);
		}
#elif UNITY_WSA_10_0 && NETFX_CORE //SMP this may need to be WSA specific
		isPlatformEnabled = json.getBool("enable_windows", false);
#else
		isPlatformEnabled = false;
#endif
		isAdminToolActive = json.getBool("active", false);
		type = (Type)json.getInt("type", -1);
		sortIndex = json.getInt("sort_index", 0);
		seconds = json.getInt("display_seconds", 0);
		actionParameter = json.getString("actionParameter", ""); //Parameter can now be configured in admin tool instead of appending it to the action name (action:paramater)
		action = json.getString("action", "");
		eosVariants = json.getStringArray("eos_variants");
		eosExperiment = json.getString("eos_experiment", "");
		if (seconds == 0)
		{
			seconds = LobbyCarousel.DISPLAY_SECONDS_DEFAULT;
		}
		
		texts = new string[]
		{
			json.getString("text1", ""),
			json.getString("text2", "")
		};
		
		imageUrls = new string[]
		{
			json.getString("image1", ""),
			json.getString("image2", "")
		};
		
		textData = json.getJsonArray("texts");
		imageData = json.getJsonArray("images");
		startTimeInt = json.getInt("start_time", -1);
		endTimeInt = json.getInt("end_time", -1);
		minLevel = json.getInt("min_level", -1);
		maxLevel = json.getInt("max_level", -1);
		isShowingChallengesNotepad = json.getBool("enable_challenges_notepad", false);
		isShowingJackpotMeter = json.getBool("show_jackpot_amount", true); //Prior to this field exisiting, meters were always active

		if (startTimeInt >= 0)
		{
			startTimeFormatted = Common.convertFromUnixTimestampSeconds(startTimeInt);
		}
		
		if (endTimeInt >= 0)
		{
			endTimeFormatted = Common.convertFromUnixTimestampSeconds(endTimeInt);
		}

		// Some slides are only valid is certain data is available, such as targeted sales.
		// Only add them to the list if they are valid to show right now.
		if (getIsValid())
		{
			// Special case for finding out which xpromos are currently showing to the user.
		    if (action.FastStartsWith("xpromo"))
			{
				if (!MobileXpromo.liveXpromos.Contains(actionParameter))
				{
					MobileXpromo.liveXpromos.Add(actionParameter);
				}
			}			
			active.Add(this);
			isActive = true;
		}
		else
		{
			inactive.Add(this);
			isActive = false;
		}
	}

	private bool isLevelValid
	{
		get
		{
			int cachedPlayerLevel = PlayerPrefs.GetInt(Prefs.PLAYER_LEVEL, 1);
			if (cachedPlayerLevel >= minLevel)
			{
				if (maxLevel == -1)
				{
					return true;
				}
				
				if (maxLevel > -1 && cachedPlayerLevel <= maxLevel)
				{
					return true;
				}
				
				return false;
			}
			
			return false;
		}
	}
	
	// Returns whether the slide is valid to show.
	public bool getIsValid()
	{
		if (isDefault)
		{
			// The default slide is always valid.
			return true;
		}

		bool validTime = false;

		if (startTimeInt >= 0 && startTimeFormatted != System.DateTime.MinValue)
		{
			validTime = System.DateTime.UtcNow > startTimeFormatted;
		}

		//Only need to check the end time if it was set in the admin tool and we have a valid start time, or the start time wasn't set at all
		if ((validTime || startTimeInt < 0) && endTimeInt >= 0 && endTimeFormatted != System.DateTime.MinValue)
		{
			validTime = System.DateTime.UtcNow < endTimeFormatted;
		}

		bool isActive = (isAdminToolActive || validTime) && isLevelValid; //If admin tool active then ignore the start/end times
		
		if (!SlotsPlayer.isLoggedIn || !isPlatformEnabled || !isActive)
		{
			// Under normal circumstances, don't show slides that are inactive in the admin tool.
			// However, we still created the objects on the client so that they can be shown
			// when activating all panels from the dev panel for testing.
			return false;
		}
		
		bool isValid = false;
		switch (type)
		{
			case Type.NEXT_UNLOCK:
				if (SlotsPlayer.instance != null && SlotsPlayer.instance.socialMember != null)
				{
					LobbyGame nextUnlockGame = LobbyGame.getNextUnlocked(SlotsPlayer.instance.socialMember.experienceLevel);
					isValid = (nextUnlockGame != null);
				}
				break;
			case Type.ZADE:
				isValid = false; // Default this slide to off, we will turn it on if we get correct data.
				if (ExperimentWrapper.ZadeXPromo.isInExperiment)
				{
					isValid = CarouselPanelZade.isValid;
					CarouselPanelZade.getZadeAd();
				}
				break;
			default:
				// Using a generic slide type. Check for specific actions.
				isValid = DoSomething.getIsValidToSurface(this);
				break;
		}
		return isValid;
	}

	// Moves the data from the inactive list to the active list.
	public void activate(bool shouldPrep = true)
	{
		active.Add(this);
		inactive.Remove(this);
		isActive = true;

		// onActivateCarouselSlide shouldn't get called in resetting.
		if (SlotsPlayer.isLoggedIn)
		{
			DoSomething.onActivateCarouselSlide(this);
		}
		if (shouldPrep)
		{
			// If we're activating several at once, we call prepData() once after they're all activated.
			prepData();
		}
		// Special case for finding out which xpromos are currently showing to the user.
		if (actionName == "xpromo")
		{
			if (!MobileXpromo.liveXpromos.Contains(actionParameter))
			{
				MobileXpromo.liveXpromos.Add(actionParameter);
			}
		}
	}
	
	// Moves the data from the active list to the inactive list.
	public void deactivate()
	{
		if (isTestViewing)
		{
			// Don't deactivate if test viewing all slides.
			// If the caller is also logging something when doing this,
			// then you should short-circuit that logic with this same check
			// to prevent the log spamming.
			return;
		}
		
		inactive.Add(this);
		active.Remove(this);
		isActive = false;

		// onDeactivateCarouselSlide shouldn't get called in resetting.
		if (SlotsPlayer.isLoggedIn)
		{
			DoSomething.onDeactivateCarouselSlide(this);
		}

		if (active.Count == 0)
		{
			createDefaultSlide();
		}

		// Special case for finding out which xpromos are currently showing to the user.
		if (actionName == "xpromo")
		{
			if (!MobileXpromo.liveXpromos.Contains(actionParameter))
			{
				MobileXpromo.liveXpromos.Remove(actionParameter);
			}
		}

		prepData();
	}

	// Called from the dev panel to force all CUSTOM slides to be active,
	// especially the ones that aren't active due to validation.
	public static void activateAll()
	{
		if (!Data.debugMode)
		{
			// This should already be protected by only being called from the dev panel, but just making sure.
			return;
		}
		
		isTestViewing = true;
		
		List<CarouselData> inactiveCustoms = new List<CarouselData>();
		
		foreach (CarouselData data in inactive)
		{
			if (data.type == Type.CUSTOM)
			{
				inactiveCustoms.Add(data);
			}
		}
		
		foreach (CarouselData data in inactiveCustoms)
		{
			data.activate(false);
		}
		
		prepData();
	}
	
	// Find active data of the given type.
	public static CarouselData findActiveByAction(string action)
	{
		return findByAction(action, active);
	}

	// Find inactive data of the given type.
	public static CarouselData findInactiveByAction(string action)
	{
		return findByAction(action, inactive);
	}
	
	// Find data that uses the given action. There could be more than one data object
	// of the given type, but we only return the first one. For the purposes
	// of code that use this function, it would usually expect only up to one
	// object of the given type to exist.
	private static CarouselData findByAction(string action, List<CarouselData> list)
	{
		foreach (CarouselData data in list)
		{
			if (data.action == action)
			{
				return data;
			}
		}
		return null;
	}

	// Used by the Sort() method to sort the data by index.
	private static int sortByIndex(CarouselData a, CarouselData b)
	{
		return a.sortIndex.CompareTo(b.sortIndex);
	}

	// Info about this data for tracking purposes.
	public string getTrackingInfo(int page)
	{
		return string.Format("Type: {0}, Action: {1}, Page: {2}", type, action, page);
	}

	public static void resetStaticClassData()
	{
		looped.Clear();
		active.Clear();
		inactive.Clear();
		isTestViewing = false;
	}
}
