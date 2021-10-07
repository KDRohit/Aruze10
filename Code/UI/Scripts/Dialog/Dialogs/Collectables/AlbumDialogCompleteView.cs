using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AlbumDialogCompleteView : MonoBehaviour 
{
	[SerializeField] private ButtonHandler collectButton;
	[SerializeField] private LabelWrapperComponent rewardAmountLabel;
	[SerializeField] private LabelWrapperComponent albumNameLabel;
	[SerializeField] private AnimationListController.AnimationInformationList introAnimationsList = new AnimationListController.AnimationInformationList();
	[SerializeField] private AnimationListController.AnimationInformationList rewardsRevealAnimationsList = new AnimationListController.AnimationInformationList();
	[SerializeField] private Animator coinCollectAnimator;

	private UISprite shroud;

	private long rewardValue = 0;

	private const float SHROUD_MAX_ALPHA = 0.75f;
	private const float SHROUD_ALPHA_TWEEN_TIME = 1.0f;

	private const float AMBIENT_ANIM_LENGTH = 1.0f;

	public void init(long rewardAmount, GameObject normalViewParent, UISprite shroud)
	{
		this.shroud = shroud;
		rewardValue = rewardAmount;
		StatsManager.Instance.LogCount(counterName:"dialog",
			kingdom: "hir_collection",
			phylum: "collection_complete",
			klass: Collectables.currentAlbum,
			genus: "view",
			val:rewardValue * CreditsEconomy.economyMultiplier);
		SlotsPlayer.addCredits(rewardAmount, "collectables album complete");
		collectButton.registerEventDelegate(onClickCloseCardView);
		rewardAmountLabel.text = CreditsEconomy.convertCredits(rewardAmount);
		albumNameLabel.text = string.Format("You've completed the {0} Collection!", Localize.text("collections_" + Collectables.currentAlbum));
		StartCoroutine(playRewardAnimations(rewardAmount, normalViewParent));
	}

	private IEnumerator playRewardAnimations(long rewardAmount, GameObject normalViewParent)
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(introAnimationsList));
		normalViewParent.SetActive(true); //See normal dialog behind now
		yield return new WaitForSeconds(AMBIENT_ANIM_LENGTH); //Let the lights animate on their own before we bring in the other elements

		iTween.ValueTo(gameObject, iTween.Hash(
			"from", shroud.alpha,
			"to", SHROUD_MAX_ALPHA,
			"time", SHROUD_ALPHA_TWEEN_TIME,
			"easetype", iTween.EaseType.linear,
			"onupdate", "setAlpha"
		));

		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(rewardsRevealAnimationsList));
	}

	private void onClickCloseCardView(Dict args = null)
	{
		StatsManager.Instance.LogCount(counterName:"dialog",
			kingdom: "hir_collection",
			phylum: "collection_complete",
			klass: Collectables.currentAlbum,
			family: "collect",
			genus: "click",
			val:rewardValue * CreditsEconomy.economyMultiplier);
		
		Audio.play("ButtonCompleteCollections");
		StartCoroutine(playRollupAnimations());
	}

	private IEnumerator playRollupAnimations()
	{
		coinCollectAnimator.Play("collect");
		yield return new WaitForSeconds(1.5f);
		Overlay.instance.top.updateCredits(false); //Rollup the top overlay to the new player amount
		yield return new WaitForSeconds(1.0f);
		shroud.gameObject.SetActive(false);
		Destroy(gameObject);
	}

	// iTween callback.
	public void setAlpha(float alpha)
	{
		shroud.alpha = alpha;
	}
}