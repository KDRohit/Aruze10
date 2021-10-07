using Com.Scheduler;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zynga.Core.Util;

namespace Com.HitItRich.EUE
{
	public class EueFtueRichDialog : DialogBase, IResetGame
	{
		private const float DIALOG_CAMERA_OFFSET = 10000;
		private const float LOBBY_OPTION_Z_DEPTH = 10;
		private const float BRIEFCASE_DELAY = 1.0f;
		private const string CHARACTER_INTRO = "Intro";
		private const string CHARACTER_OUTRO = "Outro";
		private const string CHARACTER_OFF = "Off";

		private const float CHALLENGE_INTRO_TIMEOUT = 30.0f;

		private const string BRIEFCASE_INTRO = "Intro";
		private const string BRIEFCASE_OUTRO = "Outro";

		private const string FINGER_INTRO = "Intro";
		private const string FINGER_OUTRO = "Outro";

		private const string FUTE_BRIEFCASE_TEXT = "ftue_briefcase";
		private const string FTUE_FORCED_DAILY_BONUS_TEXT = "forced_daily_bonus_collect_ftue";
		private const string FTUE_GAME_INTRO_TEXT = "ftue_game_intro";
		private const string FTUE_CHALLENE_INTRO_TEXT = "ftue_challenge_intro";
		private const string FTUE_CHALLENGE_COMPLETE_TEXT = "ftue_challenge_complete";

		private const string RICH_APPEARS_LOBBY_SOUND = "FTUERichAppearsLobby";
		private const string RICH_APPEARS_GAME_SOUND = "FTUERichAppearsGame";
		private const string COLLECT_COINS_SOUND = "FTUECollectCoins";
		private const string DAILY_BONUS_COLLECT_SOUND = "FTUEDailyBonusCollect";
		public const string LEVEL_UP_SOUND = "FTUELevelUp";
		
		public enum OverlayState
		{
			FIRST_LOGIN,
			FORCED_BONUS,
			GAME_INTRO,
			CHALLENGE_INTRO,
			CHALLENGE_COMPLETE,
		}
	
		[SerializeField] private RuntimeAnimatorController briefcaseIntro;
		[SerializeField] private RuntimeAnimatorController dailyBonusIntro;
		[SerializeField] private RuntimeAnimatorController gameIntro;
		[SerializeField] private RuntimeAnimatorController challengeIntro;
		[SerializeField] private RuntimeAnimatorController challengeComplete;
		[SerializeField] private GameObject briefcase;
		[SerializeField] private Animator briefcaseAnimator;
		[SerializeField] private ButtonHandler briefcaseCollectButton;
		[SerializeField] private GameObject finger;
		[SerializeField] private Animator fingerAnimator;
		[SerializeField] private ClickHandler fullScreenButton;
		[SerializeField] private ButtonHandler loginButton;
		[SerializeField] private EUECharacterItem characterItem;

		public static bool isLoading = false;
		public static bool skipFTUE = false;
		private static bool didTimeout = false;
		private static bool runCallbackRoutine = false;
		
		
		private Dictionary<OverlayState, System.Action> stateInitializationFunctions;
		private GameObject clonedLobbyOption;
		private OverlayState currentState;
		private ButtonHandler bonusCollectButton;
		private ClickHandler clonedFullScreenButton;
		private bool isClosing;

		private void setupStateDictionary()
		{
			stateInitializationFunctions = new Dictionary<OverlayState, System.Action>()
			{
				{ OverlayState.FIRST_LOGIN, initFirstLogin },
				{ OverlayState.FORCED_BONUS, initBonusCollect },
				{ OverlayState.GAME_INTRO, initGameIntro },
				{ OverlayState.CHALLENGE_INTRO, initChallengeIntro },
				{ OverlayState.CHALLENGE_COMPLETE, initChallengeComplete }
			};
		}

