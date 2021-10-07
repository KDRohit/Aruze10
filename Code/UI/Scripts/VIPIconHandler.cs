using System;
using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class VIPIconHandler : MonoBehaviour
{
	public bool shouldSetToPlayerLevel = false;

	public VIPNewIcon vipIcon;
	public VIPNewIcon linkedVipIcon;

	private VIPNewIcon activeIcon;
	private bool isSetup = false;
	
	public void Awake()
	{
		if (Application.isPlaying)
		{
			SlotsPlayer.instance.onVipLevelUpdated += updateVIPLevel;
			checkForPowerup();
			updateVIPLevel();
		}
	}

	private void OnDestroy()
	{
		SlotsPlayer.instance.onVipLevelUpdated -= updateVIPLevel;
		PowerupsManager.removeEventHandler(onPowerupActivated);
	}

	public void updateVIPLevel()
	{
		if (shouldSetToPlayerLevel)
		{
			// Set this to be the Players Level.
			setLevel(SlotsPlayer.instance.adjustedVipLevel);
		}
	}

	private void checkForPowerup()
	{
		if (PowerupsManager.isPowerupsEnabled)
		{
			if (PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_VIP_BOOSTS_KEY))
			{
				updateVIPLevel();
				PowerupsManager.getActivePowerup(PowerupBase.POWER_UP_VIP_BOOSTS_KEY).runningTimer.registerFunction(onPowerupExpired);
			}
			else
			{
				PowerupsManager.addEventHandler(onPowerupActivated);
			}
		}
	}

	private void onPowerupActivated(PowerupBase powerup)
	{
		if (powerup.name == PowerupBase.POWER_UP_VIP_BOOSTS_KEY)
		{
			updateVIPLevel();
			powerup.runningTimer.registerFunction(onPowerupExpired);
		}
	}

	private void onPowerupExpired(Dict  args = null, GameTimerRange originalTimer = null)
	{
		updateVIPLevel();
	}

	private void setupActiveIcon()
	{
		if (!isSetup)
		{
			// Now that LinkedVIP is going to be always on, do some checks to make sure that if only one is set, we dont turn it off even if the experiment is off.
			if (vipIcon == null && linkedVipIcon == null)
			{
				activeIcon = null;
				Debug.LogErrorFormat("VIPIconHandler.cs -- Awake -- both icons are null, something is wrong here.");
				return;
			}
			else if (linkedVipIcon == null)
			{
				activeIcon = vipIcon;
				return;
			}
			else if (vipIcon == null)
			{
				activeIcon = linkedVipIcon;
				return;
			}
			
			// If they are in the experiment and connected to the new network.
			if (LinkedVipProgram.instance.shouldSurfaceBranding &&
				linkedVipIcon != null)
			{
				SafeSet.componentGameObjectActive(linkedVipIcon, true);
				SafeSet.componentGameObjectActive(vipIcon, false);
				activeIcon = linkedVipIcon;
			}
			else
			{
				SafeSet.componentGameObjectActive(vipIcon, true);
				SafeSet.componentGameObjectActive(linkedVipIcon, false);
				activeIcon = vipIcon;
			}
		    isSetup = true;
		}
	}
	

	// Sets the icon to the players current level.
	public void setToPlayerLevel(string statusBoostString = "")
	{
		setLevel(SlotsPlayer.instance.vipNewLevel, statusBoostString);
	}

    // Status boost string is use by the VIP Status Boost event to pair features with a bosted VIP level, should the event be active
	public void setLevel(int vipLevel, string statusBoostString = "")
	{
		setupActiveIcon(); // In case we try to set this before Awake() gets called, to it here.
		if (activeIcon == null)
		{
			return;
		}
		
		VIPLevel level = VIPLevel.find(vipLevel, statusBoostString);
		if (level != null)
		{
			activeIcon.setLevel(level);
		}
		else
		{
			activeIcon.gameObject.SetActive(false);
		}
	}

#if UNITY_EDITOR
	public bool isSmall = false;
	public bool shouldCreateDefaults = false;
	private const string VIP_ICON_DEFAULT = "Prefabs/Misc/VIP Icon";
	private const string LINKED_VIP_ICON_DEFAULT = "Prefabs/Misc/Linked VIP Icon";

	private const string VIP_ICON_DEFAULT_SMALL = "Prefabs/Misc/VIP Icon Small";
	private const string LINKED_VIP_ICON_DEFAULT_SMALL = "Prefabs/Misc/Linked VIP Icon Small";
	
	void Update()
	{
		if (shouldCreateDefaults)
		{
			createDefaults();
			shouldCreateDefaults = false;
		}
	}
	
	public void createDefaults()
	{
		if (vipIcon == null)
		{
			Debug.Log("VIPIconHandler -- creating vip default");
			// Load in the default normal vip card.
			string prefabPath = isSmall ? VIP_ICON_DEFAULT_SMALL : VIP_ICON_DEFAULT;
			GameObject iconPrefab = SkuResources.loadSkuSpecificResourcePrefab(prefabPath);
			GameObject iconObject = CommonGameObject.instantiate(iconPrefab) as GameObject;
			iconObject.transform.parent = transform;
			iconObject.transform.localPosition = Vector3.zero;
			iconObject.transform.localScale = Vector3.one;
			iconObject.name = "VIP New Icon";
			vipIcon = iconObject.GetComponent<VIPNewIcon>();
		}
		if (linkedVipIcon == null)
		{
			Debug.Log("VIPIconHandler -- creating linked vip default");
			// Load in the default linked vip card.
			string prefabPath = isSmall ? LINKED_VIP_ICON_DEFAULT_SMALL : LINKED_VIP_ICON_DEFAULT;
			GameObject iconPrefab = SkuResources.loadSkuSpecificResourcePrefab(prefabPath);
			GameObject iconObject = CommonGameObject.instantiate(iconPrefab) as GameObject;
			iconObject.transform.parent = transform;
			iconObject.transform.localPosition = Vector3.zero;
			iconObject.transform.localScale = Vector3.one;
			iconObject.name = "Linked VIP Icon";
			linkedVipIcon = iconObject.GetComponent<VIPNewIcon>();
		}
	}
#endif
	
}