using UnityEngine;
using System.Collections;

using TMPro;
using Com.States;
using System.Collections.Generic;
using Com.Scheduler;

/// <summary>
///   This class displays the weekly race rewards if the user received any. It's typically a "chest" with
///   a fancy animation, then some coins show, and the user possibly gets a collectable pack
/// </summary>
public class WeeklyRaceRewards : DialogBase
{
	// =============================
	// PRIVATE
	// =============================
	[SerializeField] private GameObject chestAsset;

	[SerializeField] private Animator chestAnimator;
	[SerializeField] private Animator buttonAnimator;
	[SerializeField] private Animator coinAnimator;

	[SerializeField] private ButtonHandler confirmHandler;
	[SerializeField] private TextMeshPro buttonText;
	[SerializeField] private TextMeshPro chestText;
	[SerializeField] private TextMeshPro coinText;
	[SerializeField] private GameObject coinReward;
	[SerializeField] private GameObject coinTrail;

	// different chest rewards
	[SerializeField] private GameObject commonChest;
	[SerializeField] private GameObject bronzeChest;
	[SerializeField] private GameObject silverChest;
	[SerializeField] private GameObject goldChest;
	[SerializeField] private GameObject epicChest;

	// different logo rewards
	[SerializeField] private Renderer commonLogoImage;
	[SerializeField] private Renderer bronzeLogoImage;
	[SerializeField] private Renderer silverLogoImage;
	[SerializeField] private Renderer goldLogoImage;
	[SerializeField] private Renderer epicLogoImage;
	
	private GameObject targetChest;
	private Renderer targetLogo;
	private GameObject collectablesAsset;
	private StateMachine stateMachine;
	private long credits = 0;
	private string cardPackKey = "";
	private JSON cardPack = null;
	private int chestId = 0;
	private string eventId = "";
	private string packId = "";
	private string source = "";
	private JSON starPack = null;
	private JSON rewards = null;
	private List<CollectableCardData> collectedCards = null;

	// =============================
	// CONST
	// =============================
	public const string REWARD_SOURCE = "weeklyRaceRewardOutro";
	// chest animations
	private const string CHEST_INTRO 	= "Chest Award Intro";
	private const string CHEST_LOOP 	= "Chest Award Loop";
	private const string CHEST_OUTRO 	= "Chest Award Outro";
	private const string CHEST_OPEN		= "ani";
	// button animations
	private const string BUTTON_INTRO	= "Button Intro";
	private const string BUTTON_OUTRO	= "Button Outro";

	// coin award animations
	private const string COIN_INTRO		= "Coin Award Meter Intro";
	private const string COIN_OUTRO		= "Coin Award Meter Outro";

	private const string COLLECTIONS_ASSET = "Collections Pack Item ani";

	// delays
	private const float RANKS_DELAY		= 3f;
	private const float CHEST_DELAY		= 3f;

	private const string TEXTURE_BASE_PATH = "Features/Collections/Albums/{0}/Collection Textures/{1}";
	private const string LOGO_TEXTURE = "collection_logo";

	public override void init()
	{
		credits = (long)dialogArgs.getWithDefault(D.AMOUNT, 0L);
		cardPack = dialogArgs.getWithDefault(D.DATA, null) as JSON;
		if (cardPack != null)
		{
			JSON packDroppedEvents = cardPack.getJSON("pack_dropped_events");
			List<string> events = packDroppedEvents.getKeyList();
			string[] cards = null;
			for (int i = 0; i < events.Count; i++)
			{
				JSON pack = packDroppedEvents.getJSON(events[i]);
				cardPackKey = pack.getString("album", "");
				packId = pack.getString("pack", "");
				source = pack.getString("source", "");
				starPack = pack.getJSON("star_pack");
				rewards = pack.getJSON("rewards");
				cards = pack.getStringArray("cards");
				eventId = pack.getString("event", "");

				collectedCards = new List<CollectableCardData>();
				for (int j = 0; j < cards.Length; j++)
				{
					string cardKeyName = cards[j];
					CollectableCardData collectedCard = Collectables.Instance.findCard(cardKeyName);
					collectedCards.Add(collectedCard);
				}
			}
		}

		chestId = (int)dialogArgs.getWithDefault(D.INDEX, 1);

		stateMachine = new StateMachine("weekly_race_rewards");
		stateMachine.addState( "init", new StateOptions(null, null, setup) );
		stateMachine.addState( "chest", new StateOptions(null, null, playChests)  );
		stateMachine.addState( "chest_reveal", new StateOptions(null, null, playChestOpen)  );
		stateMachine.addState( "chest_reveal_reward", new StateOptions(null, null, revealRewards)  );
		stateMachine.addState( "chest_outro", new StateOptions(null, null, playChestsOutro)   );
		stateMachine.addState( State.COMPLETE );
		confirmHandler.registerEventDelegate(onConfirmClicked);

		coinText.text = CreditsEconomy.convertCredits(credits, true);

		Userflows.logStep("credits_weekly_race", credits.ToString());

		updateState("init");

		StatsWeeklyRace.logViewReward(chestId, "view");
	}

	private void onConfirmClicked(Dict args = null)
	{
		updateState(nextState);
	}

	private void updateState(string state)
	{
		stateMachine.updateState(state);
		
		if( stateMachine.can( State.COMPLETE ) )
		{
			coinTrail.SetActive(false);
			if (cardPack != null)
			{
				PackDroppedDialog.showDialog(collectedCards, cardPackKey, eventId, packId, source, starPack, rewards);
			}
			Dialog.close(this);
		}
	}

