using UnityEngine;
using UnityEngine.Profiling;
using System.Collections;
using System.Collections.Generic;

/**
Lives on a symbol prefab instance, acting as an anchor point for various 3d visual effects.
*/
public class SymbolAnimator3d : SymbolAnimator
{

	public AnimationClip  anticipationAnimation3d;
	public Animation  anticipationAnimation3dObject;
	public AnimationClip  outcomeAnimation3d;
	public Animation  outcomeAnimation3dObject;
	public AnimationClip  mutateFromAnimation3d;
	public Animation  mutateFromAnimation3dObject;
	public AnimationClip  mutateToAnimation3d;
	public Animation  mutateToAnimation3dObject;
	public Collider paytableSizer = null;			// Box Collider that specifies a square 2D region that encompasses the symbol


	public static float landScaleAmountX = .7f;
	public static float landScaleAmountY = 1.3f;
	public static float landScaleAmountZ = 1.3f;
	public static float landScaleTime1 = .1f;

	public static float landScaleAmount2X = 1.3f;
	public static float landScaleAmount2Y = .7f;
	public static float landScaleAmount2Z = .7f;
	public static float landScaleTime2 = .2f;

	public static float landScaleTime3 = .15f;
	public static iTween.EaseType landEaseType1 = iTween.EaseType.easeInSine;
	public static iTween.EaseType landEaseType2 = iTween.EaseType.easeInSine;
	public static iTween.EaseType landEaseType3 = iTween.EaseType.easeInSine;
	
	public void Start()
	{
		setupSpecialRenderQueue();
	}

	public override void activate(bool isFlattened = false)
	{
		Profiler.BeginSample("SymAnim3D.activate");

		// Cancel all pending invoke calls just to be safe
		CancelInvoke();
		
		setIsSymbolActive( true );
		isWildShowing = false;
		isMutating = false;
		gameObject.SetActive(true);
		
		staticRenderer.enabled = false;
		if (skinnedRenderer != null)
		{
			skinnedRenderer.enabled = false;
		}
		
		// Make sure we're on the default reel layer
		CommonGameObject.setLayerRecursively(gameObject, Layers.ID_SLOT_REELS, Layers.ID_NGUI_PERSPECTIVE);
		
		if (!isAnimationAndRenderingDataCached)
		{
			cacheAnimationAndRenderingComponents();
			isAnimationAndRenderingDataCached = true;
		}

		Profiler.EndSample();
	}

	/// Turns off a symbol (deactivates it), which may include some special cleanup/reset code.
	/// Override this for special symbol prefab types.
	public override void deactivate()
	{
		Profiler.BeginSample("SymAnim3D.deactivate");

		// Make sure the symbol isn't animating from a prior life
		if (isAnimating)
		{
			stopAnimation();
		}
		transform.localScale = info.scaling;
		gameObject.SetActive(false);
		setIsSymbolActive( false );

		Profiler.EndSample();
	}

	public override void stopAnimation(bool force = false)
	{
		// Stop the animation from playing.
		return;
	}

	// do a squish squash scale tween on symbols. used in zynga01 when the symbols land.
	public IEnumerator doSquashAndSquish()
	{
		if (gameObject != null && gameObject.transform != null && gameObject.transform.Find("ScalePivot") != null)
		{
			GameObject scalePivot = gameObject.transform.Find("ScalePivot").gameObject;
			Vector3 originalScale = scalePivot.transform.localScale;
			iTween.ScaleTo (scalePivot, iTween.Hash("scale", new Vector3(originalScale.x * landScaleAmountX, originalScale.y * landScaleAmountY, originalScale.z * landScaleAmountZ), "time", landScaleTime1, "easetype", landEaseType1));
			yield return new TIWaitForSeconds(landScaleTime1);
			iTween.ScaleTo (scalePivot, iTween.Hash("scale", new Vector3(originalScale.x * landScaleAmount2X, originalScale.y * landScaleAmount2Y, originalScale.z * landScaleAmount2Z), "time", landScaleTime2, "easetype", landEaseType2));
			yield return new TIWaitForSeconds(landScaleTime2);
			iTween.ScaleTo (scalePivot, iTween.Hash("scale", originalScale, "time", landScaleTime3, "easetype", landEaseType3));
			yield return new TIWaitForSeconds(landScaleTime3);
		}
	}

