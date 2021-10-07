using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Zynga.Core.Util;
using System;

public class ZisData : IResetGame
{
	public static string playerId = "";
	public static string zisToken = "";
	public static string zisIssuedAt = "";
	public static string zisExpiresAt = "";
	public static string zisAuthContext = "";
	public static string ssoToken = "";
	public static string ssoIssuedAt = "";
	public static string ssoAuthContext = "";
	public static string installCredentialsId = "";
	public static string installCredentialsSecret = "";
	public static JSON installCredentials = null;

	private static string appleName = "";
	private static string appleEmail = "";

	private static string facebookName = "";
	private static string facebookEmail = "";

	private static string email = "";

	public const string DEFAULT_APPLE_NAME = "Apple ID"; //Default value when apple name is null

	/*
	 *{"player_id":"78507209539",
	 *"zis_token":{"token":"CsKDZ1bt_wpDGmWF7D8YQVNNNhAIc31XJloeqeHYX5Wo0brsu0Y90faOOap2-7M8SlBRQYbvNjVNWv-OEn6GzW_Emb9lR8SYtmSLVtGoSnfCtoqgj5IGKaaACwF6Gx9MOYQ_u2BzWdYmodHZg6bbddxZsEPBdHGqmip3YjNeyj8XhDXRhajK170OeQIIukotNctTFlSq_ztpnLuWWKCF8Q==",
	 *"issued_at":"1575997394",
	 *"expires_at":"1576000994",
	 *"auth_context":["SIWA"]},
	 *"sso_token":{"token":"tBU5uMZ_Cm8gATGPCm-nTJZFhsNy4V3i4iIUQIpqi4GTZ2FuGTHjUhrvl_NPU6ujukZlXmmyMuYpWNFASZ4DOn57dhJgtAMR8A4RUxGjkx88PJs71OfCSu7Y6zQ8gzsrtM6dH6sVmd20kmwxmRTv3Zb2hu18ZSM8VS0PDTdhE68=","issued_at":"1575997394","auth_context":["SIWA"]},
	 *"install_credentials":{"id":"78511228724","secret":"265cff007629f790"}} 
	*/
	// Use this for initialization
	void Start()
	{

	}

	// Update is called once per frame
	void Update()
	{
			
	}

	//Returns the apple user name 
	public static string AppleName
	{
		get
		{
			if (appleName.IsNullOrWhiteSpace())
			{
				appleName = DEFAULT_APPLE_NAME;
			}
			if (Application.isPlaying)
			{
				return appleName;
			}
			return appleName;
		}

		set
		{
			appleName = value;
		}
	}

	//Returns the apple user name 
	public static string FacebookName
	{
		get
		{
			if (facebookName.IsNullOrWhiteSpace())
			{
				facebookName = AppleName;
			}
			if (Application.isPlaying)
			{
				return facebookName;
			}
			return facebookName;
		}

		set
		{
			facebookName = value;
		}
	}

	public static string AppleEmail
	{
		get
		{
			if (Application.isPlaying)
			{
				return appleEmail;
			}
			return appleEmail;
		}

		set
		{
			appleEmail = value;
		}
	}

	public static string FacebookEmail
	{
		get
		{
			if (Application.isPlaying)
			{
				return facebookEmail;
			}
			return facebookEmail;
		}

		set
		{
			facebookEmail = value;
		}
	}

	public static string Email
	{
		get
		{
			if (Application.isPlaying)
			{
				return email;
			}
			return email;
		}

		set
		{
			email = value;
		}
	}

	public static void loadZisData(JSON data)
	{
		if (data == null)
		{
			Debug.Log("Zis data is null");
			return;
		}

		setApplePreferences(data);
	}

