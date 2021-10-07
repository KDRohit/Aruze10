using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/**
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
**/
namespace QuestForTheChest
{
	public class QFCBoardNodeObject : MonoBehaviour
	{
		[SerializeField] private UISprite nodeSprite;
		[SerializeField] private string spriteName;
		[SerializeField] private Transform homeTeamContainer;
		[SerializeField] private Transform awayTeamContainer;
		public Transform playerContainer;
		[SerializeField] private ClickHandler nodeClickHandler;
		[SerializeField] private Animator nodeAnimator;
		[SerializeField] private Animator coinAnimator;
		[SerializeField] private string completedAnimationIdleName;
		[SerializeField] private string completedAnimationOnName;
		[SerializeField] private string completedAnimationOffName;
		[SerializeField] private string coinOffAnimationName;
		[SerializeField] private string coinIdleAnimationName;
		[SerializeField] private string coinOutroAnimationName;
		[SerializeField] private string coinFlatAnimationName;

		[System.NonSerialized] public QFCBoardPlayerIconObject currentPlayerIcon;
		[System.NonSerialized] public QFCBoardPlayerIconObject homeTeamIcon;
		private QFCBoardPlayerIconObject awayTeamIcon;

		private bool occupiedByAwayTeamLeader = false;
		private bool occupiedByHomeTeamLeader = false;
		private bool occupiedByCurrentPlayerLeader = false;
		private bool occupiedByCurrentPlayer = false;

		private UIAtlas nodeAtlas;
		private int homeTeamOccupants = 0;
		private int awayTeamOccupants = 0;
		public bool isComplete = false;

		public void init(QFCMapDialog mapParent, QFCBoardNode nodeData, string homeTeamLeaderZid, string awayTeamLeaderZid, Dictionary<QFCMapDialog.QFCBoardPlayerIconType, GameObject> playerIconPrefabs, bool isVisited, HashSet<string> usedPlayerZids)
		{
			isComplete = isVisited;

			if (coinAnimator != null)
			{
				coinAnimator.Play(isComplete ? coinOffAnimationName : coinIdleAnimationName);
			}

			for (int i = 0; i < nodeData.occupiedBy.Count; i++)
			{
				string playerZid = nodeData.occupiedBy[i];

				//Verify player is not on multiple nodes
				if (usedPlayerZids.Contains(playerZid))
				{
					Bugsnag.LeaveBreadcrumb("Player: " + playerZid + " is occupying two qfc nodes at once");
					continue;
				}
				usedPlayerZids.Add(playerZid);

				if (QuestForTheChestFeature.instance.isPlayerOnHomeTeam(playerZid))
				{
					//Instantiate/Attach home team icon, increase count
					if (playerZid == SlotsPlayer.instance.socialMember.zId)
					{
						occupiedByCurrentPlayer = true;

						if (currentPlayerIcon == null)
						{
							GameObject currentPlayerIconGameObject = NGUITools.AddChild(playerContainer, playerIconPrefabs[QFCMapDialog.QFCBoardPlayerIconType.CURRENT_PLAYER]);
							currentPlayerIcon = currentPlayerIconGameObject.GetComponent<QFCBoardPlayerIconObject>();
						}

						if (playerZid == homeTeamLeaderZid)
						{
							occupiedByCurrentPlayerLeader = true;
						}
					}
					else
					{
						if (playerZid == homeTeamLeaderZid)
						{
							occupiedByHomeTeamLeader = true;
						}
						homeTeamOccupants++;
					}

				}
				else
				{
					awayTeamOccupants++;
					if (playerZid == awayTeamLeaderZid)
					{
						occupiedByAwayTeamLeader = true;
					}
				}
			}

			if (homeTeamOccupants > 0)
			{
				if (homeTeamIcon == null)
				{
					GameObject homeTeamIconObject = NGUITools.AddChild(homeTeamContainer, playerIconPrefabs[QFCMapDialog.QFCBoardPlayerIconType.HOME]);
					homeTeamIcon = homeTeamIconObject.GetComponent<QFCBoardPlayerIconObject>();
				}
			}

			if (awayTeamOccupants > 0)
			{
				if (awayTeamIcon == null)
				{
					GameObject awayTeamIconObject = NGUITools.AddChild(awayTeamContainer, playerIconPrefabs[QFCMapDialog.QFCBoardPlayerIconType.AWAY]);
					awayTeamIcon = awayTeamIconObject.GetComponent<QFCBoardPlayerIconObject>();
				}
			}

			if (isComplete )
			{
				if (occupiedByCurrentPlayerLeader && !string.IsNullOrEmpty(completedAnimationIdleName))
				{
					nodeAnimator.Play(completedAnimationIdleName);
				}
				else if (!string.IsNullOrEmpty(completedAnimationOffName))
				{
					nodeAnimator.Play(completedAnimationOffName);
				}
			}

			updateIcons();
			nodeClickHandler.registerEventDelegate(mapParent.onNodeClicked, Dict.create(D.DATA, nodeData.occupiedBy));
		}

