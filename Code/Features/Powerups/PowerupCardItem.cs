using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PowerupCardItem : MonoBehaviour
{
	private PowerupBase powerup;
	private GameObject powerupContainer;
	[SerializeField] private Transform anchor;
	[SerializeField] private ButtonHandler actionButton;
	[SerializeField] private TextMeshPro actionButtonLabel;
	[SerializeField] private UISprite powerupLogo;
	[SerializeField] private Animator cardEffectAnim;

	private bool collected = false;
	private bool isRunning = false;
	private readonly Color unCollectedColor = new Color(0.5f, 0.5f ,0.5f);
	private CollectableCard.CardLocation location;

	public void setActionButton(bool enabled)
	{
		actionButton.gameObject.SetActive(enabled && !string.IsNullOrEmpty(powerup.actionName) && powerup.canPerformAction);
	}

	public void setupPowerup(CollectableCardData cardData, string powerupName, CollectableCard.CardLocation location)
	{
		collected = cardData.isCollected;
		isRunning = PowerupsManager.hasActivePowerupByName(powerupName);
		this.location = location;

		if (isRunning)
		{
			powerup = PowerupsManager.getActivePowerup(powerupName);
		}
		else
		{
			powerup = PowerupBase.getPowerupByName(powerupName);
		}

		if (powerup == null)
		{
			return;
		}

		if (powerup.runningTimer != null)
		{
			powerup.runningTimer.registerFunction(onTimerExpired);
		}
		
		bool locationValid = location == CollectableCard.CardLocation.PACK_DROP ||
		                     location == CollectableCard.CardLocation.DETAILED_VIEW;
		bool showActionButton = locationValid && !string.IsNullOrEmpty(powerup.actionName) && isRunning;
		actionButton.gameObject.SetActive(showActionButton);
		actionButton.registerEventDelegate(actionButtonClicked);
		actionButtonLabel.text = Localize.text(powerup.actionName);
		
		AssetBundleManager.load(PowerupBase.POWERUP_ICON_CONTAINER_UI_PATH, timerLoadSuccess, timerLoadFailure);
		
		if (location == CollectableCard.CardLocation.SET_VIEW && !collected)
		{
			powerupLogo.color = unCollectedColor;
		}

		if (location == CollectableCard.CardLocation.PACK_DROP)
		{
			RoutineRunner.instance.StartCoroutine(playEffect());
		}
	}
	
	void OnDestroy()
	{
		if (powerup != null && powerup.runningTimer != null)
		{
			powerup.runningTimer.removeFunction(onTimerExpired);
		}
	}
	
	private void onTimerExpired(Dict args, GameTimerRange sender)
	{
		//Disable action button on timer expiration
		actionButton.gameObject.SetActive(false);
	}

	private void timerLoadSuccess(string assetPath, object loadedObj, Dict data = null)
	{
		powerupContainer = loadedObj as GameObject;
		if (powerup != null)
		{
			GameObject container = CommonGameObject.instantiate(powerupContainer, anchor) as GameObject;
			PowerupTimer timerReference = container.GetComponent<PowerupTimer>();

			if (timerReference != null)
			{
				bool delay = location == CollectableCard.CardLocation.PACK_DROP;
				timerReference.init(powerup, isRunning, collected, false, delay);
				timerReference.setIsCollectedCard(collected || location == CollectableCard.CardLocation.PACK_DROP);

				// even if the card isn't collected anymore, if the powerup is active, it shouldn't be tinted
				if (PowerupsManager.hasActivePowerupByName(powerup.name))
				{
					timerReference.powerupTint.setTintColor(true);
				}

				if (location == CollectableCard.CardLocation.DETAILED_VIEW)
				{
					BoxCollider timerCollider = timerReference.GetComponent<BoxCollider>();
					if (timerCollider != null)
					{
						timerCollider.enabled = false;
					}
				}
			}
		}
	}

	private void timerLoadFailure(string assetPath, Dict data = null)
	{
		Debug.LogError("Failed to load powerup icon at path " + assetPath);
	}
	
	private IEnumerator playEffect()
	{
		while (!PackDroppedDialog.completedPowerupDropRoutine)
		{
			yield return null;
		}

		yield return new WaitForSeconds(1);
		
		if (this != null && PowerupsManager.isPowerupStreakActive)
		{
			cardEffectAnim.Play("on");
			Audio.play("PowerupAwardedCollections");
		}
	}

	private void actionButtonClicked(Dict args = null)
	{
		powerup.doAction();
	}
}
