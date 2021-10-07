using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Com.Scheduler;
using TMPro;

public enum EliteAccessState
{
	FIRST_ACCESS = 0,
	SECOND_ACCESS,
	EXPIRED,
	REJOIN
}
public class EliteAccessDialog : DialogBase
{
	[SerializeField] private TextMeshPro titleLabel;
	[SerializeField] private TextMeshPro accessStatusLabel;
	[SerializeField] private TextMeshPro accessBodyLabel;
	[SerializeField] private ObjectSwapper stateSwapper;
	[SerializeField] private ButtonHandler okayButton ;
	[SerializeField] private EliteRewardListItem[] rewardItems;

	private const string DIALOG_OPEN_WELCOME = "DialogueWelcomeOpenElite";

	private EliteAccessState dialogState = EliteAccessState.FIRST_ACCESS;

	public override void init()
	{
		if (dialogArgs != null && dialogArgs.containsKey(D.STATE))
		{
			dialogState = (EliteAccessState) dialogArgs[D.STATE];
		}
		 
		okayButton.registerEventDelegate(onButtonClicked);
		setDialogState(dialogState);
		Audio.play(DIALOG_OPEN_WELCOME);
		
		if (dialogState == EliteAccessState.FIRST_ACCESS)
		{
			//Preload the audio assets for the elite tansition sounds
			preloadSound();
		}
	}
	
	public override void close()
	{
		if (dialogState == EliteAccessState.SECOND_ACCESS)
		{
			return;
		}

		if (GameState.game != null)
		{
			Dict args = Dict.create(D.TYPE, LobbyInfo.Type.MAIN);
			Scheduler.addFunction(LobbyLoader.returnToLobbyAfterDialogCloses, args);
		}
		else if (MainLobby.hirV3 != null)
		{
			MainLobby.hirV3.handleEliteTransition();
		}
	}
	
	private void preloadSound()
	{
		AudioInfo lobbySound1 = AudioInfo.find("LobbyTransitionTo1Elite");
		if (lobbySound1 != null)
		{
			lobbySound1.prepareClip();
		}

		AudioInfo lobbySound2 = AudioInfo.find("LobbyTransitionTo2Elite");
		if (lobbySound2 != null)
		{
			lobbySound2.prepareClip();
		}
	}

	private void onButtonClicked(Dict args = null)
	{
		Dialog.close();

		if (dialogState == EliteAccessState.EXPIRED)
		{
			StatsElite.logAccessExpired();
		}

		if (dialogState == EliteAccessState.REJOIN)
		{
			StatsElite.logAccessRejoin();
			//Only show the Elite main dialog if pressing the Okay button in the Rejoin state 
			EliteDialog.showDialog();
		}
	}

	private void setDialogState(EliteAccessState currentState)
	{
		int durationInDays = EliteManager.passDuration / Common.SECONDS_PER_DAY;
		
		switch (currentState)
		{
			case EliteAccessState.FIRST_ACCESS:
				stateSwapper.setState("elite_status");
				titleLabel.text = Localize.text("elite_status_join_title");
				accessStatusLabel.text = Localize.text("elite_status_join", durationInDays);
				accessBodyLabel.text = Localize.text("elite_status_join_body", durationInDays);
				StatsElite.logDialogView("", "active", 1);
				break;
			case EliteAccessState.SECOND_ACCESS:
				stateSwapper.setState("elite_status");
				titleLabel.text = Localize.text("elite_status_renew_title");
				accessStatusLabel.text = Localize.text("elite_status_renew", durationInDays);
				accessBodyLabel.text = Localize.text("elite_status_renew_body", durationInDays);
				StatsElite.logDialogView("", "active", 2);
				break;
			case EliteAccessState.EXPIRED:
				stateSwapper.setState("elite_status");
				titleLabel.text = Localize.text("elite_status_expired_title");
				accessStatusLabel.text = Localize.text("elite_status_expired");
				accessBodyLabel.text = Localize.text("elite_status_expired_body");
				break;
			case EliteAccessState.REJOIN:
				titleLabel.text = Localize.text("elite_status_rejoin_title");
				stateSwapper.setState("elite_rejoin");
				if (rewardItems != null)
				{
					for (int i = 0; i < rewardItems.Length; i++)
					{
						rewardItems[i].setup(i, null);
					}
				}
				break;
		}
	}
	
	/*=========================================================================================
		SHOW DIALOG CALL
	=========================================================================================*/
	public static void showDialog(Dict args = null, SchedulerPriority.PriorityType priority = SchedulerPriority.PriorityType.HIGH)
	{
		Scheduler.addDialog("elite_access_dialog", args, priority);
	}
}
