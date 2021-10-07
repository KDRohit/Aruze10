using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;

public class LinkedVipProgram : FeatureBase
{
	public static LinkedVipProgram instance
	{
		get
		{
			return FeatureDirector.createOrGetFeature<LinkedVipProgram>("linked_vip_program");
		}
	}

	public delegate void LinkedVipStateChangeDelegate(NetworkState newState);
	public event LinkedVipStateChangeDelegate onNetworkStateChanged;
	
	public enum NetworkState
	{
		CONNECTED, // When we are conencted to a loyalty lounge account.
		PENDING, // When we are in the pending state where email confirmation is needed
		NONE // When we have no loyalty lounge state.
	}	

	// Network level storage data we get from the server
	public long networkPoints = 0;
	public int previousNetworkLevel = 0;	// Let's us compare to networkLevel after an update, to see if the level changed.
	public int networkLevel = 0;
	public int skuPoints = 0;
	public int skuLevel = 0;
	public long accountPoints = 0;
	public int accountLevel = 0;
	public int accountGameLevel = 0;
	public string errorCode = null;
	public long incentiveCredits = 0;
	
	private string helpShiftURL = "";
	private NetworkState _currentState;

	private const string NETWORK_GAMES_IMAGE_PATH = "misc_dialogs/Linked_VIP_Network_Games_IconsOnly.png";	
	private const string OPT_IN_GENERIC_HEADER = "opt_in_generic_header";
	private const string OPT_IN_GENERIC_MESSAGE = "opt_in_generic_message";


	//Connected or not
	public bool isConnected
	{
		get
		{
			if (ExperimentWrapper.ZisPhase2.isInExperiment)
			{
				return SlotsPlayer.IsEmailLoggedIn;
			}
			else
			{
				return currentState == NetworkState.CONNECTED;
			}
		}
	}

	//Pending or not
	public bool isPending
	{
		get
		{
			return currentState == NetworkState.PENDING;
		}
	}

	// Check if you are eligible for the linked vip program
	public bool isEligible
	{
		get
		{
			return ExperimentWrapper.LinkedVipNetwork.isInExperiment &&
				(SlotsPlayer.instance.socialMember.experienceLevel >= Glb.LINKED_VIP_MIN_LEVEL || isConnected);
		}
	}

	public bool shouldSurfaceBranding
	{
		get
		{
			return ExperimentWrapper.LinkedVipNetwork.isInExperiment;
		}
	}

	public bool shouldPromptForConnect
	{
		get
		{
			return isEligible && !isConnected && !isPending;
		}
	}

	public bool isHelpShiftActive
	{
	    get
		{
			return !string.IsNullOrEmpty(helpShiftURL);
		}
	}
	
	public NetworkState currentState
	{
		get
		{
			return _currentState;
		}
		private set
		{
			if (_currentState != value)
			{
				_currentState = value;
				if (onNetworkStateChanged != null)
				{
					onNetworkStateChanged(_currentState);
				}
			}
		}
	}

	protected override void registerEventDelegates()
	{
		Server.registerEventDelegate("vip_status", setLinkedProgramData, true);
		Server.registerEventDelegate("network_connect_incentive", linkedVipIncentiveEvent, true);
		Server.registerEventDelegate("check_opt_out", onCheckOptOut);
		Server.registerEventDelegate("opt_out_reward", optOutReward);
		onNetworkStateChanged += updateNetworkInfoOnStateChange;
	}

	protected override void clearEventDelegates()
	{
		onNetworkStateChanged -= updateNetworkInfoOnStateChange;
	}

	// Opens the help URL.
	public void openHelpUrl()
	{
		Common.openSupportUrl(helpShiftURL);
	}

	public void connectWithEmail(string email)
	{
		// Register for the event callback, then send the action to the server to connect the email.
		Server.registerEventDelegate ("network_connect", onConnectCallback, true);
		NetworkAction.connectNetwork(email);
	}

	public void updateNetworkStatus(string status)
	{
		switch (status)
		{
			case "connected":
				currentState = NetworkState.CONNECTED;
				break;
			case "pending":
				currentState = NetworkState.PENDING;
				break;
			default:
				currentState = NetworkState.NONE;
				break;
		}
	}

	private void updateNetworkInfoOnStateChange(NetworkState state)
	{
		bool isDisconnected = state == NetworkState.NONE;
		updatePlayerVipStatus(isDisconnected);
	}

	private void updatePlayerVipStatus(bool isDisconnected)
	{
		if (SlotsPlayer.instance != null)
		{
			SlotsPlayer.instance.setNetworkVipPoints(
				updatedPointsAmount: isConnected ? networkPoints : accountPoints,
				updatedVipLevel: (isConnected ? networkLevel : accountLevel),
				allowDecrease: isDisconnected
			);
		}
	}

