using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Com.Scheduler;

public class GenericLobbyPortalV4 : GenericLobbyPortal 
{
	public BasePageScroller pageScroller;
	public GameObject gameOption;
	public GameObject optionParent;
	[SerializeField] private GameObject levelLock;
	[SerializeField] private LabelWrapperComponent levelLockLabel;

	private List<string> gameImageFileNames = new List<string>();
	private List<Texture> gameImages = new List<Texture>();

	private GameTimer delayTimer;

	private const int SLIDE_DELAY = 5;

	protected override void Awake()
	{
		base.Awake();
		sortIndex = 100;
	}

	private void Update()
	{
		if (delayTimer != null && delayTimer.isExpired)
		{
			// Go to the next slide.
			if (pageScroller.scrollPos + 1 == pageScroller.maxPage)
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

	public bool addGameImagePaths()
	{
		LobbyInfo info = LobbyInfo.find(lobbyType);
		if (info != null)
		{
			//remove invalid games and copy to list we can manipulate
			List<LobbyOption> allGames = new List<LobbyOption>();
			foreach (LobbyOption option in info.allLobbyOptions)
			{
				LobbyGame game = option.game;
				if (game != null)
				{
					allGames.Add(option);

				}
			}

			//sort
			sortGames(allGames);

			//load images
			for (int i = 0; i < allGames.Count; ++i)
			{
				gameImageFileNames.Add(SlotResourceMap.getLobbyImagePath(allGames[i].game.groupInfo.keyName, allGames[i].game.keyName));
			}

			return true;
		}

		return false;
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

	public void startSlideTimer()
	{
		delayTimer = new GameTimer(SLIDE_DELAY);
	}

	public void stopSlideTimer()
	{
		delayTimer = null;
	}

	public override void setup(bool isLocked, bool loadBGImage = true, bool loadTabImage = true)
	{
		base.setup(isLocked, loadBGImage, loadTabImage);
		if (mainButtonClickedHandler != null)
		{
			mainButtonClickedHandler.SetActive(!isLocked || enableIfLocked);
		}

		isWaitingForTextures = false;

		if (addGameImagePaths())
		{
			RoutineRunner.instance.StartCoroutine(populateGameImages());
		}
		else
		{
			Bugsnag.LeaveBreadcrumb("Could not load images lobby portal");
		}
		
		if (isLevelLocked())
		{
			initLevelLock(false);
		}
		else if (needsToShowUnlockAnimation())
		{
			showUnlockAnimation();
		}
	}

	protected void addGameImage(Texture2D tex, Dict data)
	{
		gameImages.Add(tex);
		if ( gameImages.Count == gameImageFileNames.Count )
		{
			if (LobbyLoader.lastLobby == LobbyInfo.Type.MAIN && pageScroller != null && pageScroller.transform != null)
			{
				startSlideTimer();
				pageScroller.init( gameImages.Count, onCreatePanel, null, null, onDestroyPanel );
			}
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		RoutineRunner.instance.StopCoroutine("populateGameImages");
	}

	private void onCreatePanel(GameObject pagePanel, int index)
	{
		RecyclablePage page = pagePanel.GetComponent<RecyclablePage>();
		if (page != null && !page.isEmpty())
		{
			page.reset();
			page.init(Dict.create(D.DATA, gameImages[index]));
		}
		else
		{
			GameObject option = CommonGameObject.instantiate(gameOption) as GameObject;
			if (null != option)
			{
				pagePanel.transform.parent = optionParent.transform;
				option.transform.parent = pagePanel.transform;
				option.transform.localScale = Vector3.one;
				option.transform.localPosition = Vector3.zero;

				VIPCarouselButtonV3 button = option.GetComponent<VIPCarouselButtonV3>();
				if (button != null)
				{
					button.setup(gameImages[index]);
				}
			}
			else
			{
				Debug.LogError("Cannot instantiate panel prefab");
			}
		}
		
	}

	private void onDestroyPanel(GameObject pagePanel, int index)
	{
		IRecycle recScript = gameObject.GetComponent<IRecycle>();
		if (recScript != null)
		{
			//if this is a recyclable object;
			recScript.reset();
		}
	}

	protected override void logClick()
	{
		StatsManager.Instance.LogCount(
			counterName:"bottom_nav",
			kingdom:	"max_voltage",
			phylum:		SlotsPlayer.isFacebookUser ? "fb_connected" : "anonymous",
			genus:		"click"
		);
	}
}
