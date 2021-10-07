using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using TMPro;

/*
Handles default override of collect bonus dialog.
*/

public class CollectBonusDialogDefault : CollectBonusDialog
{
	public Renderer backgroundRenderer;
	public GameObject panelParent;

	public CollectBonusVersion versionData;

	public override void init()
	{

		// this  switches between original and the newer super streak version
		// once the PM's decide all players are switched over to super streak
		// this line can be removed along with the Collect Bonus Animation child object
		// in the Collect Bonus prefab
		versionData.versionCheck(this, ExperimentWrapper.SuperStreak.isInExperiment);

		base.init();
		
		downloadedTextureToRenderer(backgroundRenderer, 0);
	}

	[System.Serializable]
	public class CollectBonusVersion
	{
		public Renderer backgroundRenderer;
		public GameObject panelParent;
		//Top screen assets
		public Animator anim;
		public Animator topBarAnim;
		public Animator superStreakAnim;		
		public Transform slider;
		public GameObject fillObject;
		public CollectBonusDayBox[] dayBoxes;

		//mid screen assets
		public CollectBonusScoreBox levelBox;
		public CollectBonusScoreBox vipBox;
		public CollectBonusScoreBox friendBox;
		public CollectBonusScoreBox streakBox;
		public VIPNewIcon vipIcon;
		public GameObject friendIcon;
		public GameObject facebookConnectButton;
		public TextMeshPro connectButtonLabel;
		public TextMeshPro noVIPText;
		public TextMeshPro noFriendsText;
		public TextMeshPro streakMultiplierText;
		public TextMeshPro streakMultiplierTextActive;
		public TextMeshPro streakMultiplierTextGlow;

		//Bottom screen assets
		public TextMeshPro finalScore;
		public GameObject collectButton;

		public TextMeshPro watchToEarnLabel;
		public GameObject watchToEarnButton;

		public GameObject sevenDaysPlusFX;

		public void versionCheck(CollectBonusDialogDefault dialog, bool isNewVersion)
		{
			if (isNewVersion)
			{
				swap(dialog);
			}
			else
			{
				prepForOriginal(dialog);
			}				
		}
	
		private void swap(CollectBonusDialogDefault dialog)
		{
			dialog.panelParent.SetActive(false);
			dialog.panelParent = panelParent;
			dialog.panelParent.SetActive(true);

			dialog.anim = anim;
			dialog.topBarAnim = topBarAnim;
			dialog.superStreakAnim = superStreakAnim;
			dialog.backgroundRenderer = backgroundRenderer;
			dialog.slider = slider;

			dialog.fillObject = fillObject;

			dayBoxes.CopyTo(dialog.dayBoxes, 0);

			dialog.levelBox = levelBox;
			dialog.vipBox = vipBox;
			dialog.friendBox = friendBox;
			dialog.streakBox = streakBox;
			dialog.vipIcon = vipIcon;
			dialog.friendIcon = friendIcon;
			dialog.facebookConnectButton = facebookConnectButton;
			dialog.connectButtonLabel = connectButtonLabel;
			dialog.noVIPText = noVIPText;
			dialog.noFriendsText = noFriendsText;
			dialog.streakMultiplierText = streakMultiplierText;
			dialog.streakMultiplierTextActive = streakMultiplierTextActive;
			dialog.streakMultiplierTextGlow = streakMultiplierTextGlow;
			dialog.finalScore = finalScore;
			dialog.collectButton = collectButton;
			dialog.watchToEarnLabel = watchToEarnLabel;
			dialog.watchToEarnButton = watchToEarnButton;
			dialog.sevenDaysPlusFX = sevenDaysPlusFX;
		}

		private void prepForOriginal(CollectBonusDialogDefault dialog)
		{
			dialog.panelParent.SetActive(true);
			panelParent.SetActive(false);
		}
	}	

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
		
	}
}