	private void linkedVipIncentiveEvent(JSON data)
	{
		incentiveCredits = data.getLong("incentive.credits", 0);
		string eventId = data.getString("event", "");
		updateStatusAndOpenDialog("congrats_dialog", eventId);
		NetworkAction.getNetworkState();
		// Show the collect dialog.
	}
	
	// Callback for vip status
	private void setLinkedProgramData(JSON data) 
	{
		previousNetworkLevel = networkLevel;
		// We don't neccessarily have the network state when this comes in, so parse ALL of the data now.
		// We have an event that will update the player when the status changes to handle that.
		networkPoints = data.getLong("network_points", 0);
		networkLevel = data.getInt("network_level", 0);
		skuPoints = data.getInt("sku_points", 0);
		skuLevel = data.getInt("sku_level", 0);
		accountPoints = data.getLong("account_points", 0);
		accountLevel = data.getInt("account_level", 0);
		accountGameLevel = data.getInt("account_game_level", 0);
		errorCode = data.getString ("error_code", "");

		// The vip points can change without your current player status changing,
		// so we need to update the points/level here even if we are going to change it again
		// shortly when the state change comes down.
		updatePlayerVipStatus(false);
	}

	public void updateStatusAndOpenDialog(string dialog, string eventId = "", string motdKey = "")
	{
		Dict args = Dict.create(D.DIALOG_TYPE, dialog, D.EVENT_ID, eventId, D.MOTD_KEY, motdKey);
		Server.registerEventDelegate("network_state", handleStatusEvent, args);
		NetworkAction.getNetworkState();
	}

	private void handleStatusEvent(JSON data, System.Object param)
	{
		
		string status = data.getString("network_state.status", "notyet");
		string email = data.getString("network_state.email", "No email");
		if (ExperimentWrapper.ZisPhase2.isInExperiment)
		{
			if (SlotsPlayer.IsEmailLoggedIn)
			{
				status = "connected";
				email = PackageProvider.Instance.Authentication.Flow.Account.UserAccount.Email.Id;
			}
			else
			{
				status = "disconnected";
			}
		}
		Dict args = param == null ? null : (Dict)param;
		if (!string.IsNullOrEmpty(status) && status != "notyet")
		{
			updateNetworkStatus(status);
		}
		string dialogKey = (string)args.getWithDefault(D.DIALOG_TYPE, "notyet");

		switch (dialogKey)
		{
			case "status_dialog":
				LinkedVipStatusDialog.showDialog(email);
				break;
			case "program_dialog":
				string motdKey = (string)args.getWithDefault(D.MOTD_KEY, "");
				switch (currentState)
				{
					case NetworkState.CONNECTED:
						// Pass in the motdKey in this case because we want to make sure that this MOTD
						// gets marked as seen so it doesnt pop up every time once the user is connected.
						if (incentiveCredits > 0)
						{
							// If there is an incentive, then the incentive event will pop the congrats dialog, so we should
							// mark the MOTD as seen as this event is persistent until it is accepted, so the user must see it.
							MOTDFramework.markMotdSeen(Dict.create(D.MOTD_KEY, motdKey));
						}
						else
						{
							// If there is no incentive, then we should show the congrats dialog that just has ACCEPT in it, and thus
							// shoould go through the normal MOTD flow so we make sure they see it.

							LinkedVIPCongrats.showDialog(email, motdKey);
						}				
						break;
					case NetworkState.PENDING:
						Scheduler.addDialog("linked_vip_program_pending", Dict.create(D.DATA,data), SchedulerPriority.PriorityType.IMMEDIATE);
						break;
					default:
						var backgrounds = new List<string>(){
							NETWORK_GAMES_IMAGE_PATH,
							LobbyCurtainsTransition.CURTAIN_IMAGE_PATH
						};	// All sku's should use these images.
					
						backgrounds.Add(LinkedVipProgramDialogHIR.LOGO_PATH);
						// want dialog to be first in Queue, but dont want it to showimmediately and override loading screen
						Dict programDialogArgs = Dict.create(D.MOTD_KEY, motdKey, D.IS_TOP_OF_LIST, true);
						Dialog.instance.showDialogAfterDownloadingTextures("linked_vip_program", backgrounds.ToArray(), programDialogArgs, shouldAbortOnFail:false);
						break;
				}				
				break;
			case "congrats_dialog":
				string eventId = (string)args.getWithDefault(D.EVENT_ID, "");
				LinkedVIPCongrats.showDialog(email, "", eventId);
				break;
			case "notyet":
				// Do nothing, this is invalid.
				break;				
			default:
				break;
		}
	}

