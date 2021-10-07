using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;


namespace QuestForTheChest
{
	/**
		Parent class for anything that overlays on top of the QFC Map Dialog
		Covers the core common functionality of loading the background and setting up labels
	**/
	public class QFCMapDialogOverlay : MonoBehaviour
	{
		[SerializeField] private Renderer background;
		[SerializeField] private MultiLabelWrapperComponent headerLabels;
		[SerializeField] private MultiLabelWrapperComponent bodyLabels;
		[SerializeField] private GameObject keyRewardTemplate;
		[SerializeField] private GameObject coinRewardTemplate;
		[SerializeField] private GameObject startInfoTemplate;
		[SerializeField] protected Transform rewardParent;
		[SerializeField] private UISprite[] shroudSprites;

		protected QFCBoardNode boardNode;

		protected void setUpDynamicNodeAssets(int nodeIndex)
		{
			if (hasValidBoardData(nodeIndex))
		    {
			    boardNode = QuestForTheChestFeature.instance.nodeData[nodeIndex];
		    }

			if (boardNode != null)
			{
				string headerText = Localize.textTitle(boardNode.storyLocalizationHeader);

				if (headerLabels != null)
				{
					headerLabels.text = headerText;
				}


				if (bodyLabels != null)
				{
					string bodyText = Localize.text(boardNode.storyLocalizationBody);
					bodyLabels.text = bodyText;
				}
			}
			else
			{
				if (headerLabels != null)
				{
					headerLabels.gameObject.SetActive(false);
				}


				if (bodyLabels != null)
				{
					bodyLabels.gameObject.SetActive(false);
				}
			}

			if (background != null)
			{
				string backGroundPath = boardNode != null ? boardNode.backgroundTexturePath : string.Format(QFCBoardNode.BACKGROUND_TEXTURE_PATH, ExperimentWrapper.QuestForTheChest.theme, 0); //Just default to the first BG image if we don't have valid board data
				AssetBundleManager.load(this, string.Format(backGroundPath, ExperimentWrapper.QuestForTheChest.theme, nodeIndex.ToString()), backgroundLoadSuccess, assetLoadFailed, isSkippingMapping:true, fileExtension:".png");
			}

			if (shroudSprites != null)
			{
				for (int i = 0; i < shroudSprites.Length; i++)
				{
					shroudSprites[i].color = QuestForTheChestFeature.instance.rewardShroudColor;
				}
			}
		}

		private bool hasValidBoardData(int nodeIndex)
		{
			return nodeIndex >= 0 &&
			       QuestForTheChestFeature.instance != null &&
			       QuestForTheChestFeature.instance.isEnabled &&
			       QuestForTheChestFeature.instance.nodeData != null &&
			       QuestForTheChestFeature.instance.nodeData.Count > nodeIndex;
		}
		
		private void backgroundLoadSuccess(string assetPath, Object obj, Dict data = null)
		{
			background.material.mainTexture = obj as Texture2D;
			background.gameObject.SetActive(true);
		}
		
		private void assetLoadFailed(string assetPath, Dict data = null)
		{
			Bugsnag.LeaveBreadcrumb("QFC Themed Asset failed to load: " + assetPath);
#if UNITY_EDITOR
			Debug.LogWarning("QFC Themed Asset failed to load: " + assetPath);			
#endif
		}

		protected void attachStartInfo()
		{
			QFCContainerItemIntro item = NGUITools.AddChild(rewardParent, startInfoTemplate).GetComponent<QFCContainerItemIntro>();
			if (item != null)
			{
				item.init(Localize.text("qfc_spin_instruction"), 0);
			}
		}
		
		protected QFCContainerItem attachReward(QFCReward reward)
		{
			QFCContainerItem item = null;
			switch (reward.type)
			{
				case "coin":
					item = NGUITools.AddChild(rewardParent, coinRewardTemplate).GetComponent<QFCContainerItem>();
					if (item != null)
					{
						item.init(CreditsEconomy.convertCredits(reward.value));
					}

					break;

				case "key":
					item = NGUITools.AddChild(rewardParent, coinRewardTemplate).GetComponent<QFCContainerItem>();
					if (item != null)
					{
						item.init(reward.value.ToString());
					}

					break;

				case "token":
					item = NGUITools.AddChild(rewardParent, keyRewardTemplate).GetComponent<QFCContainerItem>();
					QFCRewardItemKeys keyItem = item as QFCRewardItemKeys;
					if (keyItem != null)
					{ 
						keyItem.init((int)reward.value);
					}
					break;

				default:
					Userflows.addExtraFieldToFlow(Dialog.instance.currentDialog.userflowKey, "invalid_qfc_reward", reward.type);
					Bugsnag.LeaveBreadcrumb("Invalid QFC reward type");
					break;
			}

			return item;
		}

		protected QFCContainerItem attachCoinReward(long creditsAmount)
		{
			QFCContainerItem item = NGUITools.AddChild(rewardParent, coinRewardTemplate).GetComponent<QFCContainerItem>();
			if (item != null)
			{
				item.init(CreditsEconomy.convertCredits(creditsAmount));
				return item;
			}

			return null;
		}
		
		protected QFCRewardItemKeys attachKeysReward(int keysAmount)
		{
			QFCRewardItemKeys item = NGUITools.AddChild(rewardParent, keyRewardTemplate).GetComponent<QFCRewardItemKeys>();
			if (item != null)
			{
				item.init(keysAmount);
				return item;
			}

			return null;
		}
	}
}
