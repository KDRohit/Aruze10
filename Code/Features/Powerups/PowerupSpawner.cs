using System.Collections.Generic;
using UnityEngine;

public class PowerupSpawner : MonoBehaviour
{
	[SerializeField] private UIGrid powerupsGrid;
	[SerializeField, PowerupCustomAttribute] public List<string> applicablePowerups;
	[SerializeField] private bool showIdleAnims = false;

	private GameObject powerupContainer;

	/// <summary>
	/// Set to true to toggle the materials for the textmeshpro instances on the powerup icons
	/// to use the stencil buffer masking shader
	/// </summary>
	public bool useTextMasks = false;

	private void Start()
	{
		setupPowerups();
	}
	
	private void setupPowerups()
	{
		AssetBundleManager.load(PowerupBase.POWERUP_ICON_CONTAINER_UI_PATH, timerLoadSuccess, timerLoadFailure);
	}

	private void timerLoadSuccess(string assetPath, object loadedObj, Dict data = null)
	{
		powerupContainer = loadedObj as GameObject;
		if (powerupContainer == null)
		{
			Debug.LogError("Could not load powerup container");
			return;
		}

		if (applicablePowerups == null || applicablePowerups.Count == 0)
		{
			Debug.LogWarning("Invalid powerups available");
			return;
		}
		
		for (int i = 0; i < applicablePowerups.Count; i++)
		{
			string powerupName = applicablePowerups[i];
			if (string.IsNullOrEmpty(powerupName))
			{
				continue;
			}
			PowerupBase powerup = PowerupsManager.getActivePowerup(powerupName);

			if (powerup != null && powerup.runningTimer != null && !powerup.runningTimer.isExpired)
			{
				addPowerup(powerup);
			}
		}

		powerupsGrid.repositionNow = true;
	}

	private void addPowerup(PowerupBase powerup)
	{
		GameObject container = CommonGameObject.instantiate(powerupContainer, powerupsGrid.transform) as GameObject;
		PowerupTimer timerReference = container.GetComponent<PowerupTimer>();
		if (timerReference != null)
		{
			timerReference.init(powerup);
			if (showIdleAnims)
			{
				timerReference.playIdleAnims();
			}

			timerReference.useTextMasks = useTextMasks;
		}

		//if this powerup is a daily bonus time powerup, don't enable button as we don't have a card for it
		if (powerup.name.Contains("dailybonus"))
		{
			ButtonHandler handler = container.GetComponent<ButtonHandler>();
			if (handler != null)
			{
				handler.enabled = false;
			}
		}
		
	}

	private void timerLoadFailure(string assetPath, Dict data = null)
	{
		Debug.LogError("Failed to load powerup icon at path " + assetPath);
	}
}
