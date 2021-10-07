using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//
// Plays the symbol anticipation animation when the reel stops and does it rollback without
// needing the reel to be marked as an anticipation reel and the symbols don't have to be
// bonus symbols.
//
// Author : Nick Saito <nsaito@zynga.com>
// Date : Dec 11th, 2017
// games : orig010
//
public class PlaySymbolAnticipationAnimationWithSoundOnReelEndRollbackModule : SlotModule
{
	[Tooltip("List of the symbols, reels, sounds to play.")] [SerializeField]
	private List<CustomAnticipationAnimationData> anticipationAnimationDataList;

	// A map of reels and symbols that we can use to quickly get the anticipation data during a spin.
	// It has the format _anticipationAnimationMap[<reelId>][<symbolName>]. If the key exists then we
	// can play the anticipation for that symbol and get at the audio list to go with it.
	//
	// example : _anticipationAnimationMap[1][SC3].soundsPlayedDuringAnimation
	private Dictionary<int, Dictionary<string, CustomAnticipationAnimationData>> _anticipationAnimationMap;

	public override void Awake()
	{
		base.Awake();
		createAnticipationAnimationDataMap();
	}

	public override bool needsToExecuteOnReelEndRollback(SlotReel reel)
	{
		return
			anticipationAnimationDataList != null &&
			anticipationAnimationDataList.Count > 0 &&
			_anticipationAnimationMap.ContainsKey(reel.reelID - 1);
	}

	public override IEnumerator executeOnReelEndRollback(SlotReel reel)
	{
		playSymbolAnticipationsOnReel(reel);
		yield break;
	}

	private void playSymbolAnticipationsOnReel(SlotReel reel)
	{
		foreach (SlotSymbol slotSymbol in reel.visibleSymbols)
		{
			if (_anticipationAnimationMap[reel.reelID - 1].ContainsKey(slotSymbol.serverName))
			{
				CustomAnticipationAnimationData anticipationData = _anticipationAnimationMap[reel.reelID - 1][slotSymbol.serverName];
				slotSymbol.animateAnticipation();

				if (anticipationData.soundsPlayedDuringAnimation != null)
				{
					AudioListController.playListOfAudioInformation(anticipationData.soundsPlayedDuringAnimation);
				}
			}
		}
	}

	// Creates a mapping of reel and symbol name to its custom animation data for quick lookup.
	private void createAnticipationAnimationDataMap()
	{
		_anticipationAnimationMap = new Dictionary<int, Dictionary<string, CustomAnticipationAnimationData>>();
		foreach (CustomAnticipationAnimationData anticipationData in anticipationAnimationDataList)
		{
			foreach (int reelIndex in anticipationData.reelIndicies)
			{
				if (!_anticipationAnimationMap.ContainsKey(reelIndex))
				{
					// new reel index, so add it along with all the symbol names
					_anticipationAnimationMap.Add(reelIndex, new Dictionary<string, CustomAnticipationAnimationData>());
				}

				foreach (string symbolName in anticipationData.symbolNames)
				{
					if (!_anticipationAnimationMap[reelIndex].ContainsKey(symbolName))
					{
						_anticipationAnimationMap[reelIndex].Add(symbolName, anticipationData);
					}
				}
			}
		}
	}

	public override bool needsToExecuteForSymbolAnticipation(SlotSymbol symbol)
	{
		int reelIndex = symbol.reel.reelID - 1;

		return _anticipationAnimationMap.ContainsKey(reelIndex) && _anticipationAnimationMap[reelIndex].ContainsKey(symbol.serverName);
	}

	public override void executeForSymbolAnticipation(SlotSymbol symbol)
	{
		// This is called by SpinReel when a reel is marked with isAnticipationReel.
		// Since we are handling the symbol anticipation for this reel/symbol just override
		// this to let the reel know we got this.
	}

	// Data class for holding information about animations for each symbol
	// animations we want to override.
	[System.Serializable]
	public class CustomAnticipationAnimationData
	{
		[Tooltip("list of reels to play symbol anticipation animations on (0 based).")]
		public List<int> reelIndicies = new List<int>();

		[Tooltip("list of symbols names to play anticipation animation.")]
		public List<string> symbolNames = new List<string>();

		[Tooltip("Sounds to play with the symbol anticipation animation.")]
		public AudioListController.AudioInformationList soundsPlayedDuringAnimation = new AudioListController.AudioInformationList();
	}
}