	private void bundleLoadFail(string assetPath, Dict data = null)
	{
		Debug.LogError("Failed to load set image at " + assetPath);
	}

	private void logoLoadedSuccess(string assetPath, Object obj, Dict data = null)
	{
		if (targetLogo != null)
		{
			Material material = new Material(targetLogo.material.shader);
			material.mainTexture = obj as Texture2D;
			targetLogo.material = material;
			targetLogo.gameObject.SetActive(true);
		}
	}

	/*=========================================================================================
	ANIMATION STATES
	=========================================================================================*/
	public void setup()
	{
		targetChest = null;
		
		switch( chestId )
		{
			case 2:
				targetChest = bronzeChest;
				targetLogo = bronzeLogoImage;
				chestText.text = "Congratulations! You won a BRONZE chest!";
				Audio.play("Dialog4Fanfare2WeeklyRace");
				break;

			case 3:
				targetChest = silverChest;
				targetLogo = silverLogoImage;
				chestText.text = "Congratulations! You won a SILVER chest!";
				Audio.play("Dialog4Fanfare3WeeklyRace");
				break;

			case 4:
				targetChest = goldChest;
				targetLogo = goldLogoImage;
				chestText.text = "Congratulations! You won a GOLD chest!";
				Audio.play("Dialog4Fanfare4WeeklyRace");
				break;

			case 5:
				targetChest = epicChest;
				targetLogo = epicLogoImage;
				chestText.text = "Congratulations! You won an EPIC chest!";
				Audio.play("Dialog4Fanfare5WeeklyRace");
				break;

			default:
				targetChest = commonChest;
				targetLogo = commonLogoImage;
				chestText.text = "Congratulations! You won a COMMON chest!";
				Audio.play("Dialog4Fanfare1WeeklyRace");
				break;
		}

		collectablesAsset = CommonGameObject.findChild(targetChest, COLLECTIONS_ASSET);

		chestAsset.SetActive(false);
		coinReward.SetActive(false);												   
		confirmHandler.gameObject.SetActive(false);

		disableAllChests();
		targetChest.SetActive(true);

		updateState(nextState);
		
		Audio.play("Dialog4Overlay1OpenWeeklyRace");
	}

	public void playChests()
	{
		chestAsset.SetActive(true);
		coinReward.SetActive(false);

		chestAnimator = chestAsset.GetComponent<Animator>();

		if (cardPack == null)
		{
			collectablesAsset.SetActive(false);
		} 
		else 
		{
			collectablesAsset.SetActive(true);
			SkuResources.loadFromMegaBundleWithCallbacks(CollectablePack.GENERIC_LOGO_PATH, logoLoadedSuccess, bundleLoadFail);
		}

		confirmHandler.gameObject.SetActive(true);
		buttonText.text = "Open Chest";
		buttonAnimator.Play(BUTTON_INTRO);

		chestAnimator.Play(CHEST_INTRO);
	}

	public void playChestOpen()
	{
		Animator targetChestAnimator = targetChest.GetComponent<Animator>();
		targetChestAnimator.Play(CHEST_OPEN);
		confirmHandler.gameObject.SetActive(false);
		StartCoroutine(waitSequence(1f));
		Audio.play("Dialog4Anim1WeeklyRace");
	}

	public void revealRewards()
	{
		coinReward.SetActive(true);
		coinAnimator.Play(COIN_INTRO);
		confirmHandler.gameObject.SetActive(true);
		buttonAnimator.Play(BUTTON_INTRO);
		buttonText.text = "Collect";
		Audio.play("Dialog4Anim2WeeklyRace");
	}

	public void playChestsOutro()
	{
		chestAsset.SetActive(true);
		buttonAnimator.Play(BUTTON_OUTRO);
		coinAnimator.Play(COIN_OUTRO);
		chestAnimator.Play(CHEST_OUTRO);
		StartCoroutine(waitSequence(CHEST_DELAY));

		SlotsPlayer.addFeatureCredits(credits, REWARD_SOURCE);

		Audio.play("CountCoinsWeeklyRace");
		coinTrail.SetActive(true);

		StatsWeeklyRace.logCollectReward(chestId, (int)credits, "click");
	}

	private IEnumerator waitSequence(float delay = 3.0f)
	{
		yield return new WaitForSeconds(delay);

		updateState(nextState);

		yield break;
	}

	// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup. Downloaded textures are automatically destroyed by Dialog class.
	}

	private void disableAllChests()
	{
		commonChest.SetActive(false);
		bronzeChest.SetActive(false);
		silverChest.SetActive(false);
		goldChest.SetActive(false);
		epicChest.SetActive(false);
	}

	/*=========================================================================================
	GETTERS
	=========================================================================================*/
	private string nextState
	{
		get
		{
			switch(stateMachine.currentState)
			{
				case "init":
					return "chest";

				case "chest":
					return "chest_reveal";

				case "chest_reveal":
					return "chest_reveal_reward";

				case "chest_reveal_reward":
					return "chest_outro";

				default:
					return State.COMPLETE;
			}
		}		
	}

	public static void showDialog(Dict args = null, SchedulerPriority.PriorityType priority = SchedulerPriority.PriorityType.LOW)
	{
		SchedulerPackage raceResultsPackage =
			WeeklyRaceResults.package != null && WeeklyRaceResults.package.contains(WeeklyRaceResults.package.findTaskWith("weekly_race_rewards"))
				? WeeklyRaceResults.package
				: null;
		Scheduler.addDialog
		(
			"weekly_race_rewards",
			args,
			SchedulerPriority.PriorityType.MEDIUM,
			raceResultsPackage
		);
	}
}
