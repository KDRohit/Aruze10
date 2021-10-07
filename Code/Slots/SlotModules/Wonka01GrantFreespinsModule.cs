using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Wonka01GrantFreespinsModule : GrantFreespinsModule 
{
	[SerializeField] private GameObject[] sparkleTrail;
	[SerializeField] private GameObject winBoxEffect;
	[SerializeField] private float winboxEffectWaitTime = 1.0f;
	[SerializeField] private float postGrantWaitTime = 1.0f;

	[SerializeField] private string TRAIL_ANIM_NAME;
	[SerializeField] private float TRAIL_MOVE_TIME = 0.5f;
	[SerializeField] private float SYMBOL_ANIMATION_TIME = 1.5f;

	[SerializeField] private string ADD_SPINS_SYMBOL_ANIM_SOUND_NAME;
	[SerializeField] private string SPARKLE_TRAIL_SOUND_NAME;
	[SerializeField] private string WINBOX_EFFECT_SOUND_NAME;
	[SerializeField] private string BONUS_SYMBOL_NAME = "BN1";

	private bool isSparkleTrailMoving = false;
	
	public override IEnumerator executeOnReelsStoppedCallback()
	{
		int symbolPosition = 0;
		foreach (SlotSymbol symbol in reelGame.engine.getVisibleSymbolsAt(0))
		{	
			if(symbol.serverName == BONUS_SYMBOL_NAME)
			{
				if(sparkleTrail.Length > 0 && sparkleTrail[symbolPosition] != null)
				{
					yield return StartCoroutine(showSparkleTrail(sparkleTrail[symbolPosition], symbol));
					yield return StartCoroutine(grantFreeSpins());
				}
			}
			symbolPosition++;
		}
	}

	private IEnumerator showSparkleTrail(GameObject sparkleTrailObject, SlotSymbol symbol)
	{
		Vector3 initialSparkleTrailPosition = sparkleTrailObject.transform.position;
		Vector3 spinCountPosition = BonusSpinPanel.instance.spinsRemainingLabel.transform.position;

		spinCountPosition = BonusSpinPanel.instance.spinCountLabel.transform.position;

		winBoxEffect.transform.position = new Vector3(spinCountPosition.x, spinCountPosition.y, winBoxEffect.transform.position.z);
		symbol.animateOutcome();
		
		Audio.playSoundMapOrSoundKey(ADD_SPINS_SYMBOL_ANIM_SOUND_NAME);
		yield return new TIWaitForSeconds(SYMBOL_ANIMATION_TIME);

		sparkleTrailObject.gameObject.SetActive(true);
		sparkleTrailObject.gameObject.GetComponent<Animator>().Play(TRAIL_ANIM_NAME);
		isSparkleTrailMoving = true;
		iTween.MoveTo(sparkleTrailObject, iTween.Hash("position", winBoxEffect.transform.position, 
														"time", TRAIL_MOVE_TIME, 
														"easetype", iTween.EaseType.easeOutQuad, 
														"oncompletetarget", this.gameObject, 
														"oncomplete", "onSparkleTrailMoveComplete"));
		Audio.playSoundMapOrSoundKey(SPARKLE_TRAIL_SOUND_NAME);

		while (isSparkleTrailMoving)
		{
			yield return null;
		}

		sparkleTrailObject.gameObject.SetActive(false);
		sparkleTrailObject.transform.position = initialSparkleTrailPosition;
	}

	// Callback function for iTween.MoveTo being complete, so we can ensure that the iTween
	// object is done and removed before we turn the sparkle trail object off
	private void onSparkleTrailMoveComplete()
	{
		isSparkleTrailMoving = false;
	}

	public IEnumerator grantFreeSpins()
	{
		Audio.playSoundMapOrSoundKey(WINBOX_EFFECT_SOUND_NAME);
		if(winBoxEffect != null)
		{
			winBoxEffect.SetActive(true);
			yield return new TIWaitForSeconds(winboxEffectWaitTime);
		}
		incrementFreespinCount();
		if(winBoxEffect != null)
		{
			winBoxEffect.SetActive(false);
		}
		yield return new TIWaitForSeconds(postGrantWaitTime);
	}
}
