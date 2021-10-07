using UnityEngine;
using Com.Scheduler;
using TMPro;
using Zynga.Core.Util;
using System.Collections;
using QuestForTheChest;

public class RobustChallengesTypeBlock : MonoBehaviour 
{
	public Renderer gameIconRenderer;
	public Animator animator;

	[SerializeField] protected TextMeshPro objectiveHeaderLabel;
	[SerializeField] private TextMeshPro coinAmountLabel;
	[SerializeField] private TextMeshPro goalAmountLabel;
	[SerializeField] private TextMeshPro goalRestrictionLabel;
	[SerializeField] private GameObject coinIcon;
	[SerializeField] private GameObject xInYCoinIcon;
	[SerializeField] private TextMeshPro objectiveDescriptionLabel;
	[SerializeField] private TextMeshPro objectiveMinWagerLabel;
	[SerializeField] private TextMeshPro minWagerDescriptionLabel;

	[SerializeField] private TextMeshPro progress;
	[SerializeField] private UIMeterNGUI progressMeter;
	[SerializeField] private GameObject anyGameTag;
	[SerializeField] private ButtonHandler playButton;
	[SerializeField] private TextMeshPro playButtonText;
	[SerializeField] private GameObject dailyBonusSprite;
	[SerializeField] private GameObject welcomeJourneySprite;

	private string gameKey = "";

	public virtual void init(Objective objective, bool isFinalCompletedObjective)
	{
		// Set game icons and play button.
		if (!string.IsNullOrEmpty(objective.game))
		{
			gameKey = objective.game;
			playButton.registerEventDelegate(blockClicked, Dict.create(D.GAME_KEY, gameKey));
			if (GameState.game != null) //If we are in a game
			{
				if (GameState.game.keyName == objective.game) //If we are in the same game as the objective is
				{
					playButton.gameObject.SetActive(false);
				}
			}
		}
		else // If there is no game key or game key is "", show the HIR logo.
		{
			playButton.gameObject.SetActive(false);
			if (anyGameTag != null)
			{
				anyGameTag.SetActive(objective.type != Objective.DAILY_BONUS && objective.type != Objective.WELCOME_JOURNEY);
			}
		}

		objective.formatSymbol();
		objective.buildLocString(includeCredits:false);

		// Set description message.
		if (objective.usesTwoPartLocalization())
		{
			setHeaderAndDescription(objective);
		}
		else
		{
			bool abbr = objective.type == XDoneYTimesObjective.WIN_X_COINS_Y_TIMES;
			objectiveHeaderLabel.text = objective.getDynamicChallengeDescription(abbr);
			setProgressText(objective, false);
		}
		

		//Update the play button state for certain opjectives:
		initPlayButtonAndGameTag(objective);

		progressMeter.maximumValue = objective.progressBarMax;
		progressMeter.currentValue = objective.currentAmount;

		// When a challenge type is complete, show completion mark, disable progress bar and play button.
		if (progressMeter.maximumValue <= progressMeter.currentValue)
		{
			if (!isFinalCompletedObjective)
			{
				progressMeter.currentValue = progressMeter.maximumValue;
				if (animator != null)
				{
					animator.Play("checkmark_hold");
				}
			}

			playButton.gameObject.SetActive(false);
		}
	}

