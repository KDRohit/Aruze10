using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Advertisements;

public sealed class UnityAdsManager : MonoBehaviour, IUnityAdsListener, IDependencyInitializer, System.IDisposable
{
	public enum PlacementId
	{
		BUY_PAGE,
		DAILY_BONUS,
		OOC,
		CAROUSEL,
		VIDEO
	}

	private readonly Dictionary<PlacementId, object> eventIndexDict = new Dictionary<PlacementId, object>();
	
	private static readonly Dictionary<PlacementId, string> placementStringDict = new Dictionary<PlacementId, string>()
	{
		{ PlacementId.BUY_PAGE, "rewardedvideo_BuyPage" },
		{ PlacementId.DAILY_BONUS, "rewardedvideo_dailybonus" },
		{ PlacementId.OOC, "rewardedvideo_OOC" },
		{ PlacementId.CAROUSEL, "rewardedvideo_carousel" },
		{ PlacementId.VIDEO, "rewardedVideo" }
	};

	public static string getStatName(PlacementId id)
	{
		if (placementStringDict != null && placementStringDict.TryGetValue(id, out string statName))
		{
			return statName;
		}
		return "rewardedVideo";
	}
	
	/// <summary>
	/// Event class to handle unity ad data
	/// </summary>
	public class UnityAdEventArgs : System.EventArgs
	{
		public ShowResult result { get; private set; }
		public UnityAdEventArgs(ShowResult adShowResult)
		{
			result = adShowResult;
		}
	}
	
	/// <summary>
	/// Event delegate for ad show calls
	/// </summary>
	public delegate void UnityAdEventHandler(object sender, UnityAdEventArgs e );
	
	public static UnityAdsManager instance { get; private set; }
	
	/// <summary>
	/// Event handlers for ads
	/// </summary>
	private readonly EventHandlerList adFinishedEventHandlers = new EventHandlerList();

	private readonly EventHandlerList adStartEventHandlers = new EventHandlerList();
	
	public bool isInitialized { get; private set; }

	private PlacementId lastViewedPlacementId;

	
	private void Awake()
	{
		if (instance != null && instance.gameObject != null)
		{
			Destroy(instance.gameObject);
		}

		instance = this;
		DontDestroyOnLoad(gameObject);
	}

	private string _gameId = null;
	public string gameId
	{
		get
		{
			if (_gameId == null)
			{
				_gameId = Data.liveData.getString("UNITY_ADS_GAME_ID", ""); 
			}

			return _gameId;
		}	
	}
	
	/// <summary>
	/// Perform init tasks.
	/// </summary>
	private void performInit()
	{
		//construct index objects
		constructIndexObjects();
		
		//init unity ads
		Advertisement.AddListener(this);
		if (!string.IsNullOrEmpty(gameId))
		{
			Advertisement.Initialize(gameId, false);	
		}
		
		//set init flag
		isInitialized = true;
	}

	private void constructIndexObjects()
	{
		eventIndexDict.Clear();
		foreach (PlacementId id in placementStringDict.Keys)
		{
			eventIndexDict.Add(id, new object());
		}
	}

	public static bool isAdAvailable(PlacementId id)
	{
		if (instance == null || string.IsNullOrEmpty(instance.gameId))
		{
			return false;
		}
		
		string placementId = placementStringDict[id];
		return Advertisement.IsReady(placementId);
	}

	public static void showRewardVideo(PlacementId id, System.EventHandler startCallback, UnityAdEventHandler finishCallback)
	{
		string placementId = placementStringDict[id];
		if (instance == null || string.IsNullOrEmpty(instance.gameId))
		{
			Debug.LogError("Unity ad manager not initialized yet");
			if (finishCallback != null)
			{
				finishCallback(null, new UnityAdEventArgs(ShowResult.Failed));	
			}
			return;
		}
		
		if (Advertisement.IsReady(placementId))
		{
			instance.lastViewedPlacementId = id;
			if (startCallback != null)
			{
				instance.adStartEventHandlers.AddHandler(instance.eventIndexDict[id], startCallback);
			}
			if (finishCallback != null)
			{
				instance.adFinishedEventHandlers.AddHandler(instance.eventIndexDict[id], finishCallback);	
			}

			//Add game server id for server receipt
			ShowOptions options = new ShowOptions();
			options.gamerSid = SlotsPlayer.instance.socialMember.zId;
			
			//show add
			Advertisement.Show(placementId, options);
		}
		else
		{
			if (finishCallback != null)
			{
				Debug.LogWarning("No ad available for placement id: " + placementId);
				finishCallback(instance, new UnityAdEventArgs(ShowResult.Failed));
			}
		}
	}
	
	#region IDependecyInitializer
	// This method should be implemented to return the set of class type definitions that the implementor
	// is dependent upon.
	public System.Type[] GetDependencies()
	{
		return new System.Type[] { typeof(AuthManager), typeof(ExperimentManager), typeof(GameLoader), typeof(SocialManager), typeof(ZdkManager) } ;
	}

	// This method should contain the logic required to initialize an object/system.  Once initialization is
	// complete, the implementing class should call the "mgr.InitializationComplete(this)" method to signal
	// that downstream dependencies can be initialized.
	public void Initialize(InitializationManager mgr)
	{
		performInit();
		mgr.InitializationComplete(this);
	}

	// give a short description of the dependency that we may use for debugging purposes
	public string description()
	{
		return "UnityAdsManager";
	}
	#endregion
	
	#region IUnityAdsListener
	public void OnUnityAdsReady(string placementId)
	{	
	}

	public void OnUnityAdsDidFinish(string placementId, ShowResult showResult)
	{
		//call the event handler list
		object index = eventIndexDict[lastViewedPlacementId];
		UnityAdEventHandler adEventDelegate =(UnityAdEventHandler)adFinishedEventHandlers[index];
		if (adEventDelegate != null)
		{
			adEventDelegate(this, new UnityAdEventArgs(showResult));
		}
		else
		{
			Debug.LogWarning("No handler registered for placement id: " + placementId);
		}
		
		//remove all callbacks for placement id
		adFinishedEventHandlers.RemoveHandler(index, adFinishedEventHandlers[index]);
	}

	public void OnUnityAdsDidError(string message) {
		// Log the error.
		Debug.LogError("Unity Ad error: " + message);
	}

	public void OnUnityAdsDidStart(string placementId) 
	{
		//call the event handler list
		object index = eventIndexDict[lastViewedPlacementId];
		System.EventHandler adEventDelegate =(System.EventHandler)adStartEventHandlers[index];
		if (adEventDelegate != null)
		{
			adEventDelegate(this, System.EventArgs.Empty);
		}
		
		//remove all callbacks for placement id
		adStartEventHandlers.RemoveHandler(index, adStartEventHandlers[index]);
	} 
	#endregion

	
	public void Dispose()
	{
		adStartEventHandlers?.Dispose();
		adFinishedEventHandlers?.Dispose();
	}
}
