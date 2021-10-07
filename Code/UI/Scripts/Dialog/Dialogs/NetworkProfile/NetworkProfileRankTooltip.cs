using UnityEngine;
using System.Collections;
using System.Collections.Generic;

class NetworkProfileRankTooltip : MonoBehaviour
{
    [SerializeField] private Animator animator;
	[SerializeField] private ImageButtonHandler okayButton;
	[SerializeField] private ClickHandler clickableShroud;
	[SerializeField] private GameObject rankIconPrefab;
	[SerializeField] private GameObject	tmProMaskCenter;
	[SerializeField] private UIGrid rankIconGrid;
	[SerializeField] private AchievementRankIcon currentRankIcon;
	[SerializeField] private SlideController slideController;
	[SerializeField] private GameObject tmProMaskSizer;
	[SerializeField] private TextMeshProMasker textMeshProMasker;
    private const string ANIMATION_NAME = "on";
	public bool isActive
	{
		get
		{
			return gameObject.activeSelf;
		}
	}

	private bool isSetup = false;
	
	public void show(SocialMember member)
	{
		if (!isSetup)
		{
			setup(member);
		}

		if (isSetup)
		{
			gameObject.SetActive(true);
			slideController.enabled = true;
			animator.Play(ANIMATION_NAME);	
		}
	}
	
	public void hide()
	{
		if (FTUEManager.Instance.Go) 
		{
			Destroy (FTUEManager.Instance.Go);
		}
		gameObject.SetActive(false);
		slideController.enabled = false;
	}

	private void okayClicked(Dict args = null)
	{
		hide();
	}

    private void setup(SocialMember member)
	{
		//in case we're being destroyed
		if (this.gameObject == null || !NetworkAchievements.isEnabled)
		{
			return;
		}

		// Setup the rank icons here as well.
		AchievementLevel level;
		GameObject newIcon;
		AchievementRankIcon rankIcon;
		clickableShroud.registerEventDelegate(okayClicked);
		currentRankIcon.setRank(member);
		foreach (KeyValuePair<int, AchievementLevel> pair in AchievementLevel.allLevels)
		{
			level = pair.Value;
			newIcon = CommonGameObject.instantiate(rankIconPrefab, rankIconGrid.transform) as GameObject;
			if (newIcon == null)
			{
				Debug.LogErrorFormat("NetworkProfileRankTooltip.cs -- setup -- failed to instantiate a gameobject for the rank icon.");
				continue;
			}
			newIcon.name = "Icon " + level.rank;
		    rankIcon = newIcon.GetComponent<AchievementRankIcon>();
			if (rankIcon == null)
			{
				Debug.LogErrorFormat("NetworkProfileRankTooltip.cs -- setup -- failed to get a rankIcon from the created object: {0}", newIcon.name);
				continue;
			}
			rankIcon.setRank(level, level.requiredScore);
			rankIcon.addTextToMasker(textMeshProMasker);
		}
		rankIconGrid.Reposition();
		okayButton.registerEventDelegate(okayClicked);

		// Use absolute becuase we have a negative cell height to make the grid go upwards.
		if (slideController.content != null)
		{
			slideController.content.height = Mathf.Abs(AchievementLevel.allLevels.Count * rankIconGrid.cellHeight);
		
			float topBound = slideController.content.height + Mathf.Abs(rankIconGrid.cellHeight);
			// 3 is the number of visible panels in the container so thats why we subtract that many heights.
			float bottomBound = (AchievementLevel.allLevels.Count * rankIconGrid.cellHeight) + (Mathf.Abs(rankIconGrid.cellHeight) * 3.5f);

			slideController.setBounds(topBound, bottomBound);
			CommonTransform.addY(slideController.content.transform, rankIconGrid.cellHeight * member.achievementRank.rank);

			//set setup true if we have actual content; don't show empty data
			isSetup = true;
		}

	}	
	
}