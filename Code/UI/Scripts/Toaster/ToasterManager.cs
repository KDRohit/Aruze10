using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.HitItRich.Feature.VirtualPets;
using QuestForTheChest;


public enum ToasterType
{
	// Let's keep these in alphabetical order.
	ACHIEVEMENT,
	ALL, 					// Only used for clearing all notifications.
	BUY_COIN_PROGRESSIVE, 	// Buy Page Progressive win notification
	DEFAULT_PROGRESSIVES,
	COLLECTIONS,
	GENERIC, 				// A Generic Notification.
	GIANT_PROGRESSIVE,		// Giant Progressive win notification
	JACKPOT_DAYS,			//Jackpot Days win notification
	LEVEL_UP,
	LOBBY_V3_LL,			// Loyalty Lounge sign up notification
	MAX_VOLTAGE,
	MEGA_JACKPOT,
	NETWORK_FRIENDS,		//Friends toaster
	PROGRESSIVE_NOTIF,		// Progressive win notification
	QUEST_FOR_THE_CHEST,	// Quest for the chest events
	TICKET_TUMBLER,	
	VIP_REVAMP_PROGRESSIVE,	// VIP Revamp Progressive win notification
	VIRTUAL_PETS 			//Virtual Pets notification
}

public enum ToasterTarget
{
	IN_PLACE = 0,  // Do not move the toaster when tweening.
	UR_TOP_OFF_SCREEN = 1,
	UR_TOP_ON_SCREEN = 2,
	TOP_CENTER_OFF_SCREEN = 3,
	TOP_CENTER_ON_SCREEN = 4,
}

public class ToasterManager : TICoroutineMonoBehaviour
{
	public Transform[] toasterTweenTargets; // Locations where toasters can tween to/from.
	public Transform toasterParentObject; // Gameobject under which toasters will spawn.
	public UICamera toasterCameraUI;
	public Camera toasterCamera;

	private List<ProtoToaster> queue; // The queue of ProtoToasters that have not yet spawned.
	private List<Toaster> activeToasters; // List of currently active Toasters.
	// Queue of toaster we want to show, but are waiting on member data for.
	private List<KeyValuePair<SocialMember, ProtoToaster>> memberToasterQueue;
	private GameTimer startupDelayTimer;
	private Dictionary<ToasterType, GameObject> toasterTemplates; // Map of the Toaster prefabs, keyed by ToasterType.
	
	public bool arePrefabsLoaded { get; private set; }
	private int numBundlesToLoad = 0;

	public static ToasterManager instance; // Static instance.
	public static bool isInstantMode = false; // Whether we are in instant mode or queuing mode.

	private const string TOASTER_PACKAGE_PATH = "Features/Toaster/Prefabs/New Notification Toasters";
	private const string TOASTER_COLLECTION_PATH = "Features/Collections/Prefabs/Common Prefabs/Collections Toaster";
	private const string TOASTER_TICKET_TUMBLER_PATH = "Features/Ticket Tumbler/Prefabs/Ticket Tumbler Toaster";
	private const string TOASTER_ACHIEVEMENT_PATH = "Features/Achievements Toaster/Prefabs/Achievement Toaster";
	private const string TOASTER_LOBBY_V3_LL_PATH = "Assets/Data/HIR/Bundles/Initialization/Features/Lobby V3/Lobby Prefabs/Linked VIP Lobby Toaster.prefab";
	private const string TOASTER_FRIENDS_PATH = "Features/Network Friends/Toaster Bundle/Friends Toaster";
	private const string TOASTER_LEVEL_UP_PATH = "Assets/Data/HIR/Bundles/Initialization/Features/Level Up V2/Level Up V2 Mini Panel";
	private const string TOASTER_QUEST_FOR_THE_CHEST_PATH = "Features/Quest For The Chest/Prefabs/Quest for the Chest Toaster";
	private const string TOASTER_VIRTUAL_PETS_PATH = "Features/Virtual Pets/Prefabs/Pets Toaster";

