using System.Collections.Generic;
using UnityEngine;
using Zynga.Zdk;
using Zynga.Zdk.Services.Common;
using Zynga.Zdk.Services.Track;

public class ZdkManager : IDependencyInitializer {
	public static string DAPI_URL = ZyngaConstants.DapiUrl;

	static public bool __IsInitialized = false;
	static public bool IsInitialized { get { return __IsInitialized;} set { __IsInitialized = value;}}

	//setting for game sessions
	private string _zid = "";
	
	/// <summary>
	/// The _dapi token which will be set when auth is correctly done
	/// </summary>
	private string _dapiToken = "";

	/// <summary>
	/// The zynga session
	/// </summary>
	private ServiceSession _zSession;
	
	/// <summary>
	/// The _player identifier.
	/// </summary>
	private string _playerId;

	/// <summary>
	/// The Track service.
	/// </summary>
	private TrackServiceBase _trackService;

	/// <summary>
	/// The main zdk component
	/// </summary>
	private Packages _zdk;

	private InitializationManager initMgr;

	/// <summary>
	/// Gets the instance of GameSession
	/// </summary>
	/// <value>
	/// The instance.
	/// </value>
	static public ZdkManager Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new ZdkManager();
			}
			return _instance;
		}
	}

	private static ZdkManager _instance;
	/// <summary>
	/// It tells whether zdk is initilized and session exists
	/// </summary>
	/// <value>
	/// <c>true</c> if this instance is ready; otherwise, <c>false</c>.
	/// </value>
	public bool IsReady {
		get {
			bool result = false;
			if (_zSession != null) {
				result = true;
			}
			return result;
		}
	}
	
	/// <summary>
	/// Handles that Session is not ready.
	/// </summary>
	/// <param name='msg'>
	/// Message.
	/// </param>
	public void HandleNotReady(string msg){
		//TODO
		Debug.Log("HandleNotReady: " + msg);
	}

	public string PlayerId {
		get { return _playerId; } 
		set{ _playerId = value; }
	}
	
	/// <summary>
	/// Gets or sets the zsession. This will be set when auth is passed through AuthManager
	/// session contains dapi token and other information to use ZDK component. If this is not
	/// set most of ZDK components will not work.
	/// </summary>
	/// <value>
	/// The zsession.
	/// </value>
	public ServiceSession Zsession { 
		get{ return _zSession; } 
		set{ 
			_zSession = value; 
			_zid = _zSession.Zid.ToString();
			_dapiToken = _zSession.Token;
			//ZyngaUsersessionManager.Singleton.AddSession(value);
		}
	}

	/// <summary>
	/// Gets the app identifier.
	/// </summary>
	/// <value>
	/// The app identifier.
	/// </value>
	public string AppId {
		get{ return ZyngaConstants.AppId; } 
	}
	

	/// <summary>
	/// Gets the fb app identifier.
	/// </summary>
	/// <value>
	/// The fb app identifier.
	/// </value>
	public string FbAppId {
		get{ return ZyngaConstants.FacebookAppId; } 
	}
	
	/// <summary>
	/// Gets the zid.
	/// </summary>
	/// <value>
	/// The zid.
	/// </value>
	public string Zid {
		get{ return _zid; } 
	}

	/// <summary>
	/// Gets or sets the zdk.
	/// </summary>
	/// <value>
	/// The zdk.
	/// </value>
	public Packages Zdk {
		get { return _zdk; }
		set{ _zdk = value; }
	}
	
	/// <summary>
	/// Gets the dapi token.
	/// </summary>
	/// <value>
	/// The dapi token.
	/// </value>
	public string DapiToken {
		get{ return _dapiToken; } 
	}

	/// <summary>
	/// Gets the Track service.
	/// </summary>
	/// <value>
	/// The track service.
	/// </value>
	public TrackServiceBase ZTrack
	{
		get {
			if (_trackService == null)
			{
				_trackService = PackageProvider.Instance.Track.Service;
			}
			return _trackService;
		}
		set { _trackService = value; }
	}
		
	/// <summary>
	/// Gets the player id.
	/// </summary>
	/// <returns>
	/// The player.
	/// </returns>
	public string GetPlayerId() {
		return _zSession.PlayerId.ToString();
	}
	
	/// <summary>
	/// Initialize this instance.
	/// </summary>
	public void Initialize() {
		if (Data.zAppId == "none" || Data.fbAppId == "none")
		{
			Data.loadConfig();
		}
		if (Data.zAppId != "none" && Data.fbAppId != "none")
		{
			Initialize(Data.zAppId, Data.fbAppId);
		}
		else 
		{
			Debug.LogError("Config not loaded properly, using default URLs/AppIDs");
			Initialize("5002525", "204013263105330");
		}
	}

	/// <summary>
	/// Initialize the specified appId and fbAppId.
	/// </summary>
	/// <param name='appId'>
	/// App identifier.
	/// </param>
	/// <param name='fbAppId'>
	/// Fb app identifier.
	/// </param>
	public void Initialize(string appId, string fbAppId){
		Initialize(appId, fbAppId, "iphone");
	}
	
	/// <summary>
	/// Initialize the specified appId, fbAppId and deviceName.
	/// </summary>
	/// <param name='appId'>
	/// App identifier.
	/// </param>
	/// <param name='fbAppId'>
	/// Fb app identifier.
	/// </param>
	/// <param name='deviceName'>
	/// Device name.
	/// </param>
	public void Initialize(string appId, string fbAppId, string deviceName) {
		Initialize(appId, fbAppId, deviceName, "en_US");
	}
	
	/// <summary>
	/// Initialize the specified appId, fbAppId, deviceName and localeCode.
	/// </summary>
	/// <param name='appId'>
	/// App identifier.
	/// </param>
	/// <param name='fbAppId'>
	/// Fb app identifier.
	/// </param>
	/// <param name='deviceName'>
	/// Device name.
	/// </param>
	/// <param name='localeCode'>
	/// Locale code.
	/// </param>
	public void Initialize(string appId, string fbAppId, string deviceName, string localeCode){


		ZyngaConstants.AppId = appId;
		ZyngaConstants.GameSkuId = appId;

		ZyngaConstants.FacebookAppId = fbAppId;
		ZyngaConstants.DeviceName = deviceName;
		ZyngaConstants.LocaleCode = localeCode;
		
		ZyngaConstants.DapiUrl = DAPI_URL;
		ZyngaConstants.ClientId = StatsManager.ClientID;

		/*Dictionary<string,object> testConfig = new Dictionary<string, object>{
			{"appId",        "5000663"               },
			{"clientId",     (int)ZDKClientID.iPadHD },
			{"dapiUrl",      "https://api.zynga.com" },
			{"deviceName",   "iphone"                },
			{"showDebug",    true                    },
			{"userkeyLabel", "com.zynga.zdkunity"    },
			{"locale",       "en_US"                 }
		};
		this.zdk = new ZDK( CoroutineRunner, testConfig );
		*/
	
		
		IsInitialized = true;
		initMgr.InitializationComplete(this);
	}
	
	// Unity calls Update() every frame
	void Update ()
	{
	}
	
#region ISVDependencyInitializer implementation
	/// <summary>
	/// The AuthManager is dependent on GameSession
	/// </summary>
	/// <returns>
	/// Type[] array
	/// </returns>
	public System.Type[] GetDependencies() {
		return new System.Type[] {typeof(ClientVersionCheck)} ;	
	}

	/// <summary>
	/// Initializes the AuthManager
	/// </summary>
	/// <param name='mgr'>
	/// Manager instance to call once intialization is complete.
	/// </param>
	public void Initialize(InitializationManager mgr) {
		initMgr = mgr;
		Initialize();
	}

	// short description of this dependency for debugging purposes
	public string description()
	{
		return "ZdkManager";
	}
#endregion
}