		private void initFirstLogin()
		{
			StatsManager.Instance.LogCount("dialog", "lobby_ftue", "step", "", "welcome", "view");
			StatsManager.Instance.LogMileStone("ftue", "lobby_ftue");
			SafeSet.gameObjectActive(finger, false);
			SafeSet.gameObjectActive(fullScreenButton.gameObject, false);
			SafeSet.gameObjectActive(briefcase, false);
			characterItem.setText(FUTE_BRIEFCASE_TEXT);
			briefcaseCollectButton.registerEventDelegate(onBriefcaseCollect);
			setCharacterController(briefcaseIntro);
			StartCoroutine(firstLoginPresentation());
		}

		protected override void onFadeInComplete()
		{
			EUEManager.clearPendingDialog();
			base.onFadeInComplete();
		}

		private void initBonusCollect()
		{
			StatsManager.Instance.LogCount("dialog", "lobby_ftue", "step", "", "db_force_flow", "view");
			SafeSet.gameObjectActive(briefcase, false);
			SafeSet.gameObjectActive(finger, false);
			SafeSet.gameObjectActive(fullScreenButton.gameObject, false);
			characterItem.setText(FTUE_FORCED_DAILY_BONUS_TEXT);
			setCharacterController(dailyBonusIntro);
			
			//animate the charactger on
			Audio.play(RICH_APPEARS_LOBBY_SOUND);
			
			StartCoroutine(CommonAnimation.playAnimAndWait(characterItem.animator, CHARACTER_INTRO));
			
			//show the button
			showDailyBonusButtonOnTopOfDialog();
		}

		private void setCharacterController(RuntimeAnimatorController cntrl)
		{
			if (characterItem != null && characterItem.animator != null && characterItem.animator.gameObject != null)
			{
				characterItem.animator.runtimeAnimatorController = cntrl;
				characterItem.animator.Play(CHARACTER_OFF, -1, 1.0f);
			}
		}

		private void initGameIntro()
		{
			StatsManager.Instance.LogCount("dialog", "lobby_ftue", "step", "", "select_a_game", "view");
			SafeSet.gameObjectActive(briefcase, false);
			SafeSet.gameObjectActive(finger, false);
			SafeSet.gameObjectActive(fullScreenButton.gameObject, false);
			characterItem.setText(FTUE_GAME_INTRO_TEXT);
			setCharacterController(gameIntro);
			StartCoroutine(gameIntroAnimation());
			
		}

		

		private IEnumerator firstLoginPresentation()
		{
			Audio.play(RICH_APPEARS_LOBBY_SOUND);
			StartCoroutine(CommonAnimation.playAnimAndWait(characterItem.animator, CHARACTER_INTRO));
			yield return new WaitForSeconds(BRIEFCASE_DELAY);
			SafeSet.gameObjectActive(briefcase, true);
			briefcaseAnimator.Play(BRIEFCASE_INTRO);
		}

		private void repositionFinger(GameObject targetObj)
		{
			// Position the daily bonus button over the lobby one behind it.
			
			if (targetObj != null && finger != null && finger.gameObject != null)
			{
				//find halfway point
				BoxCollider collider = targetObj.GetComponentInChildren<BoxCollider>();

				if (collider != null && collider.gameObject != null)
				{
					CommonTransform.matchScreenPosition(finger.gameObject.transform, collider.gameObject.transform);
				}
			}
		}

		private IEnumerator gameIntroAnimation()
		{
			//clone the game option button
			clonedLobbyOption = showGameButtonOnTopOfDialog();

			//add a click handler for this new cloned object
			clonedFullScreenButton = addClickHandlerForObject(clonedLobbyOption, onGameIntroLobbyOptionClick, -1);

			//move finger on top of cloned lobby button;
			repositionFinger(clonedLobbyOption);
			
			//turn on finger and play animation
			SafeSet.gameObjectActive(finger, true);
			fingerAnimator.Play(FINGER_INTRO);
			
			Audio.play(RICH_APPEARS_LOBBY_SOUND);
			yield return StartCoroutine(CommonAnimation.playAnimAndWait(characterItem.animator, CHARACTER_INTRO));
			
		}

