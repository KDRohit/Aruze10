using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

public class RobustChallengesPlayAgainDialog : DialogBase
{
	public const string COMPLETE_BACKGROUND_IMAGE_PATH = "robust_challenges/Robust_Challenges_BG_Green.png";

	[SerializeField] private TextMeshPro titleLabel;
	[SerializeField] private TextMeshPro descriptionLabel;
	[SerializeField] private TextMeshPro timerLabel;
	[SerializeField] private Renderer backgroundRenderer;
	[SerializeField] private ButtonHandler mainLobbyButton;
	[SerializeField] private ButtonHandler playAgainButton;
	private long coinsWon = 0L;
	private static string tempMotdKey;
	private const string DEFAULT_IMAGE_PATH = "bigger_better_challenge_neutral.png";

	private bool clickedMainLobby = false;
	
	public override void init()
	{
		downloadedTextureToRenderer(backgroundRenderer, 0);
		mainLobbyButton.registerEventDelegate(onClickMainLobby);
		playAgainButton.registerEventDelegate(onClickPlayAgain);
		
		DialogData dialogData = campaign.missions[campaign.missions.Count-1].getDialogByState(ChallengeCampaign.REPLAY);
		if (dialogData != null)
		{
			titleLabel.text = dialogData.titleText;
			descriptionLabel.text = dialogData.description;
		}
		
		StatsManager.Instance.LogCount("dialog", "robust_challenges_motd", campaign.variant, GameState.game != null ? GameState.game.keyName : "", (campaign.currentEventIndex + 1).ToString(), "view");
	}

	protected override void onFadeInComplete()
	{
		playAudioFromEos("ReplayDialogOpenRChallenge");
		base.onFadeInComplete();
	}

	protected virtual void Update()
	{
		AndroidUtil.checkBackButton(onCloseButtonClicked);
	}

	public override void close()
	{
		//
	}

	public override void onCloseButtonClicked(Dict args = null)
	{
		Audio.play("minimenuclose0");
		{
			StatsManager.Instance.LogCount("dialog", "robust_challenges_motd", CampaignDirector.robust.variant, GameState.game != null ? GameState.game.keyName : "", (campaign.currentEventIndex + 1).ToString(), "close");
		}
		// We need to sync with server on new reward values etc.  otherwise, we might run into desync issue.
		restartCampaign();
	}

	private void onClickPlayAgain(Dict args = null)
	{
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "robust_challenges_motd",
			phylum:CampaignDirector.robust.variant,
			klass: "robust_challenges_replay",
			genus: "click"
		);
		restartCampaign();
	}

	private void onClickMainLobby(Dict args = null)
	{
		clickedMainLobby = true;
		restartCampaign();

	}

	private void restartCampaign(Dict args = null, GameTimerRange sender = null)
	{
		// call update
		// We finished the current round of robust challenge and starts another round of it.  We need to sync with
		// the server to get new mission reward values, etc.  Otherwise, we might run into desync issue later.
		RobustChallengesAction.getCampaignRestartData(campaign.campaignID, onCampaignUpdate);
	}

	private void onCampaignUpdate(JSON data)
	{
		JSON clientData = data.getJSON("clientData");
		campaign.restart(); //Need to reset the client UI first before refreshing data in case the new data says we're on the final event that can't replay anymore
		campaign.init(clientData);
		Scheduler.addFunction(ChallengeCampaign.updateUI);
		if (SpinPanel.hir != null && SpinPanel.hir.featureButtonHandler != null)
		{
			bool isRobustChallengesGame = RobustCampaign.hasActiveRobustCampaignInstance && CampaignDirector.robust.currentMission != null;
			SpinPanel.hir.featureButtonHandler.showRobustChallengesInGame(isRobustChallengesGame);
		}
	
		if (clickedMainLobby)
		{
			if (GameState.game != null)
			{
				NGUIExt.disableAllMouseInput();
				Loading.show(Loading.LoadingTransactionTarget.LOBBY);
				Glb.loadLobby();
			}
			
			Dialog.close();
			
		}
		else
		{
			Dialog.close();
			RobustChallengesObjectivesDialog.showDialog();
		}
	
	}
	private static string completeBackgroundImagePath
	{
		// If the path is emtpty, return the default image path.
		get
		{
			string path = null;
			DialogData dialogData = campaign.missions[campaign.missions.Count-1].getDialogByState(ChallengeCampaign.REPLAY);
			if (dialogData != null)
			{
				path = dialogData.backgroundImageURL;
			}
			
			// If there was no path, use this as the default
			if (string.IsNullOrEmpty(path))
			{
				path = DEFAULT_IMAGE_PATH;
			}

			// path is really just the image, so make sure we're in the challenges folder
			path = "challenges/" + path;
			
			return path;
		}
	}

	// Fetch latest progress data before showing up the dialog.
	public static bool showDialog()
	{
		Dialog.instance.showDialogAfterDownloadingTextures("robust_challenges_replay",new[] {completeBackgroundImagePath});
		return true;
	}



    /*=========================================================================================
    GETTERS  
    =========================================================================================*/
	
	// Shortcut getter.
    public static RobustCampaign campaign
    {
        get
        {
			return CampaignDirector.robust;
        }
    }
}
