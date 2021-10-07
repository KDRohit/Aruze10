using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoyalRushSprintSummary : MonoBehaviour
{
	[SerializeField] private GameObject sparkleTrailObject;

	[Header("No New Best Round")]
	[SerializeField] private GameObject noNewBestRoundObject;
	[SerializeField] private FacebookFriendInfo currentRankPic;
	[SerializeField] private FacebookFriendInfo bestRankPic;
	[SerializeField] private LabelWrapperComponent submittedScoreLabel;
	[SerializeField] private LabelWrapperComponent bestScoreLabel;
	[SerializeField] private LabelWrapperComponent noNewBestRankLabel;

	[Header("New Best Round")]
	[SerializeField] private GameObject newBestRoundObject;
	[SerializeField] private FacebookFriendInfo newBestRoundRankPic;
	[SerializeField] private LabelWrapperComponent newBestRoundScoreLabel;
	[SerializeField] private LabelWrapperComponent newBestRankLabel;

	[Header("New Ruler Round")]
	[SerializeField] private GameObject newRoyalRulerObject;
	[SerializeField] private FacebookFriendInfo newRulerPic;
	[SerializeField] private LabelWrapperComponent newRulerScoreLabel;

	public const string NO_NEW_BEST_STATE = "no_new_best";
	public const string NEW_BEST_STATE = "new_best";
	public const string NEW_RULER_STATE = "new_ruler";

	private const float NO_NEW_BEST_ANIM_LENGTH = 6.0f;
	private const float NEW_BEST_SPARKLE_DELAY = 5.5f;
	private const float NEW_RULER_SPARKLE_DELAY = 5.5f;
	private const float SPARKLE_ANIM_LENGTH = 1.0f;

	//Sounds
	private const string TIME_UP_NO_NEW_BEST_SOUND = "TimeUpNoBestRRush01";
	private const string TIME_UP_NEW_BEST_SOUND = "TimeUpBannerNewBestRRush01";
	private const string TIME_UP_NEW_LEADER_SOUND = "TimerUpNewLeaderRRush01";
	private const string TIME_UP_BANNER_EXITS_SOUND = "TimeUpBannerExitsRRush01";
	private const string SPARKLE_FLOURISH_SOUND = "UpdateLeaderBoardRRush01";
	private const string SPARKLE_FLOURISH_END_SOUND = "UpdateLeaderBoardPt2RRush01";

	private const float Y_OFFSET_PER_ENTRY = -165.0f;

	public IEnumerator playIntroAnimations(string state, float delayTime, RoyalRushInfo infoToUse, int entryIndex)
	{
		Vector3 targetPosition = sparkleTrailObject.transform.localPosition + new Vector3(0, entryIndex * Y_OFFSET_PER_ENTRY, 0);
		switch(state)
		{
			case NO_NEW_BEST_STATE:
				currentRankPic.member = SlotsPlayer.instance.socialMember;
				bestRankPic.member = SlotsPlayer.instance.socialMember;
				submittedScoreLabel.text = CreditsEconomy.convertCredits(infoToUse.lastRoundFinalScore);
				bestScoreLabel.text = CreditsEconomy.convertCredits(infoToUse.highScore);
				noNewBestRankLabel.text = (infoToUse.competitionRank+1).ToString();
				Audio.play(TIME_UP_NO_NEW_BEST_SOUND);
				noNewBestRoundObject.SetActive(true);
				yield return new WaitForSeconds(NO_NEW_BEST_ANIM_LENGTH);
				break;

			case NEW_BEST_STATE:
				newBestRoundRankPic.member = SlotsPlayer.instance.socialMember;
				newBestRoundScoreLabel.text = CreditsEconomy.convertCredits(infoToUse.lastRoundFinalScore);
				newBestRankLabel.text = (infoToUse.competitionRank+1).ToString();
				Audio.play(TIME_UP_NEW_BEST_SOUND);
				newBestRoundObject.SetActive(true);
				yield return new WaitForSeconds(NEW_BEST_SPARKLE_DELAY);
				Audio.play(TIME_UP_BANNER_EXITS_SOUND);
				Audio.play(SPARKLE_FLOURISH_SOUND);
				sparkleTrailObject.SetActive(true);
				iTween.MoveTo(sparkleTrailObject, iTween.Hash("position", targetPosition, "time", 0.25f, "islocal", true, "easetype", iTween.EaseType.linear)); //start tweening the sparkle
				yield return new WaitForSeconds(SPARKLE_ANIM_LENGTH);
				Audio.play(SPARKLE_FLOURISH_END_SOUND);
				break;

			case NEW_RULER_STATE:
				newRulerPic.member = SlotsPlayer.instance.socialMember;
				newRulerScoreLabel.text = CreditsEconomy.convertCredits(infoToUse.lastRoundFinalScore);
				Audio.play(TIME_UP_NEW_LEADER_SOUND);
				newRoyalRulerObject.SetActive(true);
				yield return new WaitForSeconds(NEW_RULER_SPARKLE_DELAY);
				Audio.play(TIME_UP_BANNER_EXITS_SOUND);
				Audio.play(SPARKLE_FLOURISH_SOUND);
				sparkleTrailObject.SetActive(true);
				iTween.MoveTo(sparkleTrailObject, iTween.Hash("position", targetPosition, "time", 0.25f, "islocal", true, "easetype", iTween.EaseType.linear)); //start tweening the sparkle
				yield return new WaitForSeconds(SPARKLE_ANIM_LENGTH);
				Audio.play(SPARKLE_FLOURISH_END_SOUND);
				break;
		}
	}
}
