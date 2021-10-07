using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zynga.Core.Util;
using Zynga.Authentication.Facebook;
//using Zynga.SocialAuth.Facebook;

namespace Com.HitItRich.Authentication.Facebook
{
	/// <summary>
	/// Manages caching Facebook Access tokens to PlayerPrefs (indexedDB) for the DotCom Platform.
	///  On DotCom we want to cache the token locally so we aren't asking the player to login all the time. 
	/// </summary>
	public static class FBTokenCacheManager
	{
		public static void CacheToken(FacebookAccessToken token)
		{
			PreferencesBase prefs = SlotsPlayer.getPreferences();
			prefs.SetString(Prefs.FB_ACCESS_TOKEN_EXPIRATION_TIME, Common.dateTimeToUnixSecondsAsInt(token.ExpirationTime).ToString());
			prefs.SetString(Prefs.FB_ACCESS_TOKEN_STRING, token.TokenString);
			prefs.SetString(Prefs.FB_ACCESS_TOKEN_USER_ID, token.UserId);
			prefs.SetString(Prefs.FB_ACCESS_TOKEN_LAST_REFRESH, Common.dateTimeToUnixSecondsAsInt(token.LastRefresh).ToString());
			prefs.SetString(Prefs.FB_ACCESS_TOKEN_PERMISSIONS, token.Permissions.ToString());
			prefs.Save();
		}
		
		public static FacebookAccessToken GetCachedToken()
		{
			int expirationInt = int.Parse(PlayerPrefsCache.GetString(Prefs.FB_ACCESS_TOKEN_EXPIRATION_TIME, "0"));
			DateTime expirationTime = DateTimeOffset.FromUnixTimeSeconds(expirationInt).UtcDateTime;
			string token = PlayerPrefsCache.GetString(Prefs.FB_ACCESS_TOKEN_STRING, string.Empty);
			string userId = PlayerPrefsCache.GetString(Prefs.FB_ACCESS_TOKEN_USER_ID, string.Empty);

			DateTime lastRefresh = DateTime.Now;
			List<string> permissions = null;
			try
			{
				int lastRefreshInt = int.Parse(PlayerPrefsCache.GetString(Prefs.FB_ACCESS_TOKEN_LAST_REFRESH, "0"));
				lastRefresh = DateTimeOffset.FromUnixTimeSeconds(lastRefreshInt).UtcDateTime;
				permissions = PlayerPrefsCache.GetString(Prefs.FB_ACCESS_TOKEN_PERMISSIONS, "").Split(',').ToList();
			}
			catch (Exception e)
			{
				// do nothing
			}

			if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userId))
			{
				return null;
			}

			return new FacebookAccessToken(token, expirationTime, permissions, userId, lastRefresh);
		}

		public static void ClearCache()
		{
			PreferencesBase prefs = SlotsPlayer.getPreferences();
			prefs.DeleteKey(Prefs.FB_ACCESS_TOKEN_EXPIRATION_TIME);
			prefs.DeleteKey(Prefs.FB_ACCESS_TOKEN_STRING);
			prefs.DeleteKey(Prefs.FB_ACCESS_TOKEN_USER_ID);
			prefs.DeleteKey(Prefs.FB_ACCESS_TOKEN_LAST_REFRESH);
			prefs.Save();
		}
	}
}