		private ClickHandler addClickHandlerForObject(GameObject targetObj, ClickHandler.onClickDelegate callback, float zOffset)
		{
			GameObject newObject = CommonGameObject.instantiate(fullScreenButton.gameObject, this.transform, true) as GameObject;
			ClickHandler newHandler = null;
			if (newObject != null)
			{
				SafeSet.gameObjectActive(newObject, true);
				newHandler = newObject.GetComponentInChildren<ClickHandler>();
				if (newHandler != null)
				{
					//resize to lobby option button
					resizeClickHandlerToGameObject(targetObj, newObject, true);
					//move ahead of default full screen handler
					Vector3 position = newObject.transform.localPosition;
					position.z += zOffset;
					newObject.transform.localPosition = position;
					//set handler
					newHandler.registerEventDelegate(callback);
				}
			}

			return newHandler;
		}

		private GameObject showGameButtonOnTopOfDialog()
		{
			GameObject newOption = null;
			if (MainLobby.hirV3 != null)
			{
				LobbyOption option = MainLobby.hirV3.getFirstOption();
				if (option != null && option.button != null)
				{
					GameObject obj = option.button.gameObject;
					newOption = CommonGameObject.instantiate(obj, this.transform, true) as GameObject;
					Vector3 newPosition = newOption.transform.localPosition;
					newPosition.x += DIALOG_CAMERA_OFFSET;
					newPosition.z = LOBBY_OPTION_Z_DEPTH;
					newOption.transform.localPosition = newPosition;
					
				}	
			}
			return newOption;
		}

		private GameObject showSpinButtonOnTopOfDialog()
		{
			GameObject newButton = null;
			if (SpinPanel.hir != null)
			{
				MultiClickHandler handler = SpinPanel.hir.multiClickHandler;
				if (handler != null && handler.gameObject != null)
				{
					newButton = CommonGameObject.instantiate(handler.gameObject, this.transform, true) as GameObject;
					//remove text cycler so it doesn't throw errors on cloned object
					TextCycler cycler = newButton.GetComponentInChildren<TextCycler>();
					if (cycler != null)
					{
						Destroy(cycler);
					}
					
					//update position for dialog camera
					Vector3 newPosition = newButton.transform.localPosition;
					newPosition.x += DIALOG_CAMERA_OFFSET;
					newPosition.z = LOBBY_OPTION_Z_DEPTH;
					newButton.transform.localPosition = newPosition;
				}
			}
			return newButton;
		}

		private void showDailyBonusButtonOnTopOfDialog()
		{
			if (DailyBonusButton.instance != null)
			{
				GameObject newObject = CommonGameObject.instantiate(DailyBonusButton.instance.gameObject, this.transform, true) as GameObject;

				//don't let this script run or it will break the bottom bar
				DailyBonusButtonHIRV3 buttonScript = newObject.GetComponent<DailyBonusButtonHIRV3>();
				if (buttonScript != null)
				{
					// If we are ready to collect, and the object is not currently active,
					// then turn on collect now mode.
					buttonScript.readyInParent.SetActive(false);
					buttonScript.collectNowParent.SetActive(true);
					buttonScript.timerLabel.text = Localize.textUpper("collect_now");
					buttonScript.animator.Play(DailyBonusForceCollectionDialog.ON_ANIMATION);
					
					Destroy(buttonScript);
				}
				
				//set on click handler
				bonusCollectButton = newObject.GetComponent<ButtonHandler>();
				if (bonusCollectButton != null)
				{
					bonusCollectButton.registerEventDelegate(onBonusCollectClick);
				}

				// Position the daily bonus button over the lobby one behind it.
				Vector3 newPosition = newObject.transform.localPosition;
				newPosition.x += DIALOG_CAMERA_OFFSET;
				newPosition.z = LOBBY_OPTION_Z_DEPTH;
				newObject.transform.localPosition = newPosition;
			}
			else
			{
				Debug.LogWarningFormat("EueFtueRichDialog.cs -- init() -- could not find a daily bonus button in the lobby to match the position to. This dialog may look weird to the user.");
			}
		}