	private static List<ToasterType> packagedToasters = new List<ToasterType>()
	{
		{ ToasterType.VIP_REVAMP_PROGRESSIVE },
		{ ToasterType.PROGRESSIVE_NOTIF },
		{ ToasterType.GIANT_PROGRESSIVE },
		{ ToasterType.MAX_VOLTAGE },
		{ ToasterType.BUY_COIN_PROGRESSIVE },
		{ ToasterType.JACKPOT_DAYS },
		{ ToasterType.MEGA_JACKPOT }
	};

	private const float STARTUP_TOASTER_DELAY_TIME = 6.0f; // The number of seconds that we wait from startup/login to spawn toasters.

	void Awake()
	{
		instance = this;
		queue = new List<ProtoToaster>();
		activeToasters = new List<Toaster>();
		memberToasterQueue = new List<KeyValuePair<SocialMember, ProtoToaster>>();
		startupDelayTimer = new GameTimer(STARTUP_TOASTER_DELAY_TIME);
	}
	private void Start()
	{
		instance.toasterCamera = gameObject.GetComponentInParent<Camera>();
		instance.toasterCameraUI = gameObject.GetComponentInParent<UICamera>();
#if UNITY_WEBGL
		// Toaster camera need to be toggled on, so that they can get along with screen resize if necessary at game loading.
		// we run into a problem with dotcom that toasters are at wrong places.  dotcom screen size get changed during the game loading
		toggleCamera(true);
#else
		toggleCamera(false);
#endif
	}
	// Load the prefab templates from Resources.
	// We need to call this after login because we now rely on experiment values.
	public void loadPrefabs()
	{
		// Setting up the prefab template map.
		toasterTemplates = new Dictionary<ToasterType, GameObject>();
		//toasterTemplates.Add(ToasterType.GENERIC, SkuResources.loadFromMegaBundleOrResource("Assets/Data/HIR/Bundles/Initialization/Prefabs/Toasters/Generic Toaster"));

		++numBundlesToLoad;
		AssetBundleManager.load(this, TOASTER_PACKAGE_PATH, onPackageSuccess, onPackageFailure, blockingLoadingScreen:false);
		AssetBundleManager.downloadAndCacheBundle("toasters", keepLoaded:true, blockingLoadingScreen:false);

		if (NetworkAchievements.isEnabled)
		{
			++numBundlesToLoad;
			AssetBundleManager.load(this, TOASTER_ACHIEVEMENT_PATH, onPackageSuccess, onPackageFailure, blockingLoadingScreen:false);
		}

		if (NetworkFriends.instance.isEnabled)
		{
			++numBundlesToLoad;
			AssetBundleManager.load(this, TOASTER_FRIENDS_PATH, onPackageSuccess, onPackageFailure, blockingLoadingScreen:false);
		}

		if (Collectables.isActive())
		{
			++numBundlesToLoad;
			AssetBundleManager.load(this, TOASTER_COLLECTION_PATH, onPackageSuccess, onPackageFailure, blockingLoadingScreen:false);
		}

		if (QuestForTheChestFeature.instance.isEnabled)
		{
			++numBundlesToLoad;
			AssetBundleManager.load(this, TOASTER_QUEST_FOR_THE_CHEST_PATH, onPackageSuccess, onPackageFailure, blockingLoadingScreen:false);
		}
		
		if (VirtualPetsFeature.instance != null && VirtualPetsFeature.instance.isEnabled)
		{
			++numBundlesToLoad;
			AssetBundleManager.load(this, TOASTER_VIRTUAL_PETS_PATH, onPackageSuccess, onPackageFailure,
				blockingLoadingScreen: false);
		}

		// We currently do not have buy coin progressives in Spin it Rich.
		toasterTemplates.Add(ToasterType.LEVEL_UP, SkuResources.loadFromMegaBundleOrResource(TOASTER_LEVEL_UP_PATH) as GameObject);
		toasterTemplates.Add(ToasterType.BUY_COIN_PROGRESSIVE, SkuResources.loadFromMegaBundleOrResource("Assets/Data/HIR/Bundles/Initialization/Prefabs/Toasters/Buy Credits Progressive Toaster Small") as GameObject);
		toasterTemplates.Add(ToasterType.JACKPOT_DAYS, SkuResources.loadFromMegaBundleOrResource("Assets/Data/HIR/Bundles/Initialization/Prefabs/Toasters/Jackpot Days Toaster Small") as GameObject);

		if (TicketTumblerFeature.instance.isEnabled)
		{
			++numBundlesToLoad;
			AssetBundleManager.load(this, TOASTER_TICKET_TUMBLER_PATH, onPackageSuccess, onPackageFailure, blockingLoadingScreen:false);
		}
		// Load the lobby v3 loyalty lounge toaster.
		SkuResources.loadFromMegaBundleWithCallbacks(this, TOASTER_LOBBY_V3_LL_PATH, onPackageSuccess, onPackageFailure);
		if (numBundlesToLoad <= 0)
		{
			validateTemplates();
			arePrefabsLoaded = true;
		}
	}