	private void setHeaderAndDescription(Objective objective)
	{
		objectiveHeaderLabel.text = objective.getChallengeTypeActionHeader().ToUpper();
		switch (objective.type)
		{
			case Objective.CREDITS:
			case Objective.CREDITS_BET:
			case Objective.CREDITS_WON:
				// Show percentage for credit challenge.
				setPercentageProgressText(objective);
				setCoinAmountLabel(objective);
				if (objective.minWager > 0)
				{
					setMinWagerText(objective, false);
				}

				break;

			case Objective.DAILY_BONUS:
				setProgressText(objective);
				SafeSet.gameObjectActive(dailyBonusSprite, true);
				if (gameIconRenderer != null)
				{
					SafeSet.gameObjectActive(gameIconRenderer.gameObject, false);
				}

				if (objective.isComplete)
				{
					dailyBonusSprite.GetComponent<UISprite>().color = Color.gray;
				}
				break;

			case Objective.WELCOME_JOURNEY:
				setProgressText(objective);
				SafeSet.gameObjectActive(welcomeJourneySprite, true);
				SafeSet.gameObjectActive(gameIconRenderer.gameObject, false);
				if (objective.isComplete)
				{
					welcomeJourneySprite.GetComponent<UISprite>().color = Color.gray;
				}
				break;

			case XinYObjective.X_COINS_IN_Y:
				{
					XinYObjective xInY = objective as XinYObjective;
					long numSpins = 1;
					if (xInY != null)
					{
						if (xInY.constraints == null)
						{
							Debug.LogError("Invalid x in y challenge constraints");
							numSpins = 0;
						}
						else
						{
							Objective.Constraint activeConstraint = xInY.constraints[0];
							numSpins = xInY.isComplete
								? activeConstraint.limit
								: activeConstraint.limit - activeConstraint.amount;

							if (numSpins < 0)
							{
								numSpins = 0;
							}
						}
						
					}
					setPercentageProgressText(objective);
					xInYCoinIcon.SetActive(true);
					long remainingAmount = objective.isComplete ? objective.amountNeeded : objective.amountNeeded - objective.currentAmount;
					if (remainingAmount < 0)
					{
						remainingAmount = 0;
					}
					goalAmountLabel.text = CreditsEconomy.convertCredits(remainingAmount);
					if (numSpins > 1)
					{
						goalRestrictionLabel.text = Localize.text("in_{0}_spins", numSpins);	
					}
					else
					{
						goalRestrictionLabel.text = Localize.text("in_{0}_spin", numSpins);
					}
					
					SafeSet.gameObjectActive(goalAmountLabel.gameObject, true);
					SafeSet.gameObjectActive(goalRestrictionLabel.gameObject, true);
				}
				break;

			default:
				setProgressText(objective);
				break;
		}
	}