		private static void resizeClickHandlerToGameObject(GameObject targetObj, GameObject button, bool copyScale = false)
		{
			if (targetObj == null || button == null)
			{
				return;
			}
			
			//find halfway point
			BoxCollider targetCollider = targetObj.GetComponentInChildren<BoxCollider>();
			BoxCollider buttonCollider = button.GetComponent<BoxCollider>();
			
			
			if (targetCollider != null && targetCollider.gameObject != null && buttonCollider != null && buttonCollider.gameObject != null)
			{

				float originalZDepth = buttonCollider.transform.localPosition.z;
				CommonTransform.matchScreenPosition(buttonCollider.gameObject.transform, targetCollider.gameObject.transform);
				Vector3 newPosition = button.transform.localPosition;
				newPosition.z = originalZDepth;
				button.transform.localPosition = newPosition;

				if (copyScale)
				{
					button.transform.localScale = targetCollider.gameObject.transform.localScale;
				}
				
				buttonCollider.center = targetCollider.center;
				buttonCollider.size = targetCollider.size;
			}
		}

		private void Update()
		{
			switch (currentState)
			{
				case OverlayState.CHALLENGE_COMPLETE:
					if (!isClosing)
					{
						if (TouchInput.didTap && !isLoading)
						{
							PreferencesBase prefs = SlotsPlayer.getPreferences();
							prefs.SetBool(Prefs.FTUE_CHALLENGE_COMPLETE, true);
							prefs.Save();
							
							isClosing = true;
							Dialog.close(this);	
						}
					}
					break;
			}
		}

		private void initChallengeIntro()
		{
			//in case user clicks back button before this loads
			StatsManager.Instance.LogCount("game_actions", "machine_ftue", "", "", "first_spin", "view");
			StatsManager.Instance.LogMileStone("ftue", "machine_ftue");
			if (GameState.isMainLobby)
			{
				Dialog.close(this);
				return;
			}
			
			SafeSet.gameObjectActive(briefcase, false);
			SafeSet.gameObjectActive(finger, false);
			SafeSet.gameObjectActive(fullScreenButton.gameObject, true); //use this to block clicks

			StartCoroutine(cloneSpinButton());
			StartCoroutine(autoTimeout(onChallengeIntroClick));
			
			if (characterItem != null)
			{
				characterItem.setText(FTUE_CHALLENE_INTRO_TEXT);
				setCharacterController(challengeIntro);
				characterItem.animator.Play(CHARACTER_INTRO);
				Audio.play(RICH_APPEARS_GAME_SOUND);
				StartCoroutine(disableLoadFlagInSeconds(0.5f));
			}
			else
			{
				isLoading = false;
			}
		}

		private IEnumerator autoTimeout(ClickHandler.onClickDelegate onTimeoutFunc)
		{
			int startTime = GameTimer.currentTime;
			
			//if button isn't enabled and moved to a clickable location for over 10 seconds then enable full screen handler
			while ((GameTimer.currentTime - startTime) < CHALLENGE_INTRO_TIMEOUT)
			{
				yield return null;
			}

			//don't auto click because we timed out
			didTimeout = true;
			
			//call timeout function
			onTimeoutFunc(null);
		}
		

		private IEnumerator cloneSpinButton()
		{
			//we need to wait for the spin panel to be enabled before we clone it because the spin panel uses a weird way of disabling buttons
			if (SpinPanel.hir != null)
			{
				while (!SpinPanel.hir.isButtonsEnabled || SpinPanel.hir.spinButton.transform.localPosition.y < 0)
				{
					yield return null;
				}
			}

			//clone the spin button
			GameObject clonedSpinButton = showSpinButtonOnTopOfDialog();

			//add a click handler for this new cloned object
			clonedFullScreenButton = addClickHandlerForObject(clonedSpinButton, onChallengeIntroClick, -1);
		}