	private void validateTemplates()
	{
		// Validate the templates
		foreach (KeyValuePair<ToasterType, GameObject> pair in toasterTemplates)
		{
			if (pair.Value == null)
			{
				Debug.LogErrorFormat("The prefab linked to ToasterType {0} is null", pair.Key);
			}
		}
	}

	private void onPackageSuccess(string assetPath, Object obj, Dict data = null)
	{
		switch(assetPath)
		{
			case TOASTER_PACKAGE_PATH:
				numBundlesToLoad--;
				if (!toasterTemplates.ContainsKey(ToasterType.DEFAULT_PROGRESSIVES))
 				{
 					toasterTemplates.Add(ToasterType.DEFAULT_PROGRESSIVES, obj as GameObject);
 				}
				break;

			case TOASTER_TICKET_TUMBLER_PATH:
				numBundlesToLoad--;
				if (!toasterTemplates.ContainsKey(ToasterType.TICKET_TUMBLER))
				{
					toasterTemplates.Add(ToasterType.TICKET_TUMBLER, obj as GameObject);
				}
				break;
			case TOASTER_ACHIEVEMENT_PATH:
				numBundlesToLoad--;
				if (!toasterTemplates.ContainsKey(ToasterType.ACHIEVEMENT))
				{
					toasterTemplates.Add(ToasterType.ACHIEVEMENT, obj as GameObject);
				}
				break;
			case TOASTER_LOBBY_V3_LL_PATH:
				numBundlesToLoad--;
				if (!toasterTemplates.ContainsKey(ToasterType.LOBBY_V3_LL))
				{
					toasterTemplates.Add(ToasterType.LOBBY_V3_LL, obj as GameObject);
				}
				break;
			case TOASTER_FRIENDS_PATH:
				numBundlesToLoad--;
				if(!toasterTemplates.ContainsKey(ToasterType.NETWORK_FRIENDS))
				{
					toasterTemplates.Add(ToasterType.NETWORK_FRIENDS, obj as GameObject);
				}
				break;
			case TOASTER_COLLECTION_PATH:
				numBundlesToLoad--;
				if(!toasterTemplates.ContainsKey(ToasterType.COLLECTIONS))
				{
					toasterTemplates.Add(ToasterType.COLLECTIONS, obj as GameObject);
				}
				break;
			case TOASTER_LEVEL_UP_PATH:
				numBundlesToLoad--;
				if(!toasterTemplates.ContainsKey(ToasterType.LEVEL_UP))
				{
					toasterTemplates.Add(ToasterType.LEVEL_UP, obj as GameObject);
				}
				break;
			case TOASTER_QUEST_FOR_THE_CHEST_PATH:
				numBundlesToLoad--;
				if (!toasterTemplates.ContainsKey(ToasterType.QUEST_FOR_THE_CHEST))
				{
					toasterTemplates.Add(ToasterType.QUEST_FOR_THE_CHEST, obj as GameObject);
				}
				break;
			case TOASTER_VIRTUAL_PETS_PATH:
				numBundlesToLoad--;
				if (!toasterTemplates.ContainsKey(ToasterType.VIRTUAL_PETS))
				{
					toasterTemplates.Add(ToasterType.VIRTUAL_PETS, obj as GameObject);
				}
				break;
		}

		if (numBundlesToLoad <= 0)
		{
			arePrefabsLoaded = true;
			validateTemplates();
		}
	}

