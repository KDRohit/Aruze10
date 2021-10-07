using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

namespace Com.HitItRich.Feature.VirtualPets
{
	public class VirtualPetBottomButton : BottomOverlayButton
	{
		private const string IN_PROGRESS_STATE = "inprogress";
		private const string COMPLETE_STATE = "complete";

		[SerializeField] private List<ObjectSwapper> treatIcons;
		[SerializeField] private GameObject treatIconsParent;
		[SerializeField] private GameObject hyperMeterParent;
		[SerializeField] private GameObject completedStateParent;
		[SerializeField] private GameObject inProgressStateParent;
		[SerializeField] private LabelWrapperComponent meterLabel;
		[SerializeField] private AnimationListController.AnimationInformationList lockedAnimation;
		[SerializeField] private AnimationListController.AnimationInformationList defaultAnimation;
		[SerializeField] private AnimationListController.AnimationInformationList hyperToDefaultAnimation;
		[SerializeField] private AnimationListController.AnimationInformationList hyperAnimation;
		[SerializeField] private AnimationListController.AnimationInformationList meterAnimation;
		[SerializeField] private VirtualPetEnergyMeter energyMeter;

		[SerializeField] private List<AnimationListController.AnimationInformationList> treatIdleAnimations;
		[SerializeField] private List<AnimationListController.AnimationInformationList> treatCheckmarkAnimations;

		private bool hyperTimerDisplayed;
		private bool energyMeterDisplayed;
		private List<bool> treatIsShowingComplete;
		
		protected override void Awake()
		{
			base.Awake();
			init();
			sortIndex = 6;
		}

		protected override void getUnlockData()
		{
			//override unlock type with pet ftue
			unlockData = new FeatureUnlockData("virtual_pet", "pet_ftue");
		}

		protected void markFeatureSeen()
		{
			hasViewedFeature = true;
			toolTipController.toggleNewBadge(false);
			unlockData.featureSeen = true;
			CustomPlayerData.setValue("virtual_pet_feature_seen", true);
		}

		private void turnOffPetIcons()
		{
			if (treatIcons == null)
			{
				return;
			}

			int animatorCount = treatIdleAnimations != null ? treatIdleAnimations.Count : 0;
			for (int i = 0; i < treatIcons.Count; i++)
			{
				if (treatIcons[i] == null )
				{
					continue;
				}

				if (i < animatorCount && treatIdleAnimations[i] != null)
				{
					StartCoroutine(AnimationListController.playListOfAnimationInformation(treatIdleAnimations[i]));
				}
				else
				{
					treatIcons[i].setState(IN_PROGRESS_STATE);	
				}
			}
		}

		protected override void init()
		{
			base.init();
			hyperTimerDisplayed = false;
			energyMeterDisplayed = false;
			energyMeter.init(VirtualPetsFeature.instance.currentEnergy, VirtualPetsFeature.instance.hyperEndTime);
			treatIsShowingComplete = new List<bool>(treatIcons == null ? 1 : treatIcons.Count);
			for (int i = 0; i < treatIcons.Count; i++)
			{
				treatIsShowingComplete.Add(false);
			}
			
			if (VirtualPetsFeature.instance == null || !VirtualPetsFeature.instance.ftueSeen)
			{
				initLevelLock(false);
				toolTipController.setLockedText(BottomOverlayButtonToolTipController.SPIN_TO_UNLOCK);
				turnOffPetIcons();
				if (lockedAnimation != null)
				{
					StartCoroutine(AnimationListController.playListOfAnimationInformation(lockedAnimation));
				}
			}
			else
			{
				VirtualPetsFeature.instance.registerForHyperStatusChange(onHyperStatusChange);
				
				
				if (defaultAnimation != null)
				{
					StartCoroutine(AnimationListController.playListOfAnimationInformation(defaultAnimation));
				}
				
				refreshDisplay(VirtualPetsFeature.instance.allTaskComplete, VirtualPetsFeature.instance.isHyper);
				
				if (needsToShowUnlockAnimation())
				{
					showUnlockAnimation();
				}
				else
				{
					toolTipController.toggleNewBadge(!hasViewedFeature);
				}

				refreshDisplay(VirtualPetsFeature.instance.allTaskComplete, VirtualPetsFeature.instance.isHyper);
				refreshTreatIcons();
				VirtualPetsFeature.instance.registerForStatusUpdate(onPetStatusUpdate);
			}
		}