	private void initPlayButtonAndGameTag(Objective objective)
	{
		if (playButton == null || 
		    playButton.gameObject == null || 
		    playButtonText == null ||
		    playButtonText.gameObject == null)
		{
			return;
		}

		switch (objective.type)
		{
			case Objective.QFC_KEY_COLLECT:
			case Objective.QFC_WIN:
				playButton.gameObject.SetActive(QuestForTheChestFeature.instance.isEnabled);
				if (anyGameTag != null)
				{
					anyGameTag.SetActive(false);
				}
				playButton.registerEventDelegate((Dict args) =>
				{
					Scheduler.addDialog("quest_for_the_chest_map"); Dialog.close();
				});
				break;
			case Objective.FINISH_PPU:
				playButton.gameObject.SetActive(true);
				if (anyGameTag != null)
				{
					anyGameTag.SetActive(false);
				}
				playButton.registerEventDelegate((Dict args) =>
				{
					Scheduler.addDialog("partner_power_intro", null); Dialog.close();
				});
				break;
			case Objective.PURCHASE_COINS:
			case Objective.PINATA_PURCHASE:
				playButton.gameObject.SetActive(true);
				if (anyGameTag != null)
				{
					anyGameTag.SetActive(false);
				}
				playButtonText.text = Localize.text("robust_challenges_buy");
				playButton.registerEventDelegate((Dict args) =>
				{
					BuyCreditsDialog.showDialog();
					Dialog.close();
				});
				break;
			case Objective.INVITE_NEW_FRIENDS:
				if (anyGameTag != null)
				{
					anyGameTag.SetActive(false);
				}
#if UNITY_WEBGL
				playButton.gameObject.SetActive(false);
#else
				playButton.gameObject.SetActive(true);
				playButtonText.text = Localize.text("robust_challenges_share");
				playButton.registerEventDelegate((Dict args) =>
				{
					friendCodeClicked(null);
				});
#endif
				break;
			case Objective.GIFTED_SPINS:
				playButton.gameObject.SetActive(true);
				if (anyGameTag != null)
				{
					anyGameTag.SetActive(false);
				}
				playButton.registerEventDelegate((Dict args) =>
				{
					InboxDialog.showDialog(InboxDialog.SPINS_STATE);
					Dialog.close();
				});
				break;
			case Objective.FINISH_TOP_ROYAL_RUSH:
				if (RoyalRushEvent.instance != null && RoyalRushEvent.instance.rushInfoList != null && RoyalRushEvent.instance.rushInfoList.Count > 0)
				{
					RoyalRushInfo currentRushInfo = RoyalRushEvent.instance.rushInfoList[0];
					if (currentRushInfo == null || currentRushInfo.currentState == RoyalRushInfo.STATE.AVAILABLE && !currentRushInfo.inWithinRegistrationTime())
					{
						break; //Don't turn on the play button if the player never registered for the current event and we're past registration time
					}
					playButton.gameObject.SetActive(true);
					if (anyGameTag != null)
					{
						anyGameTag.SetActive(false);
					}

					playButton.registerEventDelegate(royalRushClicked);
				}
				else
				{
					playButton.gameObject.SetActive(false);
				}
				break;
			case Objective.TICKET_TUMBLER_TOKENS_COLLECT:
				playButton.gameObject.SetActive(true);
				if (anyGameTag != null)
				{
					anyGameTag.SetActive(false);
				}
				playButton.registerEventDelegate((Dict args) =>
				{
					Scheduler.addDialog("ticket_tumbler", null); Dialog.close();
				});
				break;
			case Objective.VIP_MINI_GAME:
			case Objective.VIP_TOKENS_COLLECT:
				playButton.gameObject.SetActive(true);
				if (anyGameTag != null)
				{
					anyGameTag.SetActive(false);
				}
				playButton.registerEventDelegate((Dict args) =>
				{
					if (!GameState.isMainLobby)
					{
						Dict args2 = Dict.create(D.TYPE, LobbyInfo.Type.VIP);
						Scheduler.addFunction(LobbyLoader.returnToLobbyAfterDialogCloses, args2);
					}
					else
					{
						if (VIPRoomBottomButton.instance != null) { VIPRoomBottomButton.instance.triggerClick(); }
					}
					Dialog.close();
				});
				break;
			case Objective.MAX_VOLTAGE_MINI_GAME:
			case Objective.MAX_VOLTAGE_TOKENS_COLLECT:
				playButton.gameObject.SetActive(true);
				if (anyGameTag != null)
				{
					anyGameTag.SetActive(false);
				}
				playButton.registerEventDelegate((Dict args) =>
				{
					if (!GameState.isMainLobby)
					{
						Dict args2 = Dict.create(D.TYPE, LobbyInfo.Type.MAX_VOLTAGE);
						Scheduler.addFunction(LobbyLoader.returnToLobbyAfterDialogCloses, args2);
					}
					else
					{
						MOTDFramework.queueCallToAction(MOTDFramework.MAX_VOLTAGE_LOBBY_CALL_TO_ACTION);
					}
					Dialog.close();
				});
				break;
			case Objective.SLOTVENTURE_WIN:
				if (SlotventuresLobby.assetData != null)
				{
					playButton.gameObject.SetActive(true);
					if (anyGameTag != null)
					{
						anyGameTag.SetActive(false);
					}
					playButton.registerEventDelegate((Dict args) =>
					{
						Dict dict = Dict.create(D.MOTD_KEY, "", D.THEME, SlotventuresLobby.assetData.themeName);
						Scheduler.addDialog("slotventure_motd", dict);
						Dialog.close();
					});
				}
				else
				{
					playButton.gameObject.SetActive(false);
				}
				break;
			default: break;
		}
	}

	private void setProgressText(Objective objective, bool setDescription = true)
	{
		int progressAmount = (int)Mathf.Min(objective.currentAmount, objective.progressBarMax);
		progress.text = CommonText.formatNumber(progressAmount) + "/" + CommonText.formatNumber(objective.progressBarMax);
		if (objective.minWager > 0)
		{
			setMinWagerText(objective, true);
		}
		else if (setDescription)
		{
			objectiveDescriptionLabel.text = objective.getShortDescriptionLocalization();
			objectiveDescriptionLabel.gameObject.SetActive(true);
		}
	}

	private void setMinWagerText(Objective objective, bool useMinWagerDescriptionLabel)
	{
		objectiveMinWagerLabel.text = Localize.text("min_bet_{0}", CreditsEconomy.multiplyAndFormatNumberAbbreviated(objective.minWager));
		objectiveMinWagerLabel.gameObject.SetActive(true);
		if (useMinWagerDescriptionLabel)
		{
			minWagerDescriptionLabel.text = objective.getShortDescriptionLocalization();
			minWagerDescriptionLabel.gameObject.SetActive(true);
		}
	}