	private void onPackageFailure(string assetPath, Dict data = null)
	{
		Debug.LogError("ToasterManager: couldn't load toaster package: " + assetPath);
	}

	public bool isStillPlayingToasters
	{
		get { return queue != null && queue.Count > 0; }
	}

	public bool canAddToasterPrefabs()
	{
		return arePrefabsLoaded;
	}

	public void addPrefabToTemplate(ToasterType toastType, GameObject prefab)
	{
		if (toasterTemplates != null)
		{
			toasterTemplates[toastType] = prefab;
			if (prefab == null)
			{
				Debug.LogErrorFormat("The prefab added to ToasterType {0} is null", toastType);
			}
		}
		else
		{
			Debug.LogError("cannot add toaster, toasterTemplates is null ");
		}
	}

	// Property for handling pausing/resuming the ToasterManager
	public static bool isPaused
	{
		get
		{
			return instance._isPaused;
		}
		set
		{
			if (value)
			{
				// If we are resuming functionality, then we want to start popping toasters if there are any in the queue.
				if (instance.queue.Count != 0)
				{
					instance.StartCoroutine(instance.popToasters());
				}
			}
			instance._isPaused = value;
		}
	}
	private bool _isPaused = false; // Whether the manager is paused.

	// Property for handling toaster supression
	public static bool isPlayerAlertsOn
	{
		get
		{
			return PlayerPrefsCache.GetInt(Prefs.PLAYER_ALERTS, 1) != 0;
		}
		set
		{
			PlayerPrefsCache.SetInt(Prefs.PLAYER_ALERTS, value ? 1 : 0);
			PlayerPrefsCache.Save();
			if (!value)
			{
				// Remove all the jackpot notifications if we are turning them off.
				instance.queue.RemoveAll(delegate(ProtoToaster p)
					{
						return p.isProgressiveNotification;
					}
				);
			}

		}
	}

	private static void updateMemberAndAddToaster(JSON data)
	{
		if (data == null)
		{
			Debug.LogErrorFormat("ToasterManager.cs -- updateMemberAndAddToaster -- data is null, aborting.");
			return;
		}
		// Update this socialMember
		NetworkProfileFeature.instance.parsePlayerProfile(data);
		SocialMember member = NetworkProfileFeature.instance.getSocialMemberFromData(data);
		if (instance.memberToasterQueue != null)
		{
			for (int i = instance.memberToasterQueue.Count - 1; i >= 0; i--)
			{
				//Check if the zid or the fbid (stored in the id variable) match	
				if (instance.memberToasterQueue[i].Key.zId == member.zId || instance.memberToasterQueue[i].Key.id == member.id)
				{
					addToasterToQueue(instance.memberToasterQueue[i].Value);
				}
				instance.memberToasterQueue.RemoveAt(i);
			}
		}
		else
		{
			Debug.LogErrorFormat("ToasterManager.cs -- updateMeberAndAddToaster -- failed to find any ProtoToasters associated with this member.");
		}
	}