		private void onPetStatusUpdate()
		{
			refreshDisplay(VirtualPetsFeature.instance.allTaskComplete, VirtualPetsFeature.instance.isHyper);
			refreshTreatIcons();
			StartCoroutine(energyMeter.updateEnergy(VirtualPetsFeature.instance.currentEnergy, VirtualPetsFeature.instance.hyperEndTime));
		}
		
		protected override void showLoadingTooltip(string dialogKey)
		{ 
			Com.Scheduler.Scheduler.addTask(new BundleLoadingTask(Dict.create(D.OBJECT, toolTipController, D.KEY, dialogKey)), Com.Scheduler.SchedulerPriority.PriorityType.BLOCKING);
		}

		protected override void onClick(Dict args = null)
		{
			if (Scheduler.Scheduler.hasTaskWith(VirtualPetsFeature.DIALOG_KEY))
			{
				return;
			}

			bool isLocked = VirtualPetsFeature.instance == null || !VirtualPetsFeature.instance.ftueSeen;
			if (isLocked)
			{
				StartCoroutine(toolTipController.playLockedTooltip());
				StatsManager.Instance.LogCount(
					counterName: "bottom_nav",
					kingdom: "pet",
					phylum: "icon",
					family: "locked",
					genus: "click"
				);
			}
			else
			{
				StatsManager.Instance.LogCount(
					counterName: "bottom_nav",
					kingdom: "pet",
					phylum: "icon",
					family: CommonText.formatNumber(VirtualPetsFeature.instance.getNumCompletedTasks()),
					milestone: VirtualPetsFeature.instance.isHyper ? "hyper_on" : "hyper_off",
					val: VirtualPetsFeature.instance.currentEnergy,
					genus: "click"
				);
				
				if (!hasViewedFeature)
                {
                	markFeatureSeen();
                }

                showLoadingTooltip(VirtualPetsFeature.DIALOG_KEY);
                VirtualPetsFeatureDialog.showDialog();
			}
		}

