using UnityEngine;
using System.Collections;

public class StatsFacebookAuth
{
	public static void logInvalidToken()
	{
		StatsManager.Instance.LogCount
		(
			counterName: "start_session",
			kingdom: "auth",
			phylum: "facebook",
			klass: "login",
			family: "fail",
			genus: "invalid_token"
		);
	}

	public static void logInvalidPerms()
	{		
		StatsManager.Instance.LogCount
		(
			counterName: "start_session",
			kingdom: "auth",
			phylum: "facebook",
			klass: "login",
			family: "fail",
			genus: "additional_perm"
		);
	}

	public static void logConnected(string genus="current")
	{
		StatsManager.Instance.LogCount
		(
			counterName: "start_session",
			kingdom: "auth",
			phylum: "facebook",
			klass: "login",
			family: "success",
			genus: genus
		);
	}
	
	public static void logConnectClickBottomOverlay()
	{
		StatsManager.Instance.LogCount
		(
			counterName: "bottom_nav",
			kingdom: "fbAuth",
			phylum: "facebook_connect",
			klass: "",
			family: "",
			genus: "view"
		);
	}

	public static void logDisconnectClick()
	{
		StatsManager.Instance.LogCount
		(
			counterName: "top_nav",
			kingdom: "settings",
			phylum: "facebook_disconnect",
			klass: "",
			family: "",
			genus: "click"
		);
	}

	public static void logDisconnectView()
	{
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "fbAuth",
			phylum: "facebook_disconnect",
			klass: "",
			family: "",
			genus: "view"
		);
	}

	public static void logDisconnectConfirmed()
	{
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "fbAuth",
			phylum: "facebook_disconnect",
			klass: "",
			family: "yes",
			genus: "click"
		);
	}

	public static void logDisconnectCancelled()
	{
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "fbAuth",
			phylum: "facebook_disconnect",
			klass: "",
			family: "no",
			genus: "click"
		);
	}

	public static void logCarouselCardConnect()
	{
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "fbAuth",
			phylum: "carousel_card",
			klass: "",
			family: "",
			genus: "view"
		);
	}

}
