using UnityEngine;
using System.Collections;
using Com.Scheduler;

/*
Controls the display, animation and dismissal of the hyper economy intro stuff that points at the buy coins button.
*/

public class HyperEconomyIntroBuy : MonoBehaviour
{
	private const float LIGHT_TIME = 0.3f;
	
	public Transform arrowParent;
	public GameObject textThrobber;
	public GameObject[] lights;
	public GameObject[] textSprites;
	public GameObject[] messages;
	public GameObject[] sparkles;
	
	private int page = 0;						// The current page being viewed.
	private int lightSwitch = 0;				// The current set of lights that are turned on.
	private GameTimer lightSwitchTimer = null;
	
	public static bool shouldShow
	{
		get
		{
			return
				ExperimentWrapper.HyperEconomy.isIntroEnabled &&
				!CustomPlayerData.getBool(CustomPlayerData.ECONOMY_MIGRATION_BUY_SEEN, false);
		}
	}
	
	void Awake()
	{
		// Make sure the first page stuff is shown and the second hidden.
		textSprites[0].SetActive(true);
		messages[0].SetActive(true);

		textSprites[1].SetActive(false);
		messages[1].SetActive(false);
		
		lightSwitchTimer = new GameTimer(LIGHT_TIME);

		// Make the sparkles hidden by default.
		for (int i = 0; i < sparkles.Length; i++)
		{
			sparkles[i].transform.localScale = Vector3.zero;
		}
		
		StatsManager.Instance.LogCount(
			counterName:"dialog",
			kingdom:	"new_economy_intro",
			phylum:		"lobby_overlay_1",
			genus:		"view"
		);

		playSound();
	}
	
	void Update()
	{
		if (lightSwitchTimer.isExpired)
		{
			// Alternate the lights.
			lights[lightSwitch].SetActive(false);
			lightSwitch = (lightSwitch + 1) % 2;
			lights[lightSwitch].SetActive(true);
			lightSwitchTimer.startTimer(LIGHT_TIME);
		}
		
		// Make the arrow float up and down a little.
		CommonTransform.setY(arrowParent, CommonEffects.pulsateBetween(0.0f, 25.0f, 5.0f));
	}
	
	// NGUI button callback
	void screenTouched()
	{
		if (page == textSprites.Length - 1)
		{
			// Set this when closing the screen, to make sure MOTD's don't get shown if this is showing.
			CustomPlayerData.setValue(CustomPlayerData.ECONOMY_MIGRATION_BUY_SEEN, true);

			StatsManager.Instance.LogCount(
				counterName:"dialog",
				kingdom:	"new_economy_intro",
				phylum:		"lobby_overlay_2",
				family:		"close",
				genus:		"click"
			);

			// Touched the last page. Close the whole thing.
			Destroy(gameObject);

			// Allow dialogs to be shown now.
			Scheduler.run();
		}
		else
		{
			setPage(page + 1);
			Audio.play("EconomyMigration2");	// Always play the second sound here, since it's the second page.

			StatsManager.Instance.LogCount(
				counterName:"dialog",
				kingdom:	"new_economy_intro",
				phylum:		"lobby_overlay_1",
				family:		"close",
				genus:		"click"
			);

			StatsManager.Instance.LogCount(
				counterName:"dialog",
				kingdom:	"new_economy_intro",
				phylum:		"lobby_overlay_2",
				genus:		"view"
			);
		}
	}
	
	private void setPage(int newPage)
	{
		textSprites[page].SetActive(false);
		messages[page].SetActive(false);

		textSprites[newPage].SetActive(true);
		messages[newPage].SetActive(true);
		
		// Throb the text sprite being shown, just once.
		StartCoroutine(CommonEffects.throb(textThrobber, Vector3.one * 1.25f, 0.5f));
		
		for (int i = 0; i < sparkles.Length; i++)
		{
			StartCoroutine(CommonEffects.throb(sparkles[i], Vector3.one * 0.5f, 0.5f));
		}
		
		page = newPage;
	}
	
	// Plays the first sound if it's the first hyper economy screen seen,
	// or the second sound if it's any of them have been seen.
	public static void playSound()
	{
		if (!CustomPlayerData.getBool(CustomPlayerData.ECONOMY_MIGRATION_BUY_SEEN, false) &&
			!CustomPlayerData.getBool(CustomPlayerData.ECONOMY_MIGRATION_BET_SEEN, false)
			)
		{
			Audio.play("EconomyMigration1");
		}
		else
		{
			Audio.play("EconomyMigration2");
		}
	}
}
