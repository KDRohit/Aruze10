using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;

public class ClientVersionCheck : IDependencyInitializer
{
	private InitializationManager initManager = null;

	public static ClientVersionCheck Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new ClientVersionCheck();
			}
			return _instance;
		}
	}
	private static ClientVersionCheck _instance;

	#region IDependencyInitializer implementation
	public System.Type[] GetDependencies ()
	{
		return new System.Type[] {typeof(BasicInfoLoader)};
	}
	
	private bool hasRequireVersion()
	{
		if (Application.isEditor && Glb.clientVersion == "dev")
		{
			return true;	
		}
		
		if (string.IsNullOrEmpty(Glb.clientVersion))
		{
			Debug.LogWarning("Glb.clientVersion is empty.");
			return true;
		}
		
#if UNITY_WEBGL
		if (Data.canvasBasedConfig != null)
		{
			string liveVersion = Data.canvasBasedConfig.getString("LIVE_VERSION", "0");
			try
			{
				Version myVersion = new Version(Glb.clientVersion);
				Version publishedVersion = new Version(liveVersion);

				if (myVersion < publishedVersion)
				{
					// This is an old instance of the game. Possibly cached. We don't have a forced update flow
					// for webgl, so we're going to just halt loading.
					Debug.LogError("Cached client detected. Halting.");
					return false;
				}
			}
			catch (System.ArgumentException e )
			{
				Debug.LogErrorFormat("Bad client version detected: '{0}' - Either {1} or {2}. Halting.", e.Message, Glb.clientVersion, liveVersion);
				return false;
			}

		} else {
			Debug.LogError("Canvas data missing. Halting.");
			return false;

		}
		Application.ExternalEval("window.predownloaderComplete()");
		return true;
#else	
		return !Glb.isUpdateAvailable;
#endif
	}

	// Returns the integer revision portion of a version string (the number after the final period, if any)
	// For example, for "1.6.1050" will return 1050
	// Returns 0 if it can't parse
	private int getRevisionFromVersionString(string versionString)
	{
		int revision = 0;
		if (!string.IsNullOrEmpty(versionString))
		{
			int indexOfLastDot = versionString.LastIndexOf(".");
			int.TryParse(versionString.Substring(indexOfLastDot + 1), out revision);
		}
		return revision;
	}
	
	private void showUpdateDialog(Dict answerArgs = null)
	{
		// Prevent further login
		ServerAction.enableCommunication = false;
		
		if (answerArgs == null)
		{
			Loading.hide(Loading.LoadingTransactionResult.FAIL);
			Debug.LogWarning(string.Format("The current client version ('{0}') does not meet the minimum required client version ('{1}').", 
										Glb.clientVersion, 
										Glb.minClientVersion));
		}
		
		// TODO: Localize this dialog with device localization file or global data.
		GenericDialog.showDialog(
			Dict.create(
				D.TITLE, Localize.text("motd_update_body_title_mobile"),
				D.MESSAGE, Localize.text("motd_update_body_text"),
				D.REASON, "client-version-out-of-date",
				D.CALLBACK, new DialogBase.AnswerDelegate( 
					(args) => 
					{
						if (string.IsNullOrEmpty(Glb.clientAppstoreURL) == false)
						{
							Application.OpenURL(Glb.clientAppstoreURL);
							showUpdateDialog(args);
						}
						else
						{
							Debug.LogError("Could not redirect user to update page.  'Glb.updateClientURL' is empty.");
						}
					} )
			),
			SchedulerPriority.PriorityType.IMMEDIATE
		);	
	}

	public void Initialize (InitializationManager mgr)
	{
		this.initManager = mgr;
		
		if (hasRequireVersion())
		{
			this.initManager.InitializationComplete(this);
		}
		else
		{
#if !UNITY_WEBGL
			showUpdateDialog();
#endif
		}
	}
	
	// short description of this dependency for debugging purposes
	public string description()
	{
		return "ClientVersionCheck";
	}
	#endregion


}
