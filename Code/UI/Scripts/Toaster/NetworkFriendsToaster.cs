using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using UnityEngine;
using TMPro;

public class NetworkFriendsToaster : Toaster 
{
	[SerializeField] private TextMeshPro messageLabel;
	[SerializeField] private TextMeshPro buttonLabel;

	[SerializeField] private ClickHandler showFriendHandler;
	[SerializeField] private ImageButtonHandler showProfileButtonHandler;

	private const float TOASTER_DISPLAY_TIME = 6.0f;
	private const string MESSAGE_NEW_FRIEND_LOC_FORMAT = "friends_new_friend";
	private const string MESSAGE_NEW_REQUEST_LOC_FORMAT = "friends_new_request";
	private const string MESSAGE_BUTTON_LOC_FORMAT = "friends_view_request";
	private const string MESSAGE_FRIENDS_AND_REQUESTS_LOC_FORMAT = "friends_new_friends_and_requests";
	
	// Sound names
	private const string TOASTER_INTRO_SOUND = "ToastInNetworkAchievements";	

	private int newFriends = 0;
	private int newRequests = 0;
	
	public override void init(ProtoToaster proto)
	{
		newFriends = NetworkFriends.instance.newFriends;
		newRequests = NetworkFriends.instance.newFriendRequests;

		if (0 == newFriends && 0 == newRequests)
		{
			//invalid case
			close();
		}
		else
		{
			Audio.play(TOASTER_INTRO_SOUND);
			string data = null;
			//data passd in is for friend name (not currently supported by the server)
			if (null != proto.args && proto.args.containsKey(D.OPTION))
			{
				data = proto.args[D.OPTION] as string;
			}
			setMessageText(newFriends, newRequests, data);

			showFriendHandler.registerEventDelegate(onClick);
			showProfileButtonHandler.registerEventDelegate(onClick);

			base.init(proto);
		}
		
		
	}

	protected override void introAnimation()
	{
		base.introAnimation();

		//update the display time for this toaster
		runTimer = new GameTimer(TOASTER_DISPLAY_TIME); 
	}

	//Set message text based on number of new friends and new friend requests
	private void setMessageText(int newFriends, int newRequests, string data)
	{
		if (newFriends > 0 && newRequests > 0)
		{
			messageLabel.text = Localize.text(MESSAGE_FRIENDS_AND_REQUESTS_LOC_FORMAT);	
		}
		else
		{
			string message = newFriends > 0 ? MESSAGE_NEW_FRIEND_LOC_FORMAT : MESSAGE_NEW_REQUEST_LOC_FORMAT; 

			if (newFriends > 1) { message += "s"; }
			else if (newRequests > 1) { message += "s"; }

			messageLabel.text = Localize.text(message);
		}

		buttonLabel.text = Localize.text(MESSAGE_BUTTON_LOC_FORMAT);
	}

	public override void close()
	{
		NetworkFriends.instance.onToasterClose();
		showFriendHandler.clearAllDelegates();
		showProfileButtonHandler.clearAllDelegates();
		base.close();
	}


	//Click handler that opens the network profile dialog
	private void onClick(Dict args = null)
	{
		int dialogMode = (newRequests > newFriends) ? NetworkProfileDialog.MODE_FRIEND_REQUESTS : NetworkProfileDialog.MODE_ALL_FRIENDS;
		NetworkProfileDialog.showDialog(SlotsPlayer.instance.socialMember, SchedulerPriority.PriorityType.IMMEDIATE, null, dialogMode);
		close();
	}

}