	private void onCheckOptOut(JSON data)
	{
		Debug.LogFormat("Check opt out {0}", data.ToString());
		Server.unregisterEventDelegate("check_opt_out");
		ZisEmailOptInDialog.showDialog(Dict.create(
			D.DATA, data
		));
	}

	private void optOutReward(JSON data)
	{
		Debug.LogFormat("Opt out reward {0}", data.ToString());
		Server.unregisterEventDelegate("opt_out_reward");
		bool success = data.getBool("success", false);
		if (!success)
		{
			GenericDialog.showDialog(
					Dict.create(
						D.TITLE, Localize.text(OPT_IN_GENERIC_HEADER),
						D.MESSAGE, Localize.text(OPT_IN_GENERIC_MESSAGE),
						D.REASON, "opt-in-reard-not-granted"
					),
					SchedulerPriority.PriorityType.IMMEDIATE
				);
		}
		else
		{
			
			long rewardAmount = data.getLong("rewardAmount", 0L);
			Debug.LogFormat("Email opt in Reward Amount {0}", rewardAmount);
			//Add reward amount 
			SlotsPlayer.addCredits(rewardAmount, "reward amount for opting in for email LL");

			StatsManager.Instance.LogEconomy
			(
				currencyName: "free_credits",
				amount: (int)rewardAmount,
				kingdom: "zis",
				phylum: "gdpr_dialog",
				klass: ZisEmailOptInDialog.statsLocation
			);
		}
		ZisEmailOptInDialog.statsLocation = "";
	}

	private void onConnectCallback(JSON data)
	{
		Debug.Log("Connect Callback: " + data);
		if (data.getString("type", "none") == "network_connect")
		{
			// Then this is indeed the right callback. This really shouldn't be happening if this isnt the case.
			bool isSuccess = data.getBool("success", false);
			bool isPending = (data.getString("network_state.status", "none") == "pending");

			if (isSuccess)
			{
				if (!isPending)
				{
					currentState = NetworkState.CONNECTED;
					if (incentiveCredits <= 0)
					{
						// Only show the congrats here if they are not getting a reward. Otherwise the incentive
						// event from the server will properly show this dialog.
						string email = data.getString("network_state.email", "No email");
						LinkedVIPCongrats.showDialog(email);
					}
				}
				else if (isPending)
				{
					currentState = NetworkState.PENDING;
					LinkedVIPPendingDialog.showDialog(Dict.create(D.DATA, data));
				}
				
			}
			else
			{
				string reason = data.getString("reason", "???");
				Scheduler.addDialog("generic", Dict.create(
						D.TITLE, "ERROR!",
							D.MESSAGE, ("Connection has failed with reason: " + Localize.text(reason)))
				);
			}
		}
		else
		{
			Debug.LogError("NetworkConnectDialog -- wrong type in response.");
		}
	}

	public void onUnpauseCheck(JSON data)
	{
		string status = data.getString("network_state.status", "nothing");
		if (incentiveCredits == 0)
		{
			// Only show the congrats dialog here if we don't have an incentive.
			// There is an incentive event that will show the congrats dialog in that instance.
			if (status == NetworkAction.CONNECTED)
			{
				// Show the congrats dialog if we are now connected.
				string email = data.getString("network_state.email", "No email");
				LinkedVIPCongrats.showDialog(email);
				currentState = NetworkState.CONNECTED;
			}
		}
	}
	
#region FEATURE_BASE_OVERRIDES
	protected override void initializeWithData(JSON data)
	{
		// Update the players vip data here.
		JSON linkedVipStatusJson = data.getJSON("player.vip_status");
		if (linkedVipStatusJson != null)
		{
			setLinkedProgramData(linkedVipStatusJson);
		}
		// Eventually we'll drop the incentive value for connecting to linked VIP here as well.
		// We don't have experiment data yet, and these values still exist
		// even if we are not in the experiment, so read them regardless.
		string status = data.getString ("player.network_state.status", "nothing");
		if (status == NetworkAction.CONNECTED)
		{
			currentState = NetworkState.CONNECTED;
			StatsManager.Instance.LogCount("start_session", "loyalty_lounge", "ll_connected");
		}
		else if (status == NetworkAction.PENDING)
		{
			currentState = NetworkState.PENDING;
		}
		else
		{
			currentState = NetworkState.NONE;
		}
		incentiveCredits = data.getLong("player.network_connect_incentive.credits", 0);
		helpShiftURL = Data.liveData.getString("LINKED_VIP_HELP_URL", "");
	}
#endregion
}

