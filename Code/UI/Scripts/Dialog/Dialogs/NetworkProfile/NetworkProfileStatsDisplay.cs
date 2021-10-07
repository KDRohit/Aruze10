using UnityEngine;
using System.Collections;
using TMPro;


public class NetworkProfileStatsDisplay : NetworkProfileTabBase
{
	public TabManager gamesTabManager;

	public GameObject hirConnected;
	public GameObject wonkaConnected;
	public GameObject wonkaUnconnected;
	public GameObject wozConnected;
	public GameObject wozUnconnected;
	public GameObject gotConnected; //Game of Thrones
	public GameObject gotUnconnected;

	public NetworkProfileStatPanel hirStats;
	public NetworkProfileStatPanel wozStats;
	public NetworkProfileStatPanel wonkaStats;
	public NetworkProfileStatPanel gotStats;

	public ImageButtonHandler wozDownloadButton;
	public TextMeshPro wozDownloadButtonLabel;
	public ImageButtonHandler wonkaDownloadButton;
	public TextMeshPro wonkaDownloadButtonLabel;
	public ImageButtonHandler gotDownloadButton;
	public TextMeshPro gotDownloadButtonLabel;

	public TextMeshPro wozUnconnectedLabel;
	public TextMeshPro wonkaUnconnectedLabel;
	public TextMeshPro gotUnconnectedLabel;

	private const string HIR = "hir";
	private const string WONKA = "wonka";
	private const string WOZ = "woz";
	private const string GOT = "got";

	private enum GamesTabTypes:int
	{
		HIR = 0,
		WOZ = 1,
		WONKA = 2,
		GOT = 3
	}

	public override IEnumerator onIntro(NetworkProfileDialog.ProfileDialogState fromState, string extraData = "")
	{
		StatsManager.Instance.LogCount("dialog", "ll_profile", "games", "view", statFamily, member.networkID.ToString());
		yield return null;
	}
	
	public void init(SocialMember member)
	{
		this.member = member;
		NetworkProfile profile = member.networkProfile;		
		gamesTabManager.init(typeof(GamesTabTypes), (int)GamesTabTypes.HIR, onGamesTabSelect);


		// NEW LOGIC
		// If the player has stats for a given game, then show the stats, otherwise show the connect_to_view message

		if (profile.gameStats != null)
		{
			if (profile.gameStats.ContainsKey(HIR))
			{
				// Populate HIR stats.
				hirStats.setLabels(profile.gameStats[HIR]);
			}
			else
			{
				// Show zeroed out stats (HIR only).
				hirStats.setLabels(null);
			}

			bool wonkaKeyIsValid = profile.gameStats.ContainsKey(WONKA) && profile.gameStats[WONKA].Count > 0;
			if (!member.isUser || wonkaKeyIsValid)
			{
				// Populate WONKA stats.
				wonkaUnconnected.SetActive(false);
				wonkaConnected.SetActive(true);
				if (wonkaKeyIsValid)
				{
					wonkaStats.setLabels(profile.gameStats[WONKA]);
				}
				else
				{
					wonkaStats.setLabels(null);
				}
			}
			else
			{
				// Show please connect message.
				string wonkaUnconnectedText = Localize.text("connect_to_view_wonka", "");
				wonkaUnconnectedLabel.text = Localize.text(wonkaUnconnectedText);
				wonkaUnconnected.SetActive(true);
				wonkaConnected.SetActive(false);
				bool isWonkaInstalled = AppsManager.isBundleIdInstalled(AppsManager.WONKA_SLOTS_ID);

				
#if UNITY_WEBGL
				// There's no WebGL-based Wonka download/play; so hide the button
				wonkaDownloadButton.gameObject.SetActive(false);
#else
				wonkaDownloadButton.gameObject.SetActive(true);
				wonkaDownloadButton.registerEventDelegate(downloadSkuCallback, Dict.create(D.GAME_KEY, WONKA, D.ACTIVE, isWonkaInstalled));
				string locKey = isWonkaInstalled ? "connect_now" : "download";
				wonkaDownloadButtonLabel.text = Localize.textUpper(locKey, "");
#endif
			}

			bool wozKeyIsValid = (profile.gameStats.ContainsKey(WOZ) && profile.gameStats[WOZ].Count > 0);
			if (!member.isUser || wozKeyIsValid)
			{
				// Populate WOZ stats.
				wozUnconnected.SetActive(false);
				wozConnected.SetActive(true);
				if (wozKeyIsValid)
				{
					wozStats.setLabels(profile.gameStats[WOZ]);
				}
				else
				{
					wozStats.setLabels(null);
				}
			}
			else
			{
				// Show please connect message.
				string wozUnconnectedText = Localize.text("connect_to_view_woz", "");
				wozUnconnectedLabel.text = Localize.text(wozUnconnectedText);
				bool isWozInstalled = AppsManager.isBundleIdInstalled(AppsManager.WOZ_SLOTS_ID);

#if UNITY_WEBGL
				// There's no WebGL-based Woz download/play; so hide the button
				wozDownloadButton.gameObject.SetActive(false);
#else
				wozDownloadButton.gameObject.SetActive(true);
				wozDownloadButton.registerEventDelegate(downloadSkuCallback, Dict.create(D.GAME_KEY, WOZ, D.ACTIVE, isWozInstalled  ));
				string locKey = isWozInstalled ? "connect_now" : "download";
				wozDownloadButtonLabel.text = Localize.textUpper(locKey, "");
#endif

				wozUnconnected.SetActive(true);
				wozConnected.SetActive(false);
			}
			
			//Game of Thrones Slots
			bool gotKeyIsValid = (profile.gameStats.ContainsKey(GOT) && profile.gameStats[GOT].Count > 0);
			if (!member.isUser || gotKeyIsValid)
			{
				// Populate WOZ stats.
				gotUnconnected.SetActive(false);
				gotConnected.SetActive(true);
				if (gotKeyIsValid)
				{
					gotStats.setLabels(profile.gameStats[GOT]);
				}
				else
				{
					gotStats.setLabels(null);
				}
			}
			else
			{
				// Show please connect message.
				string gotUnconnectedText = Localize.text("connect_to_view_got", "");
				gotUnconnectedLabel.text = Localize.text(gotUnconnectedText);
				bool isGotInstalled = AppsManager.isBundleIdInstalled(AppsManager.GOT_SLOTS_ID);

#if UNITY_WEBGL
				//TODO: Change this for Game of Thrones
				// There's no WebGL-based Woz download/play; so hide the button
				gotDownloadButton.gameObject.SetActive(false);
#else
				gotDownloadButton.gameObject.SetActive(true);
				gotDownloadButton.registerEventDelegate(downloadSkuCallback, Dict.create(D.GAME_KEY, GOT, D.ACTIVE, isGotInstalled));
				string locKey = isGotInstalled ? "connect_now" : "download";
				gotDownloadButtonLabel.text = Localize.textUpper(locKey, "");
#endif

				gotUnconnected.SetActive(true);
				gotConnected.SetActive(false);
			}
		}
		else if (member.isUser)
		{
			// If there are no game stats, show the unconnected variants.
			// Turn off all the connected-only elements and replace them with their substitutes.

			// For HIR just show zero'd out stats.
			hirStats.setLabels(null);

			// Show please connect for wonka.
			string wonkaUnconnectedText = member.isUser ? "connect_to_view_wonka" : "connect_to_view_wonka_other_player";
			wonkaUnconnectedLabel.text = Localize.text(wonkaUnconnectedText);
			wonkaUnconnected.SetActive(true);
			wonkaConnected.SetActive(false);

			// Show please connect for woz.
			string wozUnconnectedText = member.isUser ? "connect_to_view_woz" : "connect_to_view_woz_other_player";
			wozUnconnectedLabel.text = Localize.text(wozUnconnectedText);
			wozUnconnected.SetActive(true);
			wozConnected.SetActive(false);
			
			// Show please connect for GoT.
			string gotUnconnectedText = member.isUser ? "connect_to_view_got" : "connect_to_view_got_other_player";
			gotUnconnectedLabel.text = Localize.text(gotUnconnectedText);
			gotUnconnected.SetActive(true);
			gotConnected.SetActive(false);
		}
		else
		{
			hirStats.setLabels(null);			
			// If there are not stats and this is 
			// Populate WONKA stats.
			wonkaUnconnected.SetActive(false);
			wonkaConnected.SetActive(true);
			wonkaStats.setLabels(null);

			// Populate WOZ stats.
			wozUnconnected.SetActive(false);
			wozConnected.SetActive(true);
			wozStats.setLabels(null);
			
			//Populate GoT stats
			gotUnconnected.SetActive(false);
			gotConnected.SetActive(true);
			gotStats.setLabels(null);
		}
	}

