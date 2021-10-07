using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Zynga.Zdk;
using Zynga.Zdk.Services.Common;
using Zynga.Core.Tasks;
using System.Threading.Tasks;

/*
Identities dev panel.
*/
using Zynga.Core.Util;
using Zynga.Zdk.Services.Identity;

public class DevGUIMenuIdentities : DevGUIMenu
{
	private List<string> identities;

	public override void drawGuts()
	{
		if (identities == null)
		{
			init();
		}
		
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Scramble FB Token"))
		{
			SocialManager.Instance.Logout(false);
			//Application.Quit();
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Revoke FB Permissions"))
		{
			if (SlotsPlayer.isFacebookUser)
			{
				SocialManager.Instance.RevokePermissions();
				SocialManager.Instance.Logout(false, false, false);
			}
			SocialManager.Instance.Logout(false);
			//Application.Quit();
		}
		
		GUILayout.EndHorizontal();

		IdentityServiceBase service = PackageProvider.Instance.Identity.Service;

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Reset Anonymous ZIDS"))
		{
			if (SlotsPlayer.isFacebookUser)
			{
				//service.Prune(Packages.SocialAuthFacebook.Channel.Session);
			}
		}

		if (identities != null)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label("Anonymous zid count: " + identities.Count.ToString());
			GUILayout.EndHorizontal();
		}
		
		GUILayout.EndHorizontal();
	
		GUILayout.TextArea ("Session Token: " + ZdkManager.Instance.Zsession.Token);

		GUILayout.BeginHorizontal();
		GUILayout.Label("Access Enabled: " + SlotsPlayer.instance.allowedAccess);

		if (SlotsPlayer.isFacebookUser)
		{
			GUILayout.Label("Has Declined Permissions: " + SocialManager.Instance.hasDeclinedPerms);
		}
		GUILayout.Label("Payments Enabled: " + SlotsPlayer.instance.allowedPayments);
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label("Player Identity");
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label("SNID: " + SlotsPlayer.instance.socialMember.id);
		GUILayout.Label("ZID: " + SlotsPlayer.instance.socialMember.zId);
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label("ZdkManager Identity");
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label("SNID: " + ZdkManager.Instance.Zsession.Snid);
		GUILayout.Label("ZID: " + ZdkManager.Instance.Zsession.Zid);
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label("Anonymous ZDK Identity");
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label("SNID: " + Snid.Anonymous);
		GUILayout.Label("ZID: " + PackageProvider.Instance.ServicesCommon.Client.Session.Zid.ToString());
		GUILayout.EndHorizontal();

		// Enumerate all of our various identities:
		if (identities != null)
		{
			for (int i = 0; i < identities.Count; ++i)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(identities[i]);
				GUILayout.EndHorizontal();
			}
		}

		GUILayout.BeginHorizontal();
		GUILayout.Label("Token expiration: " + ZdkManager.Instance.Zsession.Expires.ToString(@"MM\/dd\/yyyy HH:mm"));
		GUILayout.EndHorizontal();
	}
	
	private void init()
	{
		List<string> zidList = new List<string> ();
		zidList.Add (SlotsPlayer.instance.socialMember.zId);


		getAnonIdentities();
	}

	private void getAnonIdentities()
	{
		identities = new List<string>();

		if (SlotsPlayer.isFacebookUser)
		{
			ServiceSession currentSession = PackageProvider.Instance.ServicesCommon.Client.Session;
			IdentityServiceBase service = PackageProvider.Instance.Identity.Service;

			/*Task<IdentitiesGet> getTask = service.Get();
			getTask.Callback(identitiesTask =>
			{
				if (identitiesTask.IsCompleted)
				{
					Dictionary<Zid, List<ZidInfo>> result = identitiesTask.Result.Identities;
					List<ZidInfo> zids = null;
					
					foreach (KeyValuePair<Zid, List<ZidInfo>> entry in result)
					{
						if (entry.Key.ToString() == currentSession.Zid.ToString())
						{
							zids = entry.Value;
							break;
						}
					}

					if (zids != null)
					{
						for (int i = 0; i < zids.Count; ++i)
						{
							if (zids[i].ToString() != currentSession.Zid.ToString())
							{
								identities.Add(zids[i].ToString());
							}
						}
					}
				}
			}); */
		}
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
}