		private static bool isSpriteThemed(string spriteName)
		{
			switch (spriteName)
			{
				case "Logo":
				case "Node Start":
				case "Logo Toaster":
					return true;

				default:
					return false;
			}

		}

		public void setSprite(UIAtlas themedAtlas)
		{
			nodeAtlas = themedAtlas;

			if (nodeSprite != null)
			{
				if (isSpriteThemed(nodeSprite.spriteName))
				{
					nodeSprite.atlas = themedAtlas;
				}

				if (!spriteName.Equals("Node Start")) //Starting node doesn't have a completed state
				{
					nodeSprite.spriteName = spriteName + (isComplete ? " Complete" : " Incomplete");
				}
				else
				{
					nodeSprite.spriteName = spriteName;
				}
			}
		}

		private void updateIcons()
		{
			if (awayTeamIcon != null)
			{
				if (awayTeamOccupants > 1)
				{
					awayTeamIcon.playerCount.gameObject.SetActive(true);
					awayTeamIcon.playerCount.text = awayTeamOccupants.ToString();
				}
				else
				{
					awayTeamIcon.playerCount.gameObject.SetActive(false);
				}


				awayTeamIcon.iconSprite.spriteName = occupiedByAwayTeamLeader ? awayTeamIcon.leaderSpriteName : awayTeamIcon.defaultSpriteName;
			}

			if (homeTeamIcon != null)
			{
				if (homeTeamOccupants > 1)
				{
					homeTeamIcon.playerCount.gameObject.SetActive(true);
					homeTeamIcon.playerCount.text = homeTeamOccupants.ToString();
				}
				else
				{
					homeTeamIcon.playerCount.gameObject.SetActive(false);
				}

				homeTeamIcon.iconSprite.spriteName = occupiedByHomeTeamLeader ? homeTeamIcon.leaderSpriteName : homeTeamIcon.defaultSpriteName;
			}

			if (currentPlayerIcon != null)
			{
				currentPlayerIcon.iconSprite.spriteName = occupiedByCurrentPlayerLeader ? currentPlayerIcon.leaderSpriteName : currentPlayerIcon.defaultSpriteName;
				currentPlayerIcon.playIdleAnimation();
			}
		}
		
		public IEnumerator playCompletedAnimation()
		{
			nodeAnimator.Play(completedAnimationOnName);
			if (coinAnimator != null)
			{
				return CommonAnimation.playAnimAndWait(coinAnimator, coinOutroAnimationName);
			}

			return null;
		}

		public void playCompletedOffAnimation()
		{
			if (!string.IsNullOrEmpty(completedAnimationOffName))
			{
				nodeAnimator.Play(completedAnimationOffName);
			}
		}

		public void playIdleCompletedAnimation()
		{
			if (occupiedByCurrentPlayer && !string.IsNullOrEmpty(completedAnimationIdleName))
			{
				nodeAnimator.Play(completedAnimationIdleName);
			}
			else if (!string.IsNullOrEmpty(completedAnimationOffName))
			{
				nodeAnimator.Play(completedAnimationOffName);
			}

			if (coinAnimator != null)
			{
				coinAnimator.Play(coinOffAnimationName);
			}
		}

		//Animation Event to swap sprites mid-animation
		public void swapToCompletedSprite()
		{
			nodeSprite.spriteName = spriteName + " Complete";
			isComplete = true;
		}

		public void flatten()
		{
			if (coinAnimator != null && !isComplete)
			{
				coinAnimator.Play(coinFlatAnimationName);
			}
		}

		public void unFlatten()
		{
			if (coinAnimator != null && !isComplete)
			{
				coinAnimator.Play(coinIdleAnimationName);
			}
		}
		
		public void resetState()
		{
			isComplete = false;
			nodeAnimator.Play("Idle");
			setSprite(nodeAtlas);
		}
	}
}