	private void onGamesTabSelect(TabSelector tab)
	{
		switch(tab.index)
		{
		case (int)GamesTabTypes.HIR:
			break;
		case (int)GamesTabTypes.WONKA:
			break;
		case (int)GamesTabTypes.WOZ:
			break;
		case (int)GamesTabTypes.GOT:
			break;
		default:
			break;
		}
	}

	const string WOZ_XPROMO_URL = "HIR_PROFILE_XPROMO_URL_TO_WOZ";
	const string WONKA_XPROMO_URL = "HIR_PROFILE_XPROMO_URL_TO_WONKA";
	const string WOZ_XPROMO_URL_PLAY = "HIR_PROFILE_XPROMO_URL_TO_WOZ_PLAY";
	const string WONKA_XPROMO_URL_PLAY = "HIR_PROFILE_XPROMO_URL_TO_WONKA_PLAY";
	private const string GOT_XPROMO_URL = "HIR_PROFILE_XPROMO_URL_TO_GOT";
	private const string GOT_XPROMO_URL_PLAY = "HIR_PROFILE_XPROMO_URL_TO_GOT_PLAY";

	private void downloadSkuCallback(Dict args = null)
	{
		string url = "";
		if (args != null)
		{
			string sku = args.getWithDefault(D.GAME_KEY, "") as string;
			bool isInstalled = (bool)args.getWithDefault(D.ACTIVE, false);
			switch (sku)
			{
				case WONKA:
					if (isInstalled)
					{
						url = Data.liveData.getString(WONKA_XPROMO_URL_PLAY, "");
					}
					else
					{
						url = Data.liveData.getString(WONKA_XPROMO_URL, "");
					}
					break;
				case WOZ:
					if (isInstalled)
					{
						url = Data.liveData.getString(WOZ_XPROMO_URL_PLAY, "");
					}
					else
					{
						url = Data.liveData.getString(WOZ_XPROMO_URL, "");
					}
					break;
				case GOT:
					if (isInstalled)
					{
						url = Data.liveData.getString(GOT_XPROMO_URL_PLAY, "");
					}
					else
					{
						url = Data.liveData.getString(GOT_XPROMO_URL, "");
					}
					break;
				default:
					// Do nothing
					break;
			}

			string klassValue = (isInstalled ? "ll_connect" : "download");
			StatsManager.Instance.LogCount(counterName: "dialog",
				kingdom: "ll_profile",
				phylum: "games",
				klass: klassValue,
				family: sku,
				genus: SlotsPlayer.instance.socialMember.networkID);
		}

		if (!string.IsNullOrEmpty(url))
		{
			Application.OpenURL(url);
		}

	}
}