		private IEnumerator showMeterAndUpdateEnergy()
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(meterAnimation));
			StartCoroutine(energyMeter.updateEnergy(VirtualPetsFeature.instance.currentEnergy, VirtualPetsFeature.instance.hyperEndTime));
		}

		private void refreshDisplay(bool hasCompletedAllTasks, bool isHyper)
		{
			bool energyMeterDisplayStateChanged = false;
			if (hasCompletedAllTasks && !isHyper && !energyMeterDisplayed)
			{
				energyMeterDisplayStateChanged = true;
				energyMeterDisplayed = true;
				StartCoroutine(showMeterAndUpdateEnergy());
			}
			else if (energyMeterDisplayed && (isHyper || !hasCompletedAllTasks))
			{
				energyMeterDisplayStateChanged = true;
				energyMeterDisplayed = false;
				if (isHyper)
				{
					hyperTimerDisplayed = true;
					StartCoroutine(AnimationListController.playListOfAnimationInformation(hyperAnimation));
				}
				else
				{
					hyperTimerDisplayed = false;
					StartCoroutine(AnimationListController.playListOfAnimationInformation(defaultAnimation));
				}
			}
			else if (hyperTimerDisplayed && !isHyper)
			{
				hyperTimerDisplayed = false;
				RoutineRunner.instance.StartCoroutine(hyperToDefault());
			}
			else if (!hyperTimerDisplayed && isHyper)
			{
				hyperTimerDisplayed = true;
				StartCoroutine(AnimationListController.playListOfAnimationInformation(hyperAnimation));
			}

			if (energyMeterDisplayed && !energyMeterDisplayStateChanged)
			{
				StartCoroutine(energyMeter.updateEnergy(VirtualPetsFeature.instance.currentEnergy, VirtualPetsFeature.instance.hyperEndTime));
			}
		}

		private IEnumerator hyperToDefault()
		{
			//play the hyper to default animation
			yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(hyperToDefaultAnimation));

			//animation resets the status of all icons.  We have to update our data model to match
			for (int i = 0; i < treatIsShowingComplete.Count; i++)
			{
				treatIsShowingComplete[i] = false;
			}
			//call refresh to set correct status
			refreshTreatIcons();
		}

		private void refreshTreatIcons()
		{
			if (treatIcons == null)
			{
				return;
			}

			bool animationsExist = treatCheckmarkAnimations != null &&
			                       treatIdleAnimations != null &&
			                       treatCheckmarkAnimations.Count == treatIdleAnimations.Count &&
			                       treatIcons.Count == treatCheckmarkAnimations.Count;
			for (int i = 0; i < treatIcons.Count; ++i)
			{
				if (treatIcons[i] == null)
				{
					continue;
				}

				if (VirtualPetsFeature.instance.treatTasks == null ||
				    VirtualPetsFeature.instance.treatTasks.Count <= i)
				{
					break;
				}

				string taskId = VirtualPetsFeature.instance.treatTasks[i];
				CampaignDirector.FeatureTask featTask = CampaignDirector.getTask(taskId);
				if (animationsExist && featTask != null)
				{
					if (featTask.isComplete && !treatIsShowingComplete[i])
					{
						treatIsShowingComplete[i] = true;
						StartCoroutine(AnimationListController.playListOfAnimationInformation(treatCheckmarkAnimations[i]));
					}
					else if (!featTask.isComplete && treatIsShowingComplete[i])
					{
						treatIsShowingComplete[i] = false;
						StartCoroutine(AnimationListController.playListOfAnimationInformation(treatIdleAnimations[i]));
					}
				}
				else
				{
					treatIcons[i].setState(featTask != null && featTask.isComplete
                    					? COMPLETE_STATE
                    					: IN_PROGRESS_STATE);
				}
				
				
			}
		}

		private void onHyperStatusChange(bool isHyper)
		{
			refreshDisplay(VirtualPetsFeature.instance.allTaskComplete, isHyper);
		}

		
		private void refreshHyperMeter(bool isHyper)
		{
			if (hyperTimerDisplayed && !isHyper)
			{
				hyperTimerDisplayed = false;
				if (hyperToDefaultAnimation != null)
				{
					StartCoroutine(AnimationListController.playListOfAnimationInformation(hyperToDefaultAnimation));
				}
				else
				{
					SafeSet.gameObjectActive(hyperMeterParent, isHyper);
					SafeSet.gameObjectActive(treatIconsParent, !isHyper);
				}
			}
			else if (!hyperTimerDisplayed && isHyper)
			{
				hyperTimerDisplayed = true;
				if (hyperAnimation != null)
				{
					StartCoroutine(AnimationListController.playListOfAnimationInformation(hyperAnimation));
				}
				else
				{
					SafeSet.gameObjectActive(hyperMeterParent, isHyper);
					SafeSet.gameObjectActive(treatIconsParent, !isHyper);
				}
			}
			
			if (isHyper)
			{
				int secondsRemaining = VirtualPetsFeature.instance.hyperEndTime - GameTimer.currentTime;
				meterLabel.text = "Playtime! " + CommonText.secondsFormatted(secondsRemaining);	
			}
		}

		private void Update()
		{
			if (VirtualPetsFeature.instance == null || !VirtualPetsFeature.instance.isEnabled)
			{
				return;
			}

			if (VirtualPetsFeature.instance.isHyper)
			{
				refreshHyperMeter(true);
			}
		}

		private void OnDestroy()
		{
			if (VirtualPetsFeature.instance != null)
			{
				VirtualPetsFeature.instance.deregisterForStatusUpdate(onPetStatusUpdate);	
				VirtualPetsFeature.instance.deregisterForHyperStatusChange(onHyperStatusChange);
			}
		}
	}
}