		private IEnumerator disableLoadFlagInSeconds(float time)
		{
			yield return new WaitForSeconds(time);
			isLoading = false;

		}

		private void initChallengeComplete()
		{
			//in case user clicks back button before this loads
			if (GameState.isMainLobby)
			{
				Dialog.close(this);
				return;
			}
			SafeSet.gameObjectActive(briefcase, false);
			SafeSet.gameObjectActive(finger, false);
			SafeSet.gameObjectActive(fullScreenButton.gameObject, false);
			if (characterItem != null)
			{
				characterItem.setText(FTUE_CHALLENGE_COMPLETE_TEXT);
				setCharacterController(challengeComplete);
				characterItem.animator.Play(CHARACTER_INTRO);
				Audio.play(RICH_APPEARS_GAME_SOUND);
				StartCoroutine(disableLoadFlagInSeconds(0.5f));
			}
			else
			{
				isLoading = false;
			}
			
		}
		
		public override void init()
		{
			isLoading = true;
			setupStateDictionary();
			currentState = (OverlayState)dialogArgs.getWithDefault(D.KEY, OverlayState.FIRST_LOGIN);
			
			//setup login button
			bool isForceDisabled = (bool)dialogArgs.getWithDefault(D.OPTION, false);
			bool isConnected = SlotsPlayer.isFacebookUser || SlotsPlayer.isLoggedIn;
			if (isForceDisabled || isConnected)
			{
				SafeSet.gameObjectActive(loginButton.gameObject, false);
			}
			else
			{
				loginButton.registerEventDelegate(onLoginClick);	
			}
			
			//run initialization
			stateInitializationFunctions[currentState]();
		}

		private void onLoginClick(Dict args)
		{
			loginButton.unregisterEventDelegate(onLoginClick);
			
			PreferencesBase prefs = SlotsPlayer.getPreferences();
			switch (currentState)
			{
				case OverlayState.FIRST_LOGIN:
					//save status
					prefs.SetBool(Prefs.FTUE_FIRST_LOGIN, true);
					//tell overlay to update if this is first login
					if (OverlayTopHIRv2.instance != null)
					{
						//add credits to top overlay
						OverlayTopHIRv2.instance.updateCredits(false);
					}
					break;
				
				case OverlayState.GAME_INTRO:
					prefs.SetBool(Prefs.FTUE_GAME_INTRO, true);
					break;
				
				case OverlayState.CHALLENGE_INTRO:
					prefs.SetBool(Prefs.FTUE_CHALLENGE_INTRO, true);
					break;
				
				case OverlayState.CHALLENGE_COMPLETE:
					prefs.SetBool(Prefs.FTUE_CHALLENGE_COMPLETE, true);
					break;
			}
			
			//also set abort key to prevent other ftue dialogs
			prefs.SetBool(Prefs.FTUE_ABORT, true);
			
			//save data
			prefs.Save();

			StatsManager.Instance.LogCount("lobby", "lobby_ftue", "exit_ftue", "alt_login", "", "click");
			
			Dialog.close();
			ZisSaveYourProgressDialog.showDialog();
			StatsZIS.logSettingsZis("zis_sign_in", "click", "ftue");
		}

		private void onBriefcaseCollect(Dict args)
		{
			StatsManager.Instance.LogCount("dialog", "lobby_ftue", "step", "", "welcome", "click");
			//remove delegate
			briefcaseCollectButton.unregisterEventDelegate(onBriefcaseCollect);
			
			//save flag that we've seen this overlay
			PreferencesBase prefs = SlotsPlayer.getPreferences();
			prefs.SetBool(Prefs.FTUE_FIRST_LOGIN, true);
			prefs.Save();

			//add credits to top overlay
			if (OverlayTopHIRv2.instance != null)
			{
				OverlayTopHIRv2.instance.updateCredits(true, true, 1.5f);
			}

			//show the briefcase animation then close the dialog
			StartCoroutine(playBriefcaseOutro());
		}