	// Static call to add a toaster to the queue that has player data that needs to get displayed. 
	// This will get achievement data which is needed for displaying the achievement rank.
	public static void getPlayerDataAndAddToaster(SocialMember member, ToasterType type, Dict args, GameObject spawner = null, float delayTime = 0.0f)
	{
		ProtoToaster newToaster = new ProtoToaster(type, args, spawner, delayTime, member);
		if (member != null && member.isValid &&
			(member.networkProfile == null || !member.networkProfile.isComplete))
		{
			instance.memberToasterQueue.Add(new KeyValuePair<SocialMember, ProtoToaster>(member, newToaster));
			NetworkProfileAction.getProfile(member, updateMemberAndAddToaster);
		}
		else
		{
			// If the member was up to date, or there was no member to associate with this,
			// then just add the toaster to the queue.
			addToasterToQueue(newToaster);
		}
	}

	private static void addToasterToQueue(ProtoToaster toaster)
	{
#if !ZYNGA_PRODUCTION
		if (DevGUIMenuTools.disableFeatures)
		{
			return;
		}
#endif
		if (!instance.arePrefabsLoaded)
		{
			instance.loadPrefabs();
		}

		instance.toggleCamera(true);
		instance.queue.Add(toaster);

		Debug.Log("added the toaster to the queue: " + instance.queue[0].type.ToString());
		instance.queue.Sort(instance.ProtoToasterSort); // Sort the proto toasters by delay time.
		if ((isInstantMode || instance.queue.Count == 1) && !isPaused)
		{
			// If this is the only item in the queue and we are not paused, start poppin' toasters.
			instance.StartCoroutine(instance.popToasters());
		}
	}

	// Static call to add a toaster to the queue, and start the popping process if it is not already running.
	// We sort the toaster queue when we add a new one to make sure that it spawns closest to when it the caller
	// desired it to, without holding up other toasters entirely.
	public static void addToaster(ToasterType type, Dict args, GameObject spawner = null, float delayTime = 0.0f, GenericDelegate onComplete = null)
	{
#if !ZYNGA_PRODUCTION
		if (DevGUIMenuTools.disableFeatures)
		{
			return;
		}
#endif
		instance.toggleCamera(true);
		ProtoToaster newToaster = new ProtoToaster(type, args, spawner, delayTime);

		if (onComplete != null)
		{
			newToaster.onCompleteCallback = onComplete;
		}

		addToasterToQueue(newToaster);
	}

	private Toaster createToaster(ProtoToaster proto, Transform toasterParent)
	{
		toggleCamera(true);
		bool isPackagedToaster = packagedToasters.Contains(proto.type);
		ToasterType type = isPackagedToaster ? ToasterType.DEFAULT_PROGRESSIVES : proto.type;
		bool isInToasterTemplates = toasterTemplates.ContainsKey(type);
		
		// Despite having the key in packagedToasters, if it still doesn't exist for whatever reason in toasterTemplates, get outta here.
		if (!isInToasterTemplates)
		{
			Bugsnag.LeaveBreadcrumb("ToasterManager::createToaster - Could not find toaster with type " + proto.type.ToString() + " in toasterTemplates");
			return null;   
		}

		GameObject go = (GameObject)CommonGameObject.instantiate(toasterTemplates[type], Vector3.zero, Quaternion.identity);
		if (go == null)
		{
			Debug.LogError("Failed ot spawn the toaster correctly from the prefab. Failing");
			return null;
		}

		go.transform.parent = toasterParent;
		go.transform.localScale = Vector3.one;
		go.transform.localPosition = toasterTemplates[type].transform.localPosition;
		// Set the z position to 0 so it's not layered weirdly compared to the top part of the overlay.
		// This is necessary because it's often necessary to work with the prefab at > 0 in the editor
		// in order to see everything since it's not a child of the Overlay object at edit time.
		CommonTransform.setZ(go.transform, 0.0f);
		Toaster toaster = go.GetComponent<Toaster>();
		if (toaster == null)
		{
			Debug.LogError("GameObject does not have an instance of Toaster on it. Failing");
			Destroy(go);
			return null;
		}
		toaster.init(proto);
		activeToasters.Add(toaster);
		return toaster;
	}

