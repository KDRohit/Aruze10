using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;


public class VIPRevampCarousel : MonoBehaviour, IResetGame
{
	// =============================
	// PUBLIC
	// =============================
	public PageScroller pageScroller;
	public GameObject gameOption;
	public TextMeshPro tickerLabel;

	// =============================
	// PROTECTED
	// =============================
	protected List<string> gameImageFileNames = new List<string>();
	protected List<Texture> gameImages = new List<Texture>();
	protected GameTimer delayTimer;
	protected Texture currentLoadedTex = null;

	// =============================
	// CONST
	// =============================
	protected const int SLIDE_DELAY = 5;

	void Awake()
	{
		addGameImagePaths();
		RoutineRunner.instance.StartCoroutine(populateGameImages());	// The image should be loaded already by now, but just in case.populateGameImages();
	}

	void Update()
	{
		if ( delayTimer != null && delayTimer.isExpired )
		{
			// Go to the next slide.
			if ( pageScroller.scrollPos + 1 == pageScroller.maxPage )
			{
				pageScroller.resetToFirstPage();
			}
			else
			{
				pageScroller.scrollPosQuietly++;
			}
			startSlideTimer();
		}
	}

	void OnDestroy()
	{
		RoutineRunner.instance.StopCoroutine("populateGameImages");
	}

	private void onCreatePanel(GameObject pagePanel, int index)
	{
		GameObject option = CommonGameObject.instantiate(gameOption) as GameObject;
		option.transform.parent = pagePanel.transform;
		option.transform.localScale = Vector3.one;
		option.transform.localPosition = Vector3.zero;

		VIPCarouselButton button = option.GetComponent<VIPCarouselButton>();
		button.setup(gameImages[index]);

		if (tickerLabel != null && ProgressiveJackpot.vipRevampGrand != null)
		{
			ProgressiveJackpot.vipRevampGrand.registerLabel(tickerLabel);
		}
	}

	public void startSlideTimer()
	{
		delayTimer = new GameTimer(SLIDE_DELAY);
	}

	public void stopSlideTimer()
	{
		delayTimer = null;
	}

	public void onVIPClicked()
	{
		MOTDFramework.queueCallToAction(MOTDFramework.VIP_ROOM_CALL_TO_ACTION);
		StatsManager.Instance.LogCount("lobby", "", "vip_carousel", "", "vip_room", "click");
	}

	/// <summary>
	///   Add all the game image paths
	/// </summary>
	public void addGameImagePaths()
	{
		LobbyInfo vipRevamp = LobbyInfo.find(LobbyInfo.Type.VIP_REVAMP);
		if (vipRevamp != null)
		{
			foreach (LobbyOption option in vipRevamp.allLobbyOptions)
			{
				LobbyGame game = option.game;
				if (game != null)
				{
					gameImageFileNames.Add(SlotResourceMap.getLobbyImagePath(game.groupInfo.keyName, game.keyName));
				}
			}
		}
	}	

	/// <summary>
	///   Load all the game images
	/// </summary>
	public IEnumerator populateGameImages()
	{
		for (int i = 0; i < gameImageFileNames.Count; i++)
		{
			yield return RoutineRunner.instance.StartCoroutine(DisplayAsset.loadTextureFromBundle(gameImageFileNames[i], addGameImage, skipBundleMapping:true, pathExtension:".png"));
		}
	}

	/// <summary>
	///   Store the loaded game image textures
	/// </summary>
	protected void addGameImage(Texture2D tex, Dict data)
	{
		gameImages.Add( tex );
		if ( gameImages.Count == gameImageFileNames.Count )
		{
			if (LobbyLoader.lastLobby == LobbyInfo.Type.MAIN && pageScroller != null && pageScroller.transform != null)
			{
				startSlideTimer();
				pageScroller.init( gameImages.Count, onCreatePanel );
			}
		}
	}

	public static void resetStaticClassData()
	{
		
	}
}