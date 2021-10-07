using System.Collections;
using System.Collections.Generic;
using Com.HitItRich.Feature.VirtualPets;
using Com.Scheduler;
using UnityEngine;

public class VirtalPetsFeedDialog : DialogBase
{
	private class TreatMap
	{
		public TreatMap(int index, string name, string introKey, string outroKey)
		{
			treatIndex = index;
			treatName = name;
			treatIntroTextKey = introKey;
			treatOutroTextKey = outroKey;

		}
		public int treatIndex;
		public string treatName { get; private set; }
		public string treatIntroTextKey { get; private set; }
		public string treatOutroTextKey { get; private set; }
	}

	public class TreatData
	{
		public TreatData(string id, long beforeTreatEnergy, long afterTreatEnergy, int playtimeEndBeforeTreat, int playtimeEndAfterTreat, long creditsAwarded)
		{
			treadId = id;
			previousEnergy = beforeTreatEnergy;
			currentEnergy = afterTreatEnergy;
			previousPlaytimeEnd = playtimeEndBeforeTreat;
			currentPlaytimeEnd = playtimeEndAfterTreat;
			treatAward = creditsAwarded;
		}

		public void awardCredits()
		{
			if (treatAward > 0)
			{
				SlotsPlayer.addFeatureCredits(treatAward, VirtualPetsFeature.PET_FEED_PENDING_CREDIT_SOURCE);
				treatAward = 0; //Prevent double paying this out if they skip the animations and we fast-forward through the presentation
			}
		}
		public bool treatFed = false;
		public string treadId { get; private set; }
		public long previousEnergy { get; private set; }
		public long currentEnergy { get; private set; }
		public int currentPlaytimeEnd { get; private set; }
		public int previousPlaytimeEnd { get; private set; }

		public long treatEnergy
		{
			get
			{
				return currentEnergy - previousEnergy;
			}
		}

		public int deltaPlaytime
		{
			get
			{
				if (currentPlaytimeEnd <= GameTimer.currentTime)
				{
					return 0;
				}
				
				if (previousPlaytimeEnd <= GameTimer.currentTime)
				{
					return currentPlaytimeEnd - GameTimer.currentTime;
				}
				else
				{
					return currentPlaytimeEnd - previousPlaytimeEnd;
				}
			}
		}
		public long treatAward { get; private set; }

	}
	
	public const string dialogName = "virtual_pets_feed_dialog";
	[SerializeField] private VirtualPet dog;
	[SerializeField] private ClickHandler fullScreenButton;
	private List<TreatData> treatData; // convet to set
	private CampaignDirector.FeatureTask [] treatTaskList;
	
	private List<TreatData>treatNewTreatsToFeed = new List<TreatData>(); //convert to set to speed up searching

	private readonly Dictionary<string, TreatMap> statsTaskLookUp = new Dictionary<string, TreatMap>()
	{
		{ "login", new TreatMap(0,"login_treat","vp_treat_login_intro_text","vp_treat_login_outro_text") },
		{ "spin", new TreatMap(1,"spin_treat","vp_treat_spin_intro_text","vp_treat_spin_outro_text") },
		{"rp_periodic", new TreatMap(2,"richpass_treat","vp_treat_rich_pass_intro_text", "vp_treat_rich_pass_outro_text") },
		{ "special_treat", new TreatMap(3, "special_treat","vp_treat_special_intro_text","vp_treat_special_outro_text") }
	};
		
	[SerializeField] private AnimationListController.AnimationInformationList introList;
	[SerializeField] private AnimationListController.AnimationInformationList outroList;

	[SerializeField] private VirtalPetFeedTreatAnimation[] treats;
	[SerializeField] private MultiLabelWrapperComponent treatTextField;    
	[SerializeField] private VirtualPetEnergyMeter energyMeter;

	private long displayedEnergyAmount = 0;
	private int displayedPlaytimeEnd = 0;
	private List<TICoroutine> runningCoroutines = null;
	private TICoroutine initRoutine = null;

	private int displayedCollectedTaskTreats = 0;

	private const string SPECIAL_TREAT_KEY = "special_treat";