		private void onGameIntroLobbyOptionClick(Dict args)
		{
			if (clonedFullScreenButton != null)
			{
				clonedFullScreenButton.unregisterEventDelegate(onGameIntroLobbyOptionClick);
			}
			
			runCallbackRoutine = true;
			onGameIntroClick(args);
		}

		private void onGameIntroClick(Dict args)
		{
			StatsManager.Instance.LogCount("dialog", "lobby_ftue", "step", "", "select_a_game", "click");
			fullScreenButton.unregisterEventDelegate(onGameIntroClick);
			PreferencesBase prefs = SlotsPlayer.getPreferences();
			prefs.SetBool(Prefs.FTUE_GAME_INTRO, true);
			prefs.Save();
			
			StartCoroutine(playFingerOutro());
		}

		private void onBonusCollectClick(Dict args = null)
		{
			StatsManager.Instance.LogCount("dialog", "lobby_ftue", "step", "", "db_force_flow", "click");
			bonusCollectButton.unregisterEventDelegate(onBonusCollectClick);

			Audio.play(DAILY_BONUS_COLLECT_SOUND);
			if (EUEManager.canCollectDailyBonus)
			{
				string bonusString = ExperimentWrapper.NewDailyBonus.isInExperiment
					? ExperimentWrapper.NewDailyBonus.bonusKeyName
					: "bonus";
				// Tell the Scheduler to wait until we get the claim action back.

				Scheduler.Scheduler.addTask(new DailyBonusForceCollectionTask(), SchedulerPriority.PriorityType.BLOCKING);
				CreditAction.claimTimerCredits(-1, bonusString); // Payout number isnt read on the server.	
			}
			else
			{
				Debug.LogWarning("Cannot collect daily bonus");
			}
			
			Dialog.close(this);
		}

		private void onChallengeIntroClick(Dict args)
		{
			StopAllCoroutines();
			StatsManager.Instance.LogCount("game_actions", "machine_ftue", "", "", "first_spin", "click");
			runCallbackRoutine = true;
			fullScreenButton.unregisterEventDelegate(onChallengeIntroClick);
			PreferencesBase prefs = SlotsPlayer.getPreferences();
			prefs.SetBool(Prefs.FTUE_CHALLENGE_INTRO, true);
			prefs.Save();

			Dialog.close(this);
		}


		private void onChallengeCompleteClick(Dict args)
		{
			StatsManager.Instance.LogCount("game_actions", "machine_ftue", "", "", "level_up_5", "click");
			fullScreenButton.unregisterEventDelegate(onChallengeCompleteClick);
			PreferencesBase prefs = SlotsPlayer.getPreferences();
			prefs.SetBool(Prefs.FTUE_CHALLENGE_COMPLETE, true);
			prefs.Save();
			
			Dialog.close(this);
			
			//press the spin button
			if (SpinPanel.instance != null && SpinPanel.instance.multiClickHandler != null)
			{
				SpinPanel.instance.multiClickHandler.OnClick();
			}
			
			
		}
		private IEnumerator playBriefcaseOutro()
		{
			Audio.play(COLLECT_COINS_SOUND);
			yield return StartCoroutine(CommonAnimation.playAnimAndWait(briefcaseAnimator, BRIEFCASE_OUTRO));
			yield return StartCoroutine(playOutro(false));

			if (EUEManager.shouldDisplayBonusCollect && EUEManager.canCollectDailyBonus)
			{
				currentState = OverlayState.FORCED_BONUS;
				initBonusCollect();
			}
			else
			{
				Dialog.close(this);
			}
		}

		private IEnumerator playFingerOutro()
		{
			if (clonedLobbyOption != null)
			{
				SafeSet.gameObjectActive(clonedLobbyOption, false);
			}
			SafeSet.gameObjectActive(fingerAnimator.gameObject, false);
			yield return StartCoroutine(playOutro(true));
			
			
		}

