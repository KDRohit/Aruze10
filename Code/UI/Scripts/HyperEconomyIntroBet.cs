using UnityEngine;
using System.Collections;
using Com.Scheduler;

/*
Controls the display, animation and dismissal of the hyper economy intro stuff that points at the buy coins button.
*/

public class HyperEconomyIntroBet : MonoBehaviour
{
	private const float LIGHT_TIME = 0.3f;
	
	public Transform arrowParent;
	public Vector2 arrowFloatDestination;	// Floating animation destination, between the starting position and this.
	public GameObject[] lights;
	public string trackingPhylum = "";
	
	private float startX = 0.0f;
	private float startY = 0.0f;
	private int lightSwitch = 0;				// The current set of lights that are turned on.
	private GameTimer lightSwitchTimer = null;
	
	public static bool shouldShow
	{
		get
		{
			return
				ExperimentWrapper.HyperEconomy.isIntroEnabled &&
				!CustomPlayerData.getBool(CustomPlayerData.ECONOMY_MIGRATION_BET_SEEN, false);
		}
	}

	void Awake()
	{
		startX = arrowParent.localPosition.x;
		startY = arrowParent.localPosition.y;
		
		lightSwitchTimer = new GameTimer(LIGHT_TIME);
		
		StatsManager.Instance.LogCount(
			counterName:"dialog",
			kingdom:	"new_economy_intro",
			phylum:		trackingPhylum,
			genus:		"view"
		);

		StartCoroutine(playSoundWhenReady());
	}
	
	// Wait for the loading screen to go away before playing the sound.
	// Mainly for the spin panel implementation.
	private IEnumerator playSoundWhenReady()
	{
		while (Loading.isLoading)
		{
			yield return null;
		}
		
		HyperEconomyIntroBuy.playSound();
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
		
		// Make the arrow float up/down or left/right a little.
		CommonTransform.setX(arrowParent, CommonEffects.pulsateBetween(startX, arrowFloatDestination.x, 5.0f));
		CommonTransform.setY(arrowParent, CommonEffects.pulsateBetween(startY, arrowFloatDestination.y, 5.0f));

		// If the player swiped to start spinning while this is showing, then just hide it now.
		if (SlotBaseGame.instance != null && SlotBaseGame.instance.isGameBusy)
		{
			closeMe();
		}
	}
	
	// NGUI button callback
	void screenTouched()
	{
		closeMe();
	}
	
	private void closeMe()
	{
		if (Overlay.instance != null)	// It shouldn't be null
		{
			Overlay.instance.top.setButtons(true);
		}
		CustomPlayerData.setValue(CustomPlayerData.ECONOMY_MIGRATION_BET_SEEN, true);

		StatsManager.Instance.LogCount(
			counterName:"dialog",
			kingdom:	"new_economy_intro",
			phylum:		trackingPhylum,
			family:		"close",
			genus:		"click"
		);

		Destroy(gameObject);
		
		// Allow dialogs to be shown now.
		Scheduler.run();
	}
}