	// Remove all the Toasters of the input type.
	public static void removeOfType(ToasterType type)
	{
		for (int i = instance.queue.Count -1; i >= 0; i--)
		{
			if (instance.queue[i].type == type || type == ToasterType.ALL)
			{
				instance.queue.RemoveAt(i);
			}
		}
	}

	// Callback for when a toaster is closed.
	public static void toasterClosed(Toaster toaster)
	{
		instance.activeToasters.Remove(toaster);

		if (isInstantMode && instance.queue.Count != 0)
		{
			instance.StartCoroutine(instance.popToasters());
		}

		if (!instance.isStillPlayingToasters)
		{
			instance.toggleCamera(false);
		}
	}

	// Can we have some custom conditions to show the toasters? Perhaps 
	// the loaded toaster class can have like "ready to show" bool 
	// that when true allows this pop toasters to keep going? Low invasive if possible.
	
	// Coroutine to pop the toasters off the queue.
	// Once started, runs until the queue is empty, or toasters are paused.
	private IEnumerator popToasters()
	{
		// This is the first in the instance.queue, so we only want to wait if there is a specified delay.
		bool first = activeToasters.Count == 0;
		while (queue.Count != 0 && !isPaused)
		{
			ProtoToaster proto = queue[0];

			// This is the first valid check, before wait delays.
			// Please keep both proto.isValid checks.
			if (!proto.isValid)
			{
				// Remove the ProtoToaster as it is no longer valid.
				queue.Remove(proto);
				Debug.Log("Toaster had become invalid, removing");
				continue;
			}

			float waitTime = proto.delayTime;
			if (first && !isInstantMode)
			{
				// If this is the first toaster in the queue since we started popping, then only wait
				// if the toaster itself has a wait time.
				first = false;
				waitTime = 0;
			}

			if (waitTime > 0)
			{
				yield return new WaitForSeconds(waitTime);
			}

			// Do not spawn the toaster if we are showing the loading screen or there is a dialog showing
			// or if we have just started the game.
			if (Loading.isLoading) { continue; }
			else if (Dialog.instance.isShowing)
			{
				if("buy_credits" != Dialog.instance.currentDialog.type.keyName)
				{
					continue;
				}
			}
			else if (!startupDelayTimer.isExpired) { continue; }
			
			/* HIR-91922: Removing toaster notifications
			else if (WeeklyRaceAlertDirector.isShowingAlerts) { continue; } 
			*/
			
			// This is the second valid check, after wait delays.
			// Please keep both proto.isValid checks.
			if (!proto.isValid)
			{
				// Remove the ProtoToaster as it is no longer valid.
				queue.Remove(proto);
				continue;
			}

			if (activeToasters.Count == 0)
			{
				// If there isn't an active toaster.
				queue.RemoveAt(0); // Remove the toaster from the queue.
				Toaster toaster = createToaster(proto, toasterParentObject);
				if (toaster == null)
				{
					Debug.LogError("Failed to spawn toaster ");
				}
			}

			if (isInstantMode)
			{
				// If we are in instant mode then we shouldn't be popping from the queue after x seconds,
				// as we will instantly pop the next one when one is closed.
				yield return null;
			}

		}
		yield return null;
	}

	public void toggleCamera(bool isEnabled)
	{
		if (toasterCameraUI != null)
		{
			toasterCameraUI.enabled = isEnabled;
		}

		if (toasterCamera != null)
		{
			toasterCamera.enabled = isEnabled;
		}

		// UIAnchors need to toggled with camera as well, so that they can be repositioned if necessary.
		// we run into a problem with dotcom that toasters are at wrong places.  the root cause
		// is that UIAnchors not enabled probably together with Camera when the screen resizes at very beginning of
		// game loading.
		foreach (UIAnchor script in toasterCamera.gameObject.GetComponentsInChildren<UIAnchor>())
		{
			script.enabled = isEnabled;
		}
	}

