using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using UnityEngine;
using TMPro;

public class TicketTumblerMOTD : DialogBase, IResetGame 
{

	[SerializeField] private Renderer background;
	[SerializeField] private TextMeshPro textLabel;
	[SerializeField] private ButtonHandler closeHandler;
	[SerializeField] private ButtonHandler playHandler;

	private static bool hasBeenViewed = false;

	private string statName = "";
	
	public override void init()
	{
		downloadedTextureToRenderer(background, 0);

		if (TicketTumblerFeature.instance.numTicketsForCollectablesPack >= 0)
		{
			textLabel.text = TicketTumblerFeature.instance.numTicketsForCollectablesPack.ToString();
		}
		else
		{
			textLabel.gameObject.SetActive(false);
		}
		
		closeHandler.registerEventDelegate(closeClicked);
		playHandler.registerEventDelegate(playClicked);

		hasBeenViewed = true;
		PlayerAction.markMotdSeen("ticket_tumbler_motd", true);

	}

	public void closeClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount("dialog", "ticket_tumbler_motd", statName, "close", "click");
		Dialog.close();
	}

	public void playClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount("dialog", "ticket_tumbler_motd", statName, "", "click");
		Dialog.close();
		//DoSomething.now(action);
	}

	// Called by Dialog.close() - do not call directly.	
	public override void close()
	{

	}

	public static void addDialog(SchedulerPriority.PriorityType priorityType = SchedulerPriority.PriorityType.LOW, bool shouldMakeArgs = false)
	{
		Dict args = null;

		if (shouldMakeArgs)
		{
			args = Dict.create
			(
				D.IS_LOBBY_ONLY_DIALOG, GameState.isMainLobby,
				D.MOTD_KEY, "ticket_tumbler_motd"
			);
		}

		string imagePath = "Features/Ticket Tumbler/Textures/TicketTumbler_V2_BonusCoins_MOTD";
		if (TicketTumblerFeature.instance.numTicketsForCollectablesPack >= 0)
		{
			imagePath = "Features/Ticket Tumbler/Textures/TicketTumbler_V2_CoinsAndCards_MOTD";
		}
		
		Dialog.instance.showDialogAfterDownloadingTextures("ticket_tumbler_motd", imagePath, args, false, priorityType, true);
	}

	// called from feature dialog
	public static void showDialogFromFeature(SchedulerPriority.PriorityType priorityType = SchedulerPriority.PriorityType.LOW)
	{
		addDialog(priorityType);
	}

	// called from motdframework
	public static bool showDialog(SchedulerPriority.PriorityType priorityType = SchedulerPriority.PriorityType.LOW)
	{
		if (hasBeenViewed)
		{
			// this only gets called from motdframework, so if we already saw it this session don't show again this way
			return false;
		}

		hasBeenViewed = true;

		addDialog(priorityType, true);
		return true;

	}

	public static void resetStaticClassData()
	{
		hasBeenViewed = false;
	}
}
