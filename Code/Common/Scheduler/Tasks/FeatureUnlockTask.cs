using UnityEngine;
using System.Collections;
using Com.LobbyTransitions;
using Com.Scheduler;
using Com.States;

public class FeatureUnlockTask : SchedulerTask
{
	private BottomOverlayButtonToolTipController unlockedToolTipController;
	private int lobbyPageIndex = -1;
	private string featureKey = "";
	
	public FeatureUnlockTask(Dict args = null) : base(args)
	{
		if (args != null)
		{
			unlockedToolTipController = args.getWithDefault(D.OBJECT, null) as BottomOverlayButtonToolTipController;
			lobbyPageIndex = (int)args.getWithDefault(D.INDEX, -1);
			featureKey = (string) args.getWithDefault(D.KEY, "");
		}
	}

	public override void execute()
	{
		base.execute();
		if (unlockedToolTipController == null || !unlockedToolTipController.gameObject.activeInHierarchy)
		{
			Scheduler.removeTask(this);
		}
		else
		{
			if (MainLobby.hirV3 != null && lobbyPageIndex >= 0 && lobbyPageIndex != MainLobby.pageBeforeGame)
			{
				//Set this to the default value so we don't actually jump to game page.
				//We want to go to the page with the feature being unlocked instead
				MainLobby.pageBeforeGame = MainLobby.DEFAULT_LAST_GAME_PAGE; 

				//Don't go to the page if we're already on it
				if (lobbyPageIndex != MainLobby.hirV3.getTrackedScrollPosition())
				{
					MainLobby.hirV3.pageController.goToPageAfterInit(lobbyPageIndex);
				}
			}
			
			unlockedToolTipController.startFeatureUnlockedPresentation(onAnimationFinished, this);
		}
	}

	public void onAnimationFinished(Dict args = null)
	{
		if (!string.IsNullOrEmpty(featureKey))
		{
			FeatureUnlockData data = EueFeatureUnlocks.getUnlockData(featureKey);
			data.unlockAnimationSeen = true;
		}
		Scheduler.removeTask(this);
	}
}