using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class VIPRevampCarouselV3 : MonoBehaviour, IResetGame
{
	// =============================
	// PUBLIC
	// =============================
	public BasePageScroller pageScroller;
	public GameObject gameOption;
	public GameObject optionParent;
	public TextMeshPro tickerLabel;

	// =============================
	// PROTECTED
	// =============================
	protected List<string> gameImageFileNames = new List<string>();
	protected List<Texture> gameImages = new List<Texture>();
	protected GameTimer delayTimer;


	// =============================
	// PRIVATE
	// =============================
	private int imageLoadTotal = 0;

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
		GameObject option = null;
		VIPCarouselButtonV3 button = null;
		pagePanel.transform.parent = optionParent.transform;
		RecyclablePage page = pagePanel.GetComponent<RecyclablePage>();
		if (page != null && !page.isEmpty())
		{
			page.reset();
			page.init(Dict.create(D.DATA, gameImages[index]));
		}
		else
		{
			option = CommonGameObject.instantiate(gameOption) as GameObject;
			option.transform.parent = pagePanel.transform;
			option.transform.localScale = Vector3.one;
			option.transform.localPosition = Vector3.zero;
			button = option.GetComponent<VIPCarouselButtonV3>();
			button.reset();
			button.init(Dict.create(D.DATA, gameImages[index]));
		}

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

	protected virtual void sortGames(List<LobbyOption> games)
	{
		games.Sort(LobbyOption.sortVIPOptions);
	}

	protected static int compareByVIPLevel(LobbyGame x, LobbyGame y)
	{
		if (x == null)
		{
			if (y == null)
			{
				//nulls are equal
				return 0;
			}

			//y is greater
			return -1;
		}
		else if (y == null)
		{
			//x is greater
			return 1;
		}

		if (x.vipLevel == null)
		{
			if (y.vipLevel == null)
			{
				return 0;
			}

			return -1;
		}
		else if (y.vipLevel == null)
		{
			return 1;
		}

		return x.vipLevel.levelNumber.CompareTo(y.vipLevel.levelNumber);
	}

	/// <summary>
	///   Add all the game image paths
	/// </summary>
	public void addGameImagePaths()
	{
		LobbyInfo vipRevamp = LobbyInfo.find(LobbyInfo.Type.VIP_REVAMP);
		if (vipRevamp != null)
		{
			List<LobbyOption> allGames = new List<LobbyOption>();
			foreach (LobbyOption option in vipRevamp.allLobbyOptions)
			{
				LobbyGame game = option.game;
				if (game != null)
				{
					allGames.Add(option);
				}
			}

			sortGames(allGames);

			for (int i = 0; i < allGames.Count; ++i)
			{
				gameImageFileNames.Add(SlotResourceMap.getLobbyImagePath(allGames[i].game.groupInfo.keyName, allGames[i].game.keyName));
			}
		}
	}	

	/// <summary>
	///   Load all the game images
	/// </summary>
	public IEnumerator populateGameImages()
	{
		imageLoadTotal = gameImageFileNames.Count;
		for (int i = 0; i < gameImageFileNames.Count; i++)
		{
			yield return RoutineRunner.instance.StartCoroutine(DisplayAsset.loadTextureFromBundle(gameImageFileNames[i], addGameImage, skipBundleMapping:true, pathExtension:".png"));
		}
	}

	/// <summary>
	///   Store the loaded game image textures
	/// </summary>
	private void addGameImage(Texture2D tex, Dict data)
	{
		if (tex == null)
		{
			--imageLoadTotal;
		}
		else
		{
			gameImages.Add( tex );
		}

		if ( gameImages.Count == imageLoadTotal )
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