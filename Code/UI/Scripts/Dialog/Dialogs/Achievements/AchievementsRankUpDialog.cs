using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class AchievementsRankUpDialog : DialogBase
{
	[SerializeField] private MeshRenderer profileRenderer;
	[SerializeField] private Animator animator;
	[SerializeField] private ImageButtonHandler okayButton;
	[SerializeField] private AchievementRankIcon oldRankIcon;
	[SerializeField] private AchievementRankIcon newRankIcon;
	[SerializeField] private TextMeshPro shadowLabel;

	private const string RANK_SPRITE_FORMAT = "Rank Icon {0}";
	private bool shouldClose = false;
	private AchievementLevel newAchievementLevel; // Needed for stats.

	private const string TIER_UP_AUDIO = "TrophyTierLevelUpNetworkAchievements";
	private const string BUTTON_AUDIO = "SendMail";

	private const string INTRO_ANIMATION = "Trophy MOTD Rank Up ani on";
	private const string LOOP_ANIMATION = "Trophy MOTD Rank Up ani Loop";
	private const string EMPTY = "Empty";

	public override void init()
	{
		if (!downloadedTextureToRenderer(profileRenderer, 0))
		{
			// As a bakcup lets call this if it failed earlier.
			StartCoroutine(SlotsPlayer.instance.socialMember.setPicOnRenderer(profileRenderer, true));
		}

		AchievementLevel newLevel = (AchievementLevel)dialogArgs.getWithDefault(D.RANK, null);
		// The old level is the new one minus one. We already check in ShowDialog if this is valid.
		AchievementLevel oldLevel = AchievementLevel.getLevel(newLevel.rank - 1);
		newAchievementLevel = newLevel;
		if (newLevel == null || oldLevel == null)
		{
			Debug.LogErrorFormat("AchievementsRankUpDialog.cs -- init -- a null AchievementLevel was used here, auto-closing");
			shouldClose = true;
			return;
		}
		oldRankIcon.setRank(oldLevel);
		newRankIcon.setRank(newLevel);
		okayButton.registerEventDelegate(okayClicked);
		Audio.play(TIER_UP_AUDIO);

		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "ll_profile",
			phylum: "trophy_rank_up",
			klass: "view",
			family: newLevel.name);
		animator.Play(INTRO_ANIMATION);
	}

	protected override void onShow()
	{
		// There are loads of particles that dont get hidden by hide() so just use the gameObject to show/hide them.
		gameObject.SetActive(true);
		animator.Play(INTRO_ANIMATION);
	}

	protected override void onHide()
	{
		animator.Play(EMPTY);
		// There are loads of particles that dont get hidden by hide() so just use the gameObject to show/hide them.
		gameObject.SetActive(false);
	}

	protected override void onFadeInComplete()
	{
		base.onFadeInComplete();
		if (shouldClose)
		{
			Dialog.close();
		}
	}

	public override void close()
	{
		// Do cleanup here.
	}

	private void okayClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "ll_profile",
			phylum: "trophy_rank_up",
			klass: "close",
			family: newAchievementLevel.name);
		Audio.play(BUTTON_AUDIO);
		Dialog.close();
	}

	public static void showDialog(AchievementLevel rank)
	{
		if (rank.rank <= 0)
		{
			Debug.LogErrorFormat("AchievementsRankUpDialog.cs -- showDialog -- Trying to level up the player to the first level, this is not possible and we won't show this dialog.");
			return;
		}

		Dialog.instance.showDialogAfterDownloadingTextures("achievement_rank_up",
			SlotsPlayer.instance.socialMember.getImageURL,
			Dict.create(D.RANK, rank),
			isPersistent: true);
	}
}