	// Returns the Transform associated with a ToasterTarget
	public static Transform getTweenTarget(ToasterTarget target)
	{
		return instance.toasterTweenTargets[(int)target];
	}

	// Sorting function for the ProtoToaster Queue.
	// It sorts first by the remaining delayTime, and then by their spawnTime.
	private int ProtoToasterSort(ProtoToaster a, ProtoToaster b)
	{
		int result = a.delayTime.CompareTo(b.delayTime);
		if (result == 0)
		{
			// If they have the same delay time, sort by whichever was added to the queue first.
			result = a.spawnTime.CompareTo(b.spawnTime);
		}
		return result;
	}
}

// This is a class to hold the information about a toaster before we actually spawn the gameObejct.
// This also handles the logic behind when a toaster should be spawned (timing, validity, etc).
public class ProtoToaster
{
	public delegate bool ValidationDelegate(Dict answerArgs);

	public ToasterType type; // The type of Toaster we are spawning.
	public Dict args; // The Dict passed in to setup the Toaster.
	public GameObject spawningObject; // The GameObject that called this Toaster
	public float spawnTime; // The time that this ProtoToaster was created.
	public SocialMember member; // Optional SocialMember to use if there is player data on the toaster.

	public GenericDelegate onCompleteCallback;

	private float waitTime; // The amount of time to delay the spawning of this toaster.
	private bool shouldCheckSpawnObject = true; // Whether a spawning object was passed in.

	public const float TOASTER_POP_DELAY = 3.0f; // The minimum delay between popping toasters.

	// Boolean Property that determines whether this toaster is still valid
	// If a spawning object was passed in on init, then this checks if that is now null
	// Otherwise this toaster is considered always valid.
	// Added -- we can now have a validation delegate in the arguments that will also get run as a
	// secondary validity check.
	public bool isValid
	{
		get
		{
			if ((!ToasterManager.isPlayerAlertsOn && isProgressiveNotification) ||
				// No Progressive Toaster is valid if alerts are off.
				(shouldCheckSpawnObject && spawningObject == null))
			{
				return false;
			}

			ValidationDelegate validationDelegate = null;

			if (args != null && args.ContainsKey(D.CALLBACK))
			{
				validationDelegate = args[D.CALLBACK] as ValidationDelegate;
				return (validationDelegate(args));
			}

			// If there is no validation delegate provided, then the toaster is always valid.
			return true;
		}
	}

	// Constructor for the ProtoToaster class.
	public ProtoToaster(ToasterType type, Dict args, GameObject spawner, float delayTime, SocialMember member = null)
	{
		this.type = type;
		this.args = args;
		this.member = member;
		spawningObject = spawner;

		if (spawningObject == null)
		{
			shouldCheckSpawnObject = false;
		}

		waitTime = delayTime;
		spawnTime = Time.realtimeSinceStartup;
	}

	// Returns the amount of time this remains in the front of the queue.
	public float delayTime
	{
		get
		{
			float elapsedTime = Time.realtimeSinceStartup - spawnTime;
			float remainingTime = Mathf.Max(waitTime - elapsedTime, 0.0f);
			if (ToasterManager.isInstantMode)
			{
				return remainingTime;
			}
			else
			{
				return Mathf.Max(TOASTER_POP_DELAY, remainingTime);
			}
		}
	}

	// Returns true if this prototoaster is a progressive jackpot notification.
	public  bool isProgressiveNotification
	{
		get
		{
			return type == ToasterType.GIANT_PROGRESSIVE ||
				type == ToasterType.PROGRESSIVE_NOTIF;
		}

	}
}
