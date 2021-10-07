using UnityEngine;
using System.Collections;

/**
 * SymbolSplitEffectModule.cs
 * Handles playing any effects and animations when non 1x1 symbols are split into 1x1s.

 * Original Author: Stephen Arredondo

*/

public class SymbolSplitEffectModule : SlotModule {

	[SerializeField] private GameObject splitEffect = null;
	[SerializeField] private string SPLIT_EFFECT_ANIMATION;
	[SerializeField] private GameObject cachedSplitEffectHolder = null;

	private GameObjectCacher splitEffectCacher = null;

	public override void Awake ()
	{
		splitEffectCacher = new GameObjectCacher(this.gameObject, splitEffect);

		base.Awake();

		if (cachedSplitEffectHolder == null)
		{
			cachedSplitEffectHolder = new GameObject();
			cachedSplitEffectHolder.name = "Cached Split Effect Holder";
			cachedSplitEffectHolder.transform.parent = reelGame.gameObject.transform;
		}
	}

	public override bool needsToExecuteAfterSymbolSplit()
	{
		return true;
	}

	public override void executeAfterSymbolSplit(SlotSymbol splittableSymbol)
	{
		//Play our effect only if its set in the inspector
		if (splitEffect != null && splittableSymbol != null) {
			if (FreeSpinGame.instance != null && FreeSpinGame.instance.engine != null)
			{
				FreeSpinGame.instance.engine.effectInProgress = true;
			} 
			else if (SlotBaseGame.instance != null && SlotBaseGame.instance.engine != null)
			{
				SlotBaseGame.instance.engine.effectInProgress = true;
			}
			GameObject splitEffectObject = splitEffectCacher.getInstance();
			if (splitEffectObject != null)
			{
				splitEffectObject.transform.parent = splittableSymbol.transform;
				splitEffectObject.transform.localPosition = Vector3.zero;
				splitEffectObject.transform.localScale = splitEffect.transform.localScale;
				splitEffectObject.SetActive (true);
				Animator splitEffectAnimator = splitEffectObject.GetComponent<Animator>();

				//Check for the animator in the children if its not in the parent
				if (splitEffectAnimator == null)
				{
					splitEffectAnimator = splitEffectObject.GetComponentInChildren<Animator>();
				}

				//Play our animation now if it was it in the parent or the children
				if (splitEffectAnimator != null)
				{
					StartCoroutine(playAndReleaseSplitEffectInstance(splitEffectObject, splitEffectAnimator));
				}
			}
		} 
		else 
		{
			Debug.LogWarning("Module is missing an effect to play. Set it in the inspector. Your symbol could also be null");
		}
	}

	private IEnumerator playAndReleaseSplitEffectInstance(GameObject splitEffectObject, Animator splitEffectAnimator)
	{
		//Coroutine for releasing the gameobject back to the cache. Using the coroutine so we can
		// wait until the animation plays out before releasing it.
		if (splitEffectAnimator != null) 
		{
			yield return StartCoroutine (CommonAnimation.playAnimAndWait(splitEffectAnimator, SPLIT_EFFECT_ANIMATION));
			if (ReelGame.activeGame != null)
			{
				ReelGame.activeGame.engine.effectInProgress = false;
			} 
			else
			{
				Debug.LogError("There is no active game and we're in a module!");
			}
		}
		// store it off the symbol, in the holder object
		splitEffectObject.transform.parent = cachedSplitEffectHolder.transform;

		// release it back into the cache
		splitEffectCacher.releaseInstance(splitEffectObject);
	}
}