	private void setPercentageProgressText(Objective objective)
	{
		int percent = Mathf.RoundToInt((100 * objective.currentAmount) / objective.progressBarMax);
		percent = (int)Mathf.Min((float)percent, 100f); //If we've completed the challenge and we're over 100%, only display 100%
		progress.text = Localize.text("{0}_percent", percent.ToString());
	}

	private void setCoinAmountLabel(Objective objective)
	{
		coinIcon.SetActive(true);
		coinAmountLabel.text = objective.getShortDescriptionLocalization();
		coinAmountLabel.gameObject.SetActive(true);
	}

	// User can click on the block to enter the targeted game.
	private void blockClicked(Dict args = null)
	{
		string gameKey = (string)args.getWithDefault(D.GAME_KEY, "");
		logClick();

		// Load the game right now.
		// Tell the lobby which game to launch when finished returning to the lobby.
		PreferencesBase prefs = SlotsPlayer.getPreferences();
		prefs.SetString(Prefs.AUTO_LOAD_GAME_KEY, gameKey);
		prefs.Save();

		SlotAction.setLaunchDetails(RobustCampaign.LAUNCH_DETAIL);

		if (GameState.isMainLobby)
		{
			// Refresh the lobby if already in it during game unlock,
			// so the unlocked game will appear unlocked, and so we
			// actually launch into that game
			Scheduler.addFunction(MainLobby.refresh);
		}
		else
		{
			// Currently in a game.
			// First go back to the lobby and go through the common route to launching a game.
			Scheduler.addFunction(LobbyLoader.returnToLobbyAfterDialogCloses);
		}
		
		Audio.play("minimenuclose0");
		Dialog.close();
	}

	private void royalRushClicked(Dict args = null)
	{
		RoyalRushInfo currentRushInfo = RoyalRushEvent.instance.rushInfoList[0];
		if (currentRushInfo.currentState == RoyalRushInfo.STATE.AVAILABLE)
		{
			logClick();
			PreferencesBase prefs = SlotsPlayer.getPreferences();
			prefs.SetString(Prefs.AUTO_LOAD_GAME_KEY, currentRushInfo.gameKey);
			prefs.Save();
			SlotAction.setLaunchDetails("royal rush");
			if (GameState.isMainLobby)
			{
				Scheduler.addFunction(MainLobby.refresh);
			}
			else
			{
				Scheduler.addFunction(LobbyLoader.returnToLobbyAfterDialogCloses);
			}
		
			Audio.play("minimenuclose0");
			Dialog.close();
		}
		else
		{
			Dict rushDict = Dict.create(D.DATA, currentRushInfo);
			RoyalRushStandingsDialog.showDialog(rushDict);
			Dialog.close();
		}
	}

	protected virtual void logClick()
	{
		StatsManager.Instance.LogCount("dialog", "robust_challenges_motd", CampaignDirector.robust.variant, GameState.game != null ? GameState.game.keyName : "", (CampaignDirector.robust.currentEventIndex + 1).ToString(), "click");
	}

	private void friendCodeClicked(Dict args = null)
	{
		string friendCodeShareText = "";

	// FRIEND_CODE_SHARING_URL is a bit.ly link that directs to app.adjust.com. These are used to do User Acquisition tracking.
	// It directs to the mobile landing page. We use the mobile landing page to get users to the right platform app store.
	string url = Data.liveData.getString("FRIEND_CODE_SHARING_URL", "");
	if (!string.IsNullOrEmpty(url))
	{
		friendCodeShareText = Localize.text("friend_code_share_with_link_{0}_{1}", SlotsPlayer.instance.socialMember.networkProfile.friendCode, url);
	}
	else
	{
		friendCodeShareText = Localize.text("friend_code_share_{0}", SlotsPlayer.instance.socialMember.networkProfile.friendCode);
	}

#if !UNITY_EDITOR
#if UNITY_WEBGL 
		WebGLFunctions.copyTextToClipboard(friendCodeShareText);
#else
		NativeBindings.ShareContent(
			subject:"Friend Code",
			body:friendCodeShareText,
			imagePath:"",
			url:"");
#endif
#else
		Debug.LogError("Sharing code: " + friendCodeShareText);
#endif
	}

}