		private IEnumerator playOutro(bool closeDialog)
		{
			yield return StartCoroutine(CommonAnimation.playAnimAndWait(characterItem.animator, CHARACTER_OUTRO));
			if (closeDialog)
			{
				Dialog.close(this);
			}
		}
		
		
		public override void close()
		{
			isLoading = false;
			
			StopAllCoroutines();
			
			//clear flag is user clicks too fast and closes dialog before intro anim is done
			EUEManager.clearPendingDialog(); 
			
			if (bonusCollectButton != null)
			{
				bonusCollectButton.unregisterEventDelegate(onBonusCollectClick);
			}

			if (clonedFullScreenButton != null)
			{
				clonedFullScreenButton.unregisterEventDelegate(onGameIntroLobbyOptionClick);
			}
			fullScreenButton.unregisterEventDelegate(onGameIntroClick);
			fullScreenButton.unregisterEventDelegate(onChallengeCompleteClick);
			fullScreenButton.unregisterEventDelegate(onChallengeIntroClick);
			briefcaseCollectButton.unregisterEventDelegate(onBriefcaseCollect);
			loginButton.unregisterEventDelegate(onLoginClick);
		}

		public static void Show(OverlayState displayState, SchedulerPriority.PriorityType priority = SchedulerPriority.PriorityType.HIGH)
		{
			if (!skipFTUE)
			{
				bool useShroud = displayState != OverlayState.CHALLENGE_INTRO &&
				                 displayState != OverlayState.CHALLENGE_COMPLETE;
				
				bool disableLoginButton = displayState == OverlayState.FORCED_BONUS ||
				                          displayState == OverlayState.CHALLENGE_INTRO ||
				                          displayState == OverlayState.CHALLENGE_COMPLETE;

				DialogBase.AnswerDelegate callback = new DialogBase.AnswerDelegate((args) =>
					{
						switch (displayState)
						{
							case OverlayState.GAME_INTRO:
								if (runCallbackRoutine)
								{
									runCallbackRoutine = false;
									RoutineRunner.instance.StartCoroutine(gameIntroCallbackRoutine());
								}
								break;
							case OverlayState.CHALLENGE_INTRO:
								if (runCallbackRoutine)
								{
									runCallbackRoutine = false;
									RoutineRunner.instance.StartCoroutine(challengeIntroCallbackRoutine());
								}
								break;
						}
					}
				);
				Scheduler.Scheduler.addDialog("eue_rich_overlay", Dict.create(D.KEY, displayState, D.SHROUD, useShroud, D.OPTION, disableLoginButton, D.CALLBACK, callback), priority);
			}
		}
		
		public static void resetStaticClassData()
		{
			isLoading = false;
			runCallbackRoutine = false;
			skipFTUE = false;
			didTimeout = false;
		}

		private static IEnumerator challengeIntroCallbackRoutine()
		{
			//wait for dialog to finish closing so we can open a new one
			yield return new TIWaitForSeconds(0.5f);
			
			//press the spin button after a half second
			if (SpinPanel.instance != null && SpinPanel.instance.multiClickHandler != null && !didTimeout)
			{
				StatsManager.Instance.LogCount("game_actions", "machine_ftue", "", "", "first_spin", "spin");
				SpinPanel.instance.multiClickHandler.OnClick();
			}
		}

		private static IEnumerator gameIntroCallbackRoutine()
		{
			//wait for dialog to finish closing so we can open a new one
			yield return new TIWaitForSeconds(0.5f);
			
			if (MainLobby.hirV3 != null)
			{
				LobbyOption option = MainLobby.hirV3.getFirstOption();
				if (option != null && option.button != null)
				{
					StatsManager.Instance.LogCount("lobby", "lobby_ftue", "select_game", option.name, "" , "click");
					option.click();
				}	
			}
		}

		private static IEnumerator runNextFtueStep()
		{
			//wait for dialog to finish closing so we can open a new one
			yield return new WaitForSeconds(0.5f);
			EUEManager.showLobbyEueFtue();
		}
	}	
}