	public override void init()
	{
		SafeSet.labelText(treatTextField.labelWrapper, "");
		treatData = dialogArgs.getWithDefault(D.OPTION, new List<TreatData>()) as List<TreatData>;
		treatTaskList = dialogArgs.getWithDefault(D.OPTION1, new CampaignDirector.FeatureTask[0]) as CampaignDirector.FeatureTask[];

		displayedEnergyAmount = VirtualPetsFeature.instance.currentEnergy;
		displayedPlaytimeEnd = VirtualPetsFeature.instance.hyperEndTime;
		
		for (int i = 0; i < treatData.Count; i++)
		{
			displayedEnergyAmount -= treatData[i].treatEnergy;
			int deltaPlaytime = treatData[i].deltaPlaytime;
			if (deltaPlaytime > 0)
			{
				displayedPlaytimeEnd -= deltaPlaytime;
			}
		}

		for (int index = 0; index < treatTaskList.Length; ++index)
		{
			if (statsTaskLookUp.ContainsKey(treatTaskList[index].type))
			{
				statsTaskLookUp[treatTaskList[index].type].treatIndex = index;
			}
		}

		if (fullScreenButton != null)
		{
			fullScreenButton.registerEventDelegate(onSkipClicked);
			SafeSet.gameObjectActive(fullScreenButton.gameObject, true);    
		}
		
		energyMeter.init(displayedEnergyAmount, displayedPlaytimeEnd);
		initRoutine = StartCoroutine(introAnimation());
	  
	}
	private IEnumerator introAnimation()
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(introList));
		yield return StartCoroutine(setTreatesStatus());
		if (displayedCollectedTaskTreats == 0)
		{
			yield return StartCoroutine(dog.playFirstTreatIntro()); //Special intro for the first treat of the day
		}
		else
		{
			yield return StartCoroutine(dog.playIdleAnimations());
		}

		yield return StartCoroutine(processFeedingPet());
	}

	private void onSkipClicked(Dict args = null)
	{
		fullScreenButton.unregisterEventDelegate(onSkipClicked);

		if (initRoutine != null)
		{
			StopCoroutine(initRoutine);
		}
		
		//stop playing animation coroutines
		if (runningCoroutines != null)
		{
			for (int i = 0; i < runningCoroutines.Count; i++)
			{
				StopCoroutine(runningCoroutines[i]);
			}
		}
		

		StatsManager.Instance.LogCount("popup", "pet", "got_treat", "", "", "skip",
			VirtualPetsFeature.instance.currentEnergy,
			VirtualPetsFeature.instance.isHyper ? "hyper_on" : "hyper_off");

		//play outro
		StartCoroutine(skipOutro());
	}

	private IEnumerator skipOutro()
	{
		//play outro
		long preDisplayEnergy = displayedEnergyAmount;
		long preDisplayPlaytimeEnd = displayedPlaytimeEnd;
		foreach (TreatData treat in treatData)
		{
			if (!treat.treatFed)
			{
				displayedEnergyAmount += treat.treatEnergy;
				displayedPlaytimeEnd += treat.deltaPlaytime;
				treat.awardCredits();
			}
		  
		}

		yield return StartCoroutine(energyMeter.updateEnergy(displayedEnergyAmount, displayedPlaytimeEnd));
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(outroList));
		Dialog.close(this);
	}
	
	private IEnumerator setTreatesStatus()
	{
		runningCoroutines = new List<TICoroutine>();
		for(int index = 0; index < treatTaskList.Length; ++index)
		{
			runningCoroutines.Add(StartCoroutine(setTreatStatus(index)));
		}
		yield return StartCoroutine(Common.waitForCoroutinesToEnd(runningCoroutines));
	}

	private IEnumerator setTreatStatus(int index)
	{
		CampaignDirector.FeatureTask currentTask = treatTaskList[index];
		bool isbeingFed = isTaskNameGoingToBeFed(currentTask.type);
		if (!currentTask.isComplete)
		{
			yield return StartCoroutine(treats[index].treatNotAvailableAnimation());
		}
		else if (isbeingFed)
		{
			yield return StartCoroutine(treats[index].treatAvailableAnimation());
		}
		else
		{
			displayedCollectedTaskTreats++;
			yield return StartCoroutine(treats[index].treatUsedAnimation());
		}
	}

	private IEnumerator processFeedingPet()
	{
		yield return StartCoroutine(feedTreats(treatData));
		yield return StartCoroutine(feedTreats(treatNewTreatsToFeed));
		yield return StartCoroutine(outroAnimation());
	}
   

	private IEnumerator feedTreats( List<TreatData> treatsToFeed)
	{
		foreach (TreatData treatName in treatsToFeed)
		{
			VirtalPetFeedTreatAnimation treatAnimation = getFeedItem(treatName.treadId);
			treatAnimation.textIntroLocKey = statsTaskLookUp[treatName.treadId].treatIntroTextKey;
			treatAnimation.textOutroLocKey = findOutroText(statsTaskLookUp[treatName.treadId].treatIndex);
			yield return StartCoroutine(treatIntroAnimation(treatAnimation));
			treatName.awardCredits();
			yield return StartCoroutine(playFeedAnimation(treatName.treadId, treatName.treatEnergy, treatName.deltaPlaytime));
			treatName.treatFed = true;
			yield return StartCoroutine(treatOutroAnimation(treatAnimation));
		}
	}

	private string findOutroText(int startingIndex = 0)
	{
		string outroTextKey = "vp_treat_all_done";
		if (startingIndex + 1 >= treatTaskList.Length)
		{
			startingIndex = 0;
		}
		for (int index = startingIndex; index < treatTaskList.Length; ++index)
		{
			CampaignDirector.FeatureTask currentTask = treatTaskList[index];
			if (!currentTask.isComplete)
			{
				outroTextKey =  statsTaskLookUp[currentTask.type].treatOutroTextKey;
				break;
			}
		}
		
		return outroTextKey;
	}

	public override void close()
	{
	}

	private bool isTaskNameGoingToBeFed(string task)
	{
		//TODO change these list to Sets to speed up search 
		foreach (TreatData treat in treatData)
		{
			if (treat.treadId == task)
			{
				return true;
			}
		}
		foreach (TreatData treatNew in treatNewTreatsToFeed)
		{
			if (treatNew.treadId== task)
			{
				return true;
			}
		}

		return false;
	}
	private VirtalPetFeedTreatAnimation getFeedItem(string treatName)
	{
		int index = -1;
		if (statsTaskLookUp.ContainsKey(treatName))
		{
			index = statsTaskLookUp[treatName].treatIndex;
		}

		if (index != -1 && index < treats.Length)
		{
			return  treats[index]; 
		}
		else
		{
			return null;
		}
	}
	private IEnumerator treatIntroAnimation(VirtalPetFeedTreatAnimation treatAnimation)
	{
		if (treatAnimation != null)
			yield return StartCoroutine(treatAnimation.treatIntroAnimation());
	}
	private IEnumerator treatOutroAnimation(VirtalPetFeedTreatAnimation treatAnimation)
	{
		if (treatAnimation != null)
			yield return StartCoroutine( treatAnimation.treatOutroAnimation());
	}
	
	private IEnumerator outroAnimation()
	{
		//turn off skip handler;
		fullScreenButton.unregisterEventDelegate(onSkipClicked);
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(outroList));
		Dialog.close(this);
	}
	private IEnumerator playFeedAnimation(string treatName, long energyGainedAmount,  int playtimeGainedAmount)
	{
		string treatStatName = "";
		if (statsTaskLookUp.ContainsKey(treatName))
		{
			treatStatName = statsTaskLookUp[treatName].treatName;
		}
		
		displayedEnergyAmount += energyGainedAmount;
		displayedPlaytimeEnd += playtimeGainedAmount;
		StartCoroutine(energyMeter.updateEnergy(displayedEnergyAmount, displayedPlaytimeEnd));

		StatsManager.Instance.LogCount("popup", "pet", "got_treat", treatStatName, "", "view",
			VirtualPetsFeature.instance.currentEnergy,
			VirtualPetsFeature.instance.isHyper ? "hyper_on" : "hyper_off");
		yield return StartCoroutine(dog.playFeedAnimation());

		if (treatName == SPECIAL_TREAT_KEY)
		{
			yield return StartCoroutine(dog.playSpecialTreatReaction());
		}
		else
		{
			//Update the number of treats we're showing as collected and play a random dog reaction animation
			displayedCollectedTaskTreats++;
			if (displayedCollectedTaskTreats == treatTaskList.Length)
			{
				yield return StartCoroutine(dog.playRandomFinalTreatReaction());
			}
			else
			{
				yield return StartCoroutine(dog.playRandomNonFinalTreatReaction());
			}
		}
	}

	public static void showDialog(Dict args = null, SchedulerPriority.PriorityType priority = SchedulerPriority.PriorityType.HIGH)
	{
		if (args == null)
		{
			args = Dict.create();
		}

		args.Add(D.SHROUD, false);

		if (RichPassInGameCounter.instance != null)
		{
			RoutineRunner.instance.StartCoroutine(showFeedDialogWhenRichPassIndicatorFinishes(args, priority));
		}
		else
		{
			Scheduler.addDialog("virtual_pets_feed_dialog", args, priority);	
		}
		
	}

	private static IEnumerator showFeedDialogWhenRichPassIndicatorFinishes(Dict args, SchedulerPriority.PriorityType priority)
	{
		//wait one frame in ca
		//se this event comes down at the same time as the rich pass challenge complete
		yield return new TIWaitForEndOfFrame();  
		yield return null;
		
		while (RichPassInGameCounter.instance != null && RichPassInGameCounter.instance.isPlayingPresentation)
		{
			yield return null;
		}
		Scheduler.addDialog("virtual_pets_feed_dialog", args, priority);	
		
	}
}
