using UnityEngine;
using System.Collections.Generic;

namespace QuestForTheChest
{
	public class QFCBoardNode
	{
		public string storyLocalizationHeader { get; private set; }
		public string storyLocalizationBody { get; private set; }
		public string backgroundTexturePath { get; private set; }
		public float rewardMultiplier;
		public Vector2 position;
		public QFCBoardNode nextNode;
		public List<string> occupiedBy;

		private const string STORY_HEADER_LOCALIZATION_KEY = "qfc_story_{0}_header_{1}";
		private const string STORY_BODY_LOCALIZATION_KEY = "qfc_story_{0}_body_{1}";
		public const string BACKGROUND_TEXTURE_PATH = "Features/Quest for the Chest/Themed Assets/{0}/Textures/Dialog {1} {0}";

		public QFCBoardNode()
		{
			storyLocalizationHeader = "";
			storyLocalizationBody = "";
			backgroundTexturePath = "";
			rewardMultiplier = 0;
			position = Vector2.zero;
			nextNode = null;
			occupiedBy = new List<string>();
		}

		public QFCBoardNode(int storyIndex, float reward)
		{
			if (storyIndex >= 0)
			{
				storyLocalizationHeader = string.Format(STORY_HEADER_LOCALIZATION_KEY, ExperimentWrapper.QuestForTheChest.theme, storyIndex);
				storyLocalizationBody = string.Format(STORY_BODY_LOCALIZATION_KEY, ExperimentWrapper.QuestForTheChest.theme, storyIndex);
				backgroundTexturePath = string.Format(BACKGROUND_TEXTURE_PATH, ExperimentWrapper.QuestForTheChest.theme, storyIndex);
			}
			else
			{
				storyLocalizationHeader = string.Empty;
				storyLocalizationBody = string.Empty;
				backgroundTexturePath = string.Empty;
			}

			rewardMultiplier = reward;
			position = Vector2.zero;
			occupiedBy = new List<string>();
		}
	}
}

