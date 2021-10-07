using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.IO;

public class CollectableCardAnimated : MonoBehaviour 
{
	[SerializeField] private Animator cardAnimator;
	[SerializeField] private CollectableCard cardTemplate;
	[SerializeField] private GameObject cardCloneParent;

	[System.NonSerialized] public CollectableCard loadedCard = null;

	private const float CARD_INTRO_ANIM_WAIT = 0.5f;
	private const float CARD_TWEEN_TIME = 0.5f;

	public CollectableCard createAnimatedCard()
	{
		GameObject newCard = NGUITools.AddChild(cardCloneParent, cardTemplate.gameObject);
		CollectableCard newCardHandle = newCard.GetComponent<CollectableCard>();
		loadedCard = newCardHandle;
		loadedCard.onClickButton.enabled = false;
		newCardHandle.cardAnimator = this.cardAnimator;
		return newCardHandle;
	}

	public IEnumerator animateAndTweenCard(string animName, int xOffset, Transform tweenTarget, GameObjectCacher starCache, GameObjectCacher bustCache, CollectionsDuplicateMeter starMeter, int finalStarCount)
	{
		Vector3 leftTweenPos = new Vector3(tweenTarget.localPosition.x, tweenTarget.localPosition.y, this.gameObject.transform.localPosition.z);
		Vector3 tweenTargetPos = new Vector3(tweenTarget.localPosition.x + xOffset, tweenTarget.localPosition.y, this.gameObject.transform.localPosition.z);

		cardAnimator.Play(animName);
		Audio.play("CardsTravelCollections");
		yield return new WaitForSeconds(CARD_INTRO_ANIM_WAIT);

		//Go all the way to the left first
		iTween.MoveTo(this.gameObject, iTween.Hash("position", leftTweenPos, "islocal", true, "time", CARD_TWEEN_TIME, "easetype", iTween.EaseType.linear));
		yield return new WaitForSeconds(CARD_TWEEN_TIME);

		//Now tween to the right to the final resting spot
		iTween.MoveTo(this.gameObject, iTween.Hash("position", tweenTargetPos, "islocal", true, "time", CARD_TWEEN_TIME, "easetype", iTween.EaseType.linear));

		//Now animate and tween any duplicate stars we have
		//If the newBadge isn't on then we know its a duplicate
		//Can't just rely on the isNew flag because a card can be a duplicate but new still if the player hasn't marked it as seen
		if (!loadedCard.newBadgeParent.activeSelf)
		{
			yield return new WaitForSeconds(CARD_TWEEN_TIME); //Wait for the tween to finish
			yield return StartCoroutine(loadedCard.startStarCollection(starCache, bustCache, starMeter, starMeter.starParent, finalStarCount));
		}

		loadedCard.onClickButton.enabled = true;
	}
}
