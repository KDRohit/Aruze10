using UnityEngine;
using System.Collections;

//This class is for the GMG01 freespins and handles the tweening of the frame and top bar feature
public class Gmg01FreespinsIntroTweenModule : SlotModule
{
	[SerializeField] private ReelGameBackground background;
	[SerializeField] private Animator whiteoutAnimator;
	[SerializeField] private float timeBeforeWhiteoutFade = 0.25f;
	[SerializeField] private float timeBetweenFadeAndBGTween = 0.6f;
	[SerializeField] private GameObject reelParentObject;
	[SerializeField] private GameObject backgroundObject;
	[SerializeField] private Vector3 backgroundStartPosition;
	[SerializeField] private float backgroundTweenTime = 0.25f;
	[SerializeField] private GameObject topBarObject;
	[SerializeField] private float topBarTweenTime = 0.25f;
	private float POST_TWEEN_DELAY = 1.0f;
	private const string WHITEOUT_IDLE_STATE = "White";
	private const string WHITEOUT_FADE_STATE = "Fade";

	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		backgroundObject.transform.localPosition = backgroundStartPosition;
		background.background.gameObject.SetActive(true);
		background.mask.gameObject.SetActive(true);
		reelParentObject.SetActive(true);
		return true;
	}

	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{

		iTween.MoveTo(backgroundObject, 
			iTween.Hash("position", Vector3.zero, 
				"time", backgroundTweenTime, 
				"islocal", true, 
				"easetype", iTween.EaseType.linear));

		yield return new TIWaitForSeconds(backgroundTweenTime);

		iTween.MoveTo(topBarObject, 
			iTween.Hash("position", Vector3.zero, 
				"time", topBarTweenTime, 
				"islocal", true, 
				"easetype", iTween.EaseType.linear));

		yield return new TIWaitForSeconds(topBarTweenTime + POST_TWEEN_DELAY);
	}
}