	public static void setApplePreferences(JSON data)
	{
		Debug.Log("AppleLogin: In setapplepreferences");
		if (data != null)
		{
			playerId = data.getString("player_id", "");
			JSON zis = data.getJSON("zis_token");
			if (zis != null)
			{
				zisToken = zis.getString("token", "");
				zisIssuedAt = zis.getString("issued_at", "");
				zisExpiresAt = zis.getString("expires_at", "");
				Debug.Log("AppleLogin: before auth_context");
				string[] zisAuthContextArray = zis.getStringArray("auth_context");
				if (zisAuthContextArray.Length > 1)
				{
					zisAuthContext = zisAuthContextArray[0];
				}
			}
			Debug.Log("AppleLogin: before sso_token");

			Debug.Log("AppleLogin: in sso_token");
				JSON sso = data.getJSON("sso_token");
				if (sso != null)
				{
					Debug.Log("AppleLogin: sso_token");
					ssoToken = sso.getString("token", "");
					ssoIssuedAt = sso.getString("issued_at", "");
					string[] ssoAuthContextArray = sso.getStringArray("auth_context");
					if (ssoAuthContextArray.Length > 1)
					{
						ssoAuthContext = ssoAuthContextArray[0];
					}
				} 
				else 
				{
					Debug.Log("AppleLogin: sso_token is empty");
				}


			installCredentials = data.getJSON("install_credentials");
				if (installCredentials != null)
				{
					Debug.Log("AppleLogin: installcredentials");
					installCredentialsId = installCredentials.getString("id", "");
					installCredentialsSecret = installCredentials.getString("secret", "");
				}
				else
				{
					Debug.Log("AppleLogin: install_credentials is empty");	
				}

			//Setting all data to preferences 
			PreferencesBase preferences = SlotsPlayer.getPreferences();
			preferences.SetString(SocialManager.zisPlayerId, playerId);
			preferences.SetString(SocialManager.zisToken, zisToken);
			preferences.SetString(SocialManager.zisIssuedAt, zisIssuedAt);
			preferences.SetString(SocialManager.zisExpiresAt, zisExpiresAt);
			preferences.SetString(SocialManager.zisAuthContext, zisAuthContext);
			preferences.SetString(SocialManager.ssoToken, ssoToken);
			preferences.SetString(SocialManager.ssoIssuedAt, ssoIssuedAt);
			preferences.SetString(SocialManager.ssoAuthContext, ssoAuthContext);
			preferences.SetString(SocialManager.installCredentialsId, installCredentialsId);
			preferences.SetString(SocialManager.installCredentialsSecret, installCredentialsSecret);
			//delete the siwaAuthCodeIssueTime key to invalidate siwa token
            //siwa tokens are good for one time in 5 min window
            //if the user logs in the 5 min window we try to use this over zis token
            //so its best to remove the expiration time as we cant re use it anyways
			preferences.DeleteKey(SocialManager.siwaAuthCodeIssueTime);
			if (installCredentials != null)
			{
				preferences.SetString(SocialManager.installCredentials, installCredentials.ToString());
			}
			else
			{
				Debug.Log("AppleLogin: install credentials is null");
			}
			Debug.LogFormat("AppleLogin: set preference in zisdata playerid {0} zis token {1} zis expires {2}", playerId, zisToken, zisExpiresAt);
			Dictionary<string, string> extraFields = new Dictionary<string, string>();
			extraFields.Add("playerId", playerId);
			extraFields.Add("zisToken", zisToken);
			extraFields.Add("zisExpiresAt", zisExpiresAt);
			SplunkEventManager.createSplunkEvent("SIWA Connections Details", "SIWA-connections-details", extraFields);

			preferences.Save();
		} 
		else 
		{
			Debug.Log("AppleLogin: Data is null");	
		}
	}


	// Logout - deleting all the preferences when logging out
	public static void deleteApplePreferences()
	{
		Debug.Log("AppleLogin: delete apple preferences..logging out of apple");
		PreferencesBase preferences = SlotsPlayer.getPreferences();
		preferences.DeleteKey(SocialManager.zisPlayerId);
		preferences.DeleteKey(SocialManager.zisToken);
		preferences.DeleteKey(SocialManager.zisIssuedAt);
		preferences.DeleteKey(SocialManager.zisExpiresAt);
		preferences.DeleteKey(SocialManager.zisAuthContext);
		preferences.DeleteKey(SocialManager.ssoToken);
		preferences.DeleteKey(SocialManager.ssoIssuedAt);
		preferences.DeleteKey(SocialManager.ssoAuthContext);
		preferences.DeleteKey(SocialManager.installCredentialsId);
		preferences.DeleteKey(SocialManager.installCredentialsSecret);
		preferences.DeleteKey(SocialManager.installCredentials);
		preferences.DeleteKey(SocialManager.siwaAuthCodeIssueTime);
		preferences.DeleteKey(SocialManager.AppleAuthorizationCode);
		preferences.DeleteKey(SocialManager.AppleIdentityToken);
		preferences.DeleteKey(SocialManager.AppleUserIdKey);
		preferences.DeleteKey(SocialManager.fbToken);
		preferences.DeleteKey(SocialManager.fbName);
		preferences.DeleteKey(SocialManager.appleName);
		preferences.Save();
	}


	public static void checkRefreshZisToken()
	{
		int currentTime = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
		int expirationTime = Int32.Parse(zisExpiresAt) - SocialManager.ZIS_TOKEN_EXPIRATION_TIME;
		var credentials = System.Text.Encoding.UTF8.GetBytes(installCredentials.ToString());
						
		if (currentTime >= expirationTime)
		{
			RefreshZisTokenAction.RefreshZisToken(zisToken, System.Convert.ToBase64String(credentials));
		}
	}

	/// Implements IResetGame
	public static void resetStaticClassData()
	{
		playerId = null;
		zisToken = null;
		zisIssuedAt = null;
		zisExpiresAt = null;
		zisAuthContext = null;
		ssoToken = null;
		ssoIssuedAt = null;
		ssoAuthContext = null;
		installCredentialsId = null;
		installCredentialsSecret = null;
		installCredentials = null;
	}

}
