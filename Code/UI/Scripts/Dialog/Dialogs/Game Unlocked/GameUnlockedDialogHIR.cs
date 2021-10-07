using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/**
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
**/

public class GameUnlockedDialogHIR : GameUnlockedDialog
{
	public GameObject lockTop;
	public UISprite starburst1;
	public UISprite starburst2;
	public UISprite lockBurst1;
	public UISprite lockBurst2;

	public override void Update()
	{
		base.Update();
		// Spin the starbursts.
		float rotate = 5f * Time.deltaTime;
		if (starburst1 != null && starburst2 != null)
		{
			starburst1.transform.Rotate(0, 0, rotate);
			starburst2.transform.Rotate(0, 0, -rotate);
		}
		if (lockBurst1 != null && lockBurst2 != null)
		{
			lockBurst1.transform.Rotate(0, 0, rotate);
			lockBurst2.transform.Rotate(0, 0, -rotate);
		}
	}
	
	// Do all the fancy animation on the dialog.
	protected override IEnumerator animateDialog()
	{
		for (int i = 0; i < NUMBER_OF_FIREWORKS; i++)
		{
			Audio.play(FIREWORK_SOUND, 1.0f, 0.0f, FIREWORK_DELAY * i);
		}
		// Start the part 1 audio
		Audio.play(PART1);
		float duration = .75f;
		float age = 0f;
		// We will need to keep track of some of the sounds in here so we can manually stop them.
		PlayingAudio soundEffect = null;
		
		// Bouncey-scale in the unlocked game panel.
		CommonGameObject.setLayerRecursively(unlockedParent, Layers.ID_NGUI);
		iTween.ScaleTo(unlockedParent, iTween.Hash("scale", Vector3.one, "time", duration, "easetype", iTween.EaseType.easeOutElastic));
		yield return new WaitForSeconds(duration);
		closeButton.SetActive(true);

		// Jiggle the lock
		age = 0;
		duration = .5f;
		soundEffect = Audio.play(LOCK_JIGGLE, 1.0f, 0.0f, 0.0f, float.PositiveInfinity);
		float originalX = unlockedLockParent.transform.localPosition.x;
		while (age < duration)
		{
			CommonTransform.setX(unlockedLockParent.transform, CommonEffects.pulsateBetween(originalX - 8f, originalX + 8f, 30f));
			age += Time.deltaTime;
			yield return null;
		}
		// soundEffect will be null if sound is disabled.
		if (soundEffect != null)
		{
			soundEffect.stop(0);
			soundEffect = null;
		}

		// Re-center the lock box after the jiggle.
		CommonTransform.setX(unlockedLockParent.transform, originalX);
		
		// Squish the lock down and back real quick.
		yield return StartCoroutine(CommonEffects.throb(unlockedLockParent, new Vector3(1f, .8f, 0f), .25f));

		// Open the lock top.
		duration = .25f;
		Audio.play(LOCK_OPEN);
		iTween.RotateTo(lockTop, iTween.Hash("z", 20, "time", duration, "islocal", true, "easetype", iTween.EaseType.linear));
		yield return new WaitForSeconds(duration);
		
		// A little pause before fading the unlocked frame.
		yield return new WaitForSeconds(.5f);
		
		// Show the UNLOCKED LABEL above the unlocked game frame.
		unlockedLabel.SetActive(true);

		// Fade the newly unlocked frame as it grows.
		age = 0;
		duration = .25f;
		while (age < duration)
		{
			age += Time.deltaTime;
			float normalized = Mathf.Clamp01(age / duration);
			unlockedFrameParent.transform.localScale = Vector3.one * (1f + normalized * .5f);
			NGUIExt.fadeGameObject(unlockedFrameParent, 1f - normalized);
			currentLevelLabel.color = CommonColor.adjustAlpha(currentLevelLabel.color, 1f - normalized); // TextMeshPro isn't faded by NGUIExt since it isnt a UIWidget
			yield return null;
		}
		
		if (nextUnlockGame != null)
		{
			// A little pause before showing the next unlocked game.
			yield return new WaitForSeconds(.5f);

			// Move the unlocked game over to make room.
			duration = .5f;
			iTween.MoveTo(unlockedParent, iTween.Hash("x", -nextUnlockParent.transform.localPosition.x, "time", duration, "islocal", true, "easetype", iTween.EaseType.easeOutQuad));
			yield return new WaitForSeconds(duration);
			
			Audio.play(PART2);
			// Bouncey-scale in the next unlock game panel.
			duration = .75f;
			CommonGameObject.setLayerRecursively(nextUnlockParent, Layers.ID_NGUI);
			iTween.ScaleTo(nextUnlockParent, iTween.Hash("scale", Vector3.one, "time", duration, "easetype", iTween.EaseType.easeOutElastic));
			yield return new WaitForSeconds(duration);

			// Move/scale in the unlocked lock.
			Vector3 pos = nextUnlockedLockParent.transform.localPosition;
			nextUnlockedLockParent.transform.localPosition = pos + new Vector3(300, 50, 0);
			nextUnlockedLockParent.transform.localScale = Vector3.one * 4f;
			CommonGameObject.setLayerRecursively(nextUnlockedLockParent, Layers.ID_NGUI);
			duration = .5f;
			Audio.play(LOCK_FALL);
			iTween.MoveTo(nextUnlockedLockParent, iTween.Hash("position", pos, "time", duration, "islocal", true, "easetype", iTween.EaseType.easeOutBounce));
			iTween.ScaleTo(nextUnlockedLockParent, iTween.Hash("scale", Vector3.one, "time", duration, "easetype", iTween.EaseType.easeOutBounce));
			yield return new WaitForSeconds(duration * .3f);
			// Play the land sound part way through the tween, since the tween bounces
			// and we want it to look like it plays during the initial hit.
			Audio.play(LOCK_LAND);
			yield return new WaitForSeconds(duration * .7f);
		}
	
		shareButton.SetActive(true);
		inputParent.SetActive(SlotsPlayer.isFacebookUser);
	}
		
	// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup.
		base.close();
	}
}