using System.Collections.Generic;
using Com.HitItRich.Feature.VirtualPets;
using UnityEngine;

public class MainLobbyBottomOverlay : MonoBehaviour 
{
	private const int Z_POSITION = -60;		// -60 seems to be the sweet spot, if you go to -70 text from daily race and vip ticker bleed through the lobby v3 ftue.
	
	protected bool shouldRefreshUI;
	public static MainLobbyBottomOverlay instance
	{
		get
		{
			return MainLobbyBottomOverlayV4.instance;
		}
	}

	public static string skuSpecificPrefabPath
	{
		get
		{
			return "Features/Lobby V3/Lobby Prefabs/Bottom Overlay Panel V4";
		}
	}

	public static string prefabPath
	{
		get
		{
			return "Assets/Data/HIR/Bundles/Initialization/Features/Lobby V3/Lobby Prefabs/Bottom Overlay Panel V4.prefab";
		}
	}

	public virtual void refreshUI()
	{
	}
	
	public virtual void repositionGrid()
	{
	}

	protected virtual float yOffsetPosition
	{
		get
		{
			return 0;
		}
	}

	protected virtual void init()
	{
		//position overlay
		if (MainLobby.hirV3 != null)
		{
			// attach ourselves to the bottom section of the main lobby
			gameObject.transform.parent = MainLobby.hirV3.bottomSection.transform;
			Vector3 localPosition = Vector3.zero;
			localPosition.z = Z_POSITION; // supposed to be above the top overlay
			gameObject.transform.localPosition = localPosition;
			gameObject.transform.localScale = Vector3.one;
			CommonTransform.setY(gameObject.transform, yOffsetPosition);
		}
		else
		{
			Debug.LogError("Cannot position bottom bar -- main lobby is null");
		}

		//initailize the buttons
		initializeDailyBonus();

		if (shouldShowWeeklyRace())
		{
			initializeRaces();
		}

		//These were made to be configurable in LOLA and appear with the other options as part of the pets feature.
		//No longer appear in bottom bar if pets is active for the player
		if (VirtualPetsFeature.instance == null || !VirtualPetsFeature.instance.isEnabled)
		{
			//If pets isn't active, verify these still aren't set in LOLA before spawning them in the bottom overlay
			LobbyInfo mainLobbyInfo = LobbyInfo.find(LobbyInfo.Type.MAIN);
			if (!mainLobbyInfo.pinnedActions.Contains("vip_lobby"))
			{
				initializeVIPRoom();
			}

			if (!mainLobbyInfo.pinnedActions.Contains("max_voltage_lobby"))
			{
				initializeMaxVoltage();
			}
		}

		initializeRichPass();

		if (!ExperimentWrapper.EUEFeatureUnlocks.isInExperiment && (!EliteManager.isActive || !Collectables.isActive()))
		{
			initializeFriends();
		}

		if (ExperimentWrapper.VirtualPets.isInExperiment)
		{
			initializeVirtualPet();
		}
	}

	protected virtual bool shouldShowWeeklyRace()
	{
		// Weekly race is always in the overlay when in eue feature unlocks experiment
		// rich pass and collectables wont appear in the bottom overlay, show weekly race
		return !ExperimentWrapper.EUEFeatureUnlocks.isInExperiment && 
		       CampaignDirector.richPass == null || 
		       !CampaignDirector.richPass.isActive || 
		       !Collectables.isActive();
	}

	public void setUpdateFlag()
	{
		shouldRefreshUI = true;
	}

	protected virtual void Update()
	{
		if (shouldRefreshUI)
		{
			refreshUI();
		}
	}

	public virtual void cleanUp()
	{
	}

	public virtual void initializeRaces()
	{	
	}

	protected virtual void initializeDailyBonus()
	{
	}

	protected virtual void initializeFriends()
	{
	}

	protected virtual void initializeCollections()
	{
	}

	protected virtual void initializeVIPRoom()
	{
	}

	protected virtual void initializeMaxVoltage()
	{
	}
	
	protected virtual void initializeRichPass()
	{	
	}

	protected virtual void initializeVirtualPet()
	{
	}
	
	// Tell the bottom bar that there are new collections cards (for notificaiton);
	public virtual void initNewCardsAlert()
	{
	}

	public virtual void initNewRichPassAlert()
	{
	}



}