	// Tweens this symbol from it's starting offset to Vector3.zero over the specified time.
	public void plopSymbol(float time)
	{
		iTween.MoveTo(gameObject, iTween.Hash("position", Vector3.zero, "time", time, "islocal", true));
	}

	/// Plays the anticipation animation sequence, based on a SymbolAnimationType.
	public override void playAnticipation(SlotSymbol targetSymbol)
	{
		setSymbol(targetSymbol);
		if (anticipationAnimation3d == null || anticipationAnimation3dObject == null)
		{
			Debug.LogWarning("Trying to play Anticipation Animation that isn't defined.");
			return;
		}
		anticipationAnimation3dObject.Play(anticipationAnimation3d.name);
	}

	/// Plays the outcome animation sequence, based on a SymbolAnimationType.
	/// Override this for special symbol prefab types.
	public override void playOutcome(SlotSymbol targetSymbol)
	{
		setSymbol(targetSymbol);
		if (outcomeAnimation3d == null || outcomeAnimation3dObject == null)
		{
			Debug.LogWarning("Trying to play Outcome Animation that isn't defined.");
			return;
		}
		outcomeAnimation3dObject.Play(outcomeAnimation3d.name);


		/* 
		Not ready to implement this
		if (slotSymbol != null && (slotSymbol.name == "WD" || isMutatedToWD))
		{
			showWild();
		}
		*/
	}


	/// Plays the mutate-to-this animation sequence, based on a SymbolAnimationType.
	/// Override this for special symbol prefab types.
	public override void playMutateFrom(SlotSymbol targetSymbol, string targetMutateName, bool playVfx = true)
	{
		isMutating = true;
		setSymbol(targetSymbol, targetMutateName);
		if (mutateFromAnimation3d == null || mutateFromAnimation3dObject == null)
		{
			Debug.LogWarning("Trying to play mutateFrom Animation that isn't defined on symbol " + targetSymbol.name + ".");
			return;
		}
		mutateFromAnimation3dObject.Play(mutateFromAnimation3d.name);
		if (playVfx)
		{
			Debug.Log("You would want to playVfx...");
		}
	}

	/// Plays the mutate-to-this animation sequence, based on a SymbolAnimationType.
	/// Override this for special symbol prefab types.
	public override void playMutateTo(SlotSymbol targetSymbol)
	{
		setSymbol(targetSymbol);
		if (mutateToAnimation3d == null || mutateToAnimation3dObject == null)
		{
			Debug.LogWarning("Trying to play mutateTo Animation that isn't defined on symbol " + targetSymbol.name + ".");
			return;
		}
		mutateToAnimation3dObject.Play(mutateToAnimation3d.name);
	}

	/// Get the base render level of a symbol animator
	public override int getBaseRenderLevel()
	{
		Profiler.BeginSample("SymAnim3D.getBaseRenderLevel");

		int baseRenderLevel = int.MaxValue;
		Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
		
		// Lets go though here and find the lowest renderer currently in the object so we can set the base level
		foreach (Renderer renderer in renderers)
		{
			if (renderer.material != null)
			{
				baseRenderLevel = Mathf.Min(baseRenderLevel,renderer.material.renderQueue);
			}
		}

		Profiler.EndSample();
		return baseRenderLevel;
	}

	/// Goes through every MeshRenderer and sets the render queue to queue.
	public override void changeRenderQueue(int queue)
	{
		Profiler.BeginSample("SymAnim3D.changeRenderQueue");

		int baseRenderLevel = getBaseRenderLevel();
		Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

		foreach (Renderer renderer in renderers)
		{
			if (renderer.material != null)
			{
				int renderDifference = renderer.material.renderQueue - baseRenderLevel;
				renderer.material.renderQueue = queue + renderDifference;
			}
		}

		Profiler.EndSample();
	}
}
