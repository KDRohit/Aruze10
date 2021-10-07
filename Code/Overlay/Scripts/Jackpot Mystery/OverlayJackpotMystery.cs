using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Controls the ingame parts of the Overlay, such as the progressive jackpot and mystery gift header thing.
*/

public abstract class OverlayJackpotMystery : MonoBehaviour
{
	[SerializeField] private GameOverlayFeatureDisplay[] featureDisplays;
	[SerializeField] private UIAnchor featureOffsetForHi03Anchor;
	public GameObject tokenAnchor;
	
	[Tooltip("If a GameOverlayFeatureDisplay or TokenCollectionModule doesn't define its own reelsBoundsLimit, this default one will be used.")]
	[SerializeField] private BoxCollider2D _defaultFeatureReelsBoundsLimit;
	public BoxCollider2D defaultFeatureReelsBoundsLimit
	{
		get { return _defaultFeatureReelsBoundsLimit;  }
	}
	
	private bool isOverlayVisible = true;

	private LobbyGame currentGame = null;
	private const string TRANSITION_IN = "Feature Bar Transition In";
	private const string TRANSITION_OUT = "Feature Bar Transition Out";	
	
	[System.NonSerialized] public TokenCollectionModule tokenBar;
	
	public const string PREFAB_PATH = "Jackpot Overlays/Prefabs/Jackpot and Mystery";

	public bool isOverlayActive 
	{
		get
		{
			for (int i = 0; i < featureDisplays.Length; i++)
			{
				if (featureDisplays[i].gameObject.activeSelf)
				{
					return true;
				}
			}
			return tokenAnchor.activeSelf;
		}
	}

	// No base method needed.
	public virtual void showToaster(){}
	
	void Awake()
	{
		if (Application.isPlaying)
		{
			ResolutionChangeHandler.instance.addOnResolutionChangeDelegate(onResolutionChange);
		}

		// Hide this until getting into a game, then the SpinPanel takes over.
		hide();
	}

	private void OnDestroy()
	{
		if (Application.isPlaying)
		{
			ResolutionChangeHandler.instance.removeOnResolutionChangeDelegate(onResolutionChange);
		}
	}

	public void onResolutionChange()
	{
		// Force update the anchor so the position is for sure updated for where
		// the Jackpot and Mystery bar is
		featureOffsetForHi03Anchor.reposition();
		
		for (int i = 0; i < featureDisplays.Length; i++)
		{
			featureDisplays[i].updatePosition(featureOffsetForHi03Anchor.transform.localPosition);
		}
	}

	// This update is called by Overlay, which is why it's lower case.
	public void update()
	{
	}

	// function to allow the OverlayJackpotMystery to be forced on, so the effects and layout of a slot game can be viewed and tested in the editor
	public void forceShowInEditor()
	{
#if !ZYNGA_PRODUCTION
		// just going to force on the jackpot parent since it is probably the largest
		for (int i = 0; i < featureDisplays.Length; i++)
		{
			GameOverlayFeatureDisplay currentFeature = featureDisplays[i];
			if (currentFeature is GameOverlayFeatureDisplayMultiprogressive)
			{
				currentFeature.gameObject.SetActive(true);
			}
		}
#endif
	}

	// Shows the jackpot and mystery gift UI elements for the spin panel when a game is played.
	public void show()
	{
		if (GameState.game != null)
		{
			for (int i = 0; i < featureDisplays.Length; i++)
			{
				if (currentGame != GameState.game)
				{
					// Make sure we init if we haven't for the current game to setup all the labels.
					featureDisplays[i].init();
				}
				
				// If the game is the same one as we already inititalized for, then just call show on it.
				featureDisplays[i].show();
			}

			tokenAnchor.SetActive(SpinPanel.instance.isShowingCollectionOverlay);

			// Now set the current game.
			currentGame = GameState.game;

			isOverlayVisible = true;

			// make sure the overlay is in the right place
			if (!currentGame.isMultiProgressive)
			{
				// Because hi03 isn't setup to scale to adjust for jackpot bars, if it is one of the small
				// jackpot bars it will be adjusted off to the side.
				if (currentGame.keyName == "hi03")
				{
					adjustSmallFeatureTransformForHi03();
				}
				else
				{
					restoreFeatureTransforms();
				}
			}
			else
			{
				restoreFeatureTransforms();
			}
		}
	}

	public void hideTooltip()
	{
		for (int i = 0; i < featureDisplays.Length; i++)
		{
			featureDisplays[i].hideTooltip();
		}
	}

	// Sets visibility of the qualified/non-qualified elements.
	public void setQualifiedStatus()
	{
		if (GameState.game == null || SlotBaseGame.instance == null || !SpinPanel.instance.isShowingSpecialWinOverlay)
		{
			return;
		}
		bool isQualified = (SlotBaseGame.instance.currentWager >= GameState.game.specialGameMinQualifyingAmount);
		Audio.play(isQualified ? "WildInWinningPayline" : "windowscreen0");

		for (int i = 0; i < featureDisplays.Length; i++)
		{
			featureDisplays[i].setQualified(isQualified);
		}
	}
	
	// Hides all variations of the jackpot/mystery game overlay.
	public void hide()
	{
		// Parent elements only. Probably don't need logging here.		
		for (int i = 0; i < featureDisplays.Length; i++)
		{
			featureDisplays[i].hide();
		}

		if (tokenBar != null)
		{
			tokenBar.hideBar();
			SafeSet.gameObjectActive(tokenAnchor, false, false);
		}
	}

	public void setButtons(bool isEnabled)
	{
		for (int i = 0; i < featureDisplays.Length; i++)
		{
			featureDisplays[i].setButtons(isEnabled);
		}
	}

	public void mysteryGiftExpired()
	{
		// We cant have mystery gifts on a game that has any other topper, so jsut hide them all when this expires.
		hide();
	}

	// Adjusts the single progressive position, only used by the game hi03 which needs this adjustment
	// to make sure that the progressive and its own built in jackpot UI do not overlap
	private void adjustSmallFeatureTransformForHi03()
	{
		// Make sure that only small overlay bars are shifted over.  The multi progressive
		// can be shifted because it is too big.  If that case comes up we'll just log an error.
		GameOverlayFeatureDisplay feature = getActiveFeatureDisplay();
		
		if (feature != null && !(feature is GameOverlayFeatureDisplayMultiprogressive))
		{
			// Force update the anchor so the position is for sure updated for where
			// the Jackpot and Mystery bar is
			featureOffsetForHi03Anchor.reposition();
			
			feature.offsetToSideForHi03(featureOffsetForHi03Anchor.transform.localPosition);
		}
	}

	// Restores the jackpot mystery parent transform to what it was before it was modified by calling adjustSmallBarFeatureTransform
	private void restoreFeatureTransforms()
	{
		for (int i = 0; i < featureDisplays.Length; i++)
		{
			featureDisplays[i].resetPosition();
		}
	}

	public void setUpTokenBar(string gameKey = "")
	{
		if (ExperimentWrapper.VIPLobbyRevamp.isInExperiment && (VIPLobbyHIRRevamp.instance != null || (SlotBaseGame.instance != null && SlotBaseGame.instance.isVipRevampGame)))
		{
			string prefabPath = string.Format("Features/VIP Revamp/VIP Revamp/Prefabs/VIP Token Collection");
			AssetBundleManager.load(this, prefabPath, tokenBarLoadSuccess, tokenBarLoadFailure);
		}

		else if ((MaxVoltageLobbyHIR.instance != null || (SlotBaseGame.instance != null && SlotBaseGame.instance.isMaxVoltageGame)))
		{
			string prefabPath = string.Format("Features/Max Voltage/Prefabs/Max Voltage Token Collection");
			AssetBundleManager.load(this, prefabPath, tokenBarLoadSuccess, tokenBarLoadFailure, isSkippingMapping:true, fileExtension:".prefab");
		}

		else if (ExperimentWrapper.RoyalRush.isInExperiment && (RoyalRushEvent.instance.getInfoByKey(gameKey) != null || (SlotBaseGame.instance != null && SlotBaseGame.instance.isRoyalRushGame)))
		{
			string prefabPath = string.Format("Features/Royal Rush/Prefabs/Royal Rush Meter Parent");
			AssetBundleManager.load(this, prefabPath, tokenBarLoadSuccess, tokenBarLoadFailure);
		}
	}

	private void tokenBarLoadSuccess(string path, UnityEngine.Object obj, Dict args = null)
	{
		GameObject prefab = obj as GameObject;
		GameObject go = NGUITools.AddChild(tokenAnchor, prefab);
		tokenBar = go.GetComponent<TokenCollectionModule>();
		if (SlotBaseGame.instance != null)
		{
			SlotBaseGame.instance.onTokenBarLoadFinished(); //If we loaded the token bar while in the base game then we should let the slot know we finished
		}
		else if (MaxVoltageLobbyHIR.instance != null && tokenBar as MaxVoltageTokenCollectionModule != null)
		{
			//Turn on and set up the meter if we loaded it while in the max voltage lobby
			tokenAnchor.SetActive(true);
			tokenBar.setupBar();
			MaxVoltageLobbyHIR.instance.applyMeterSetup();
		}
	}

	private void tokenBarLoadFailure(string path, Dict args = null)
	{
		Debug.LogErrorFormat("OverlayJackpotMysteryHIR.cs -- Token Bar -- failed to load prefab at path: {0}", path);
		if (SlotBaseGame.instance != null)
		{
			SlotBaseGame.instance.onTokenBarLoadFinished(); //Let the slot know we failed loading so we can at least hide the loading screen and let the user spin without the UI
		}
	}

	// Determine what feature is currently turned on if any
	// Will return null if no feature is currently turned on
	public GameOverlayFeatureDisplay getActiveFeatureDisplay()
	{
		for (int i = 0; i < featureDisplays.Length; i++)
		{
			if (featureDisplays[i].gameObject.activeSelf)
			{
				return featureDisplays[i];
			}
		}

		return null;
	}
}
