using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Play an animation when a symbol count of certain symbol(s) is reached 
 * 
 * Games: orig012
 *
 * Original Author: Shaun Peoples <speoples@zynga.com>
 */
public class AnimateOnSlotSymbolLandedCountModule : SlotModule 
{
	[SerializeField] private List<SymbolLandData> landedSymbolsList;

	[System.Serializable]
	public class SymbolLandData
	{
		public string symbolName;
		public bool useNameContainsComparison;
		public AnimationListController.AnimationInformationList animation;
		public AudioListController.AudioInformationList audio;
		public int requiredLandedCount;
		public bool playAnimationOnce;
		[System.NonSerialized] public int landedCount;
		[System.NonSerialized] public bool animationPlayed;
	}

	private Dictionary<string, SymbolLandData> symbolDictionary = new Dictionary<string, SymbolLandData>();
	private List<SlotSymbol> visibleSymbols;

	public override void Awake()
	{
		foreach (SymbolLandData symbolLand in landedSymbolsList)
		{
			symbolDictionary.Add(symbolLand.symbolName, symbolLand);
		}
	}

	public override bool needsToExecuteOnReevaluationSpinEnd()
	{
		//reset the played flag on the animations
		foreach (SymbolLandData data in landedSymbolsList)
		{
			data.animationPlayed = false;
		}

		return false;
	}

	public override bool needsToExecuteOnReevaluationReelsStoppedCallback()
	{
		visibleSymbols = ReelGame.activeGame.engine.getAllVisibleSymbols();

		foreach (SymbolLandData data in landedSymbolsList)
		{
			data.landedCount = 0;
			foreach (SlotSymbol visible in visibleSymbols)
			{
				if (visible == null)
				{
					continue;
				}
				
				if (data.useNameContainsComparison)
				{
					if (!visible.serverName.Contains(data.symbolName))
					{
						continue;
					}
				}
				else if (data.symbolName != visible.serverName)
				{
					continue;
				}
				
				data.landedCount++;

				if (data.landedCount >= data.requiredLandedCount)
				{
					return true;
				}
			}
		}

		return false;
	}
		
	public override IEnumerator executeOnReevaluationReelsStoppedCallback()
	{
		foreach(SymbolLandData landData in landedSymbolsList)
		{
			if (landData.requiredLandedCount < landData.landedCount)
			{
				continue;
			}

			if (landData.animationPlayed && landData.playAnimationOnce)
			{
				continue;
			}

			landData.animationPlayed = true;
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(landData.animation));
			yield return StartCoroutine(AudioListController.playListOfAudioInformation(landData.audio));
		}
	}
}

