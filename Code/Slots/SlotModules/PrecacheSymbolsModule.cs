using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
/**
 * SlotModule.cs
 * author: Leo Schnee
 * This class will precache symbols before a reelgame is started up. 
 * NOTE: This script will increase load times.
 *
 */ 
[ExecuteInEditMode]
public class PrecacheSymbolsModule : SlotModule 
{

	[SerializeField] private List<CacheInformation> cacheInformation = new List<CacheInformation>();

	public override bool needsToExecuteOnBaseGameLoad(JSON slotGameStartedData)
	{
		// This function only gets called in SlotBaseGame, so we don't need to check if it is a slotbasegame.
		return true; //return reelGame is SlotBaseGame;
	}

	public override IEnumerator executeOnBaseGameLoad(JSON slotGameStartedData)
	{
		yield return StartCoroutine(cacheSymbolsToPoolCoroutine());
	}

	// Called after the slot game loads, and once init finishes in a freespins game.
	public override bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		// We only want to do this on freespin games because we want to do the caching when the load screen is up on the base game.
		return reelGame is FreeSpinGame;
	}

	public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		cacheSymbolsToPool();
	}

	private IEnumerator cacheSymbolsToPoolCoroutine()
	{
		foreach (CacheInformation info in cacheInformation)
		{
			Debug.Log("Caching " + info.numberToCache + " " + info.name + " symbols.");
			yield return StartCoroutine(reelGame.cacheSymbolsToPoolCoroutine(info.name, info.numberToCache, info.numberToCache /*How many to cache at once*/));
		}
	}

	private void cacheSymbolsToPool()
	{
		foreach (CacheInformation info in cacheInformation)
		{
			Debug.Log("Caching " + info.numberToCache + " " + info.name + " symbols.");
			reelGame.cacheSymbolsToPool(info.name, info.numberToCache);
		}
	}

	private void Update()
	{
		if (!Application.isPlaying)
		{
			// Make sure we have one for each symbol.
			foreach (SymbolInfo info in reelGame.symbolTemplates)
			{
				ReadOnlyCollection<string> possibleSymbolNames = info.getNameArrayReadOnly();
				foreach (string name in possibleSymbolNames)
				{
					if (cacheInformation.Find(x => x.name == name) == null)
					{
						cacheInformation.Add(new CacheInformation(name, 0));
					}
				}
			}
		}
	}

	[System.Serializable]
	private class CacheInformation
	{
		public string name;
		public int numberToCache;

		public CacheInformation(string name, int numberToCache)
		{
			this.name = name;
			this.numberToCache = numberToCache;
		}
	}
}
