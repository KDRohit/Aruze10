using UnityEngine;
using System.Collections;

public class HideTumbleSymbolsModule : SlotModule 
{
	[SerializeField] private GameObject hideAnimationPrefab;
	[SerializeField] private string[] hideAnimationSoundsPerReel;
	[SerializeField] private float timeToWaitBeforeAnimationPerReel = 0.2f;
	[SerializeField] private float timeToWaitBeforeSymbolCleanUp = 0.5f;
	[SerializeField] private float timeToWaitAfterSymbolCleanUp = 0.5f;
	private GameObjectCacher hideAnimationPrefabCache;

	public override void Awake()
	{
		base.Awake();

		if(hideAnimationPrefabCache == null && hideAnimationPrefab != null)
		{
			hideAnimationPrefabCache = new GameObjectCacher(transform.gameObject, hideAnimationPrefab);
		}
	}

	// Executed in TumbleReel.updateSymbolPositions(), used to check if a module is going to perform the symbol cleanup step
	// if a module doesn't then the TumbleReel will perform the cleanup
	public override bool isCleaningUpWinningSymbolInTumbleReel(SlotSymbol symbol)
	{
		return true;
	}

// Executed in TumbleReel.updateSymbolPositions(), used to perform an action before a symbol is being cleaned up because
// it was part of a win in the tumble reel and will be removed, for instance the poof effects which are handled by the
// HideTumbleSymbolsModule.  If your module will also cleanup the symbol then you should also override
// isCleaningUpWinningSymbolInTumbleReel (see above)
	public override bool needsToExecuteBeforeCleanUpWinningSymbolInTumbleReel(SlotSymbol symbol)
	{
		return true;
	}

	public override IEnumerator executeBeforeCleanUpWinningSymbolInTumbleReel(SlotSymbol symbol)
	{
		if (hideAnimationPrefab != null)
		{
			yield return new TIWaitForSeconds((symbol.reel.reelID - 1) * timeToWaitBeforeAnimationPerReel);
			GameObject hideAnimationPrefabInstance = hideAnimationPrefabCache.getInstance();
			hideAnimationPrefabInstance.transform.position = symbol.transform.position;
			hideAnimationPrefabInstance.SetActive(true);

			if (hideAnimationSoundsPerReel != null && hideAnimationSoundsPerReel.Length > symbol.reel.reelID - 1
				&& !string.IsNullOrEmpty(hideAnimationSoundsPerReel[symbol.reel.reelID - 1]))
			{
				Audio.playSoundMapOrSoundKey(hideAnimationSoundsPerReel[symbol.reel.reelID - 1]);
			}

			yield return new TIWaitForSeconds(timeToWaitBeforeSymbolCleanUp);
			symbol.cleanUp();
			yield return new TIWaitForSeconds(timeToWaitBeforeSymbolCleanUp);
			hideAnimationPrefabCache.releaseInstance(hideAnimationPrefabInstance);
		}
		else
		{
			symbol.cleanUp();
		}
	}
}
