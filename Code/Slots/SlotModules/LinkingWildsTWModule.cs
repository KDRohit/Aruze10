using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;

/**
Used to implment a linking wild mutation feature which was originally part of munsters01
Basically when TW's land on two reels all symbols in the path between the two symbols turn into special wild symbols
*/
public class LinkingWildsTWModule : SlotModule 
{
	[SerializeField] private List<LinkingWildsAnimationInfo> linkingAnimationInfoList = new List<LinkingWildsAnimationInfo>(); // Info for animations played when symbols link
	[SerializeField] private string linkSymbolMutateName = ""; // What the symbol should swap to in order to do the mutate before finally swapping to the actual final WD version
	[SerializeField] private string TW_SYMBOL_VO = "";
	[SerializeField] private string MUTATION_ANIMATION_SOUND= "";
	[SerializeField] private float DELAY_BETWEEN_SYMBOL_MUTATIONS = 0.0f; // Time to wait between each symbol mutaiton
	[SerializeField] private float WAIT_BEFORE_FINISH_TIME = 0.0f; // Ability to add a delay so that all animations are complete before this module ends
	[SerializeField] private float DELAY_BEFORE_STARTING_SYMBOL_MUTATIONS = 0.0f; // Allows a delay to be introduced before the symbols start to mutate to allow previous things to finish before transforming the symbols

	[System.Serializable]
	protected class LinkingWildsAnimationInfo
	{
		[FormerlySerializedAs("startingSymbolIndex")] public int leftReelSymbolIndex = 0;		// The index that the animation will start at (in munsters01 case where the lighting will go to)
		[FormerlySerializedAs("endingSymbolIndex")] public int rightReelSymbolIndex = 0;		// The index that the animation will end at (in munsters01 case where the lighting will go to)
		public AnimationListController.AnimationInformationList linkingAnimations; // List of animaitons to play when linking is triggered between the two indices.
	
		// Get the lookup key that will be used in the dictionary
		public string getLookupKey()
		{
			return LinkingWildsTWModule.getLookupKey(leftReelSymbolIndex, rightReelSymbolIndex);
		}
	}

	private Dictionary<string, LinkingWildsAnimationInfo> linkingWildsAnimationInfoLookup = new Dictionary<string, LinkingWildsAnimationInfo>(); // Dictionary to lookup the anim info, using keys of the form <start_index>_<end_index>, for example 1_3

	public override void Awake()
	{
		base.Awake();

		// Build the lookup so we can quickly grab the right animation based on what the player lands
		for (int i = 0; i < linkingAnimationInfoList.Count; i++)
		{
			LinkingWildsAnimationInfo currentInfo = linkingAnimationInfoList[i];

			if (!linkingWildsAnimationInfoLookup.ContainsKey(currentInfo.getLookupKey()))
			{
				linkingWildsAnimationInfoLookup.Add(currentInfo.getLookupKey(), currentInfo);
			}
			else
			{
				Debug.LogWarning("LinkingWildsTWModule.Awake() - Trying to add currentInfo.getLookupKey() = " + currentInfo.getLookupKey() + " more than once!");
			}
		}
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		foreach (MutationBase mutation in reelGame.mutationManager.mutations)
		{
			if (mutation.type == "linking_wilds")
			{
				return true;
			}
		}
		
		// no linking_wilds mutaiton found
		return false;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		bool didWildLinking = false;
		SlotReel[] reelArray = reelGame.engine.getReelArray();

		for (int i = 0; i < reelGame.mutationManager.mutations.Count; i++)
		{
			MutationBase mutation = reelGame.mutationManager.mutations[i];

			if (mutation.type == "linking_wilds")
			{
				StandardMutation linkingWildsMutation = mutation as StandardMutation;

				if (linkingWildsMutation != null && linkingWildsMutation.twTriggeredSymbolList != null && linkingWildsMutation.twMutatedSymbolList != null)
				{
					didWildLinking = true;

					// start an animation for the path that was triggered
					if (linkingWildsMutation.twTriggeredSymbolList.Count == 2)
					{
						StandardMutation.ReplacementCell leftReelSymbolCell;
						StandardMutation.ReplacementCell rightReelSymbolCell;

						if (linkingWildsMutation.twTriggeredSymbolList[0].reelIndex < linkingWildsMutation.twTriggeredSymbolList[1].reelIndex)
						{
							leftReelSymbolCell = linkingWildsMutation.twTriggeredSymbolList[0];
							rightReelSymbolCell = linkingWildsMutation.twTriggeredSymbolList[1];
						}
						else
						{
							leftReelSymbolCell = linkingWildsMutation.twTriggeredSymbolList[1];
							rightReelSymbolCell = linkingWildsMutation.twTriggeredSymbolList[0];
						}

						// animate the two symbols triggering the effect
						for (int k = 0; k < linkingWildsMutation.twTriggeredSymbolList.Count; k++)
						{
							StandardMutation.ReplacementCell currentCell = linkingWildsMutation.twTriggeredSymbolList[k];
							SlotSymbol targetSymbol = reelArray[currentCell.reelIndex].visibleSymbolsBottomUp[currentCell.symbolIndex];
							
							if (targetSymbol.serverName != currentCell.replaceSymbol)
							{
								targetSymbol.mutateTo(currentCell.replaceSymbol);
							}
							
							targetSymbol.animateOutcome();
						}

						// now play the animation effects
						string lookupKey = getLookupKey(leftReelSymbolCell.symbolIndex, rightReelSymbolCell.symbolIndex);

						if (linkingWildsAnimationInfoLookup.ContainsKey(lookupKey))
						{
							LinkingWildsAnimationInfo animInfo = linkingWildsAnimationInfoLookup[lookupKey];
							yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(animInfo.linkingAnimations));
						}
						else
						{
							Debug.LogWarning("LinkingWildsTWModule.executeOnReelsStoppedCallback() - Couldn't find animation setup with lookupKey: " + lookupKey + "!");
						}
					}
					else
					{
						if (linkingWildsMutation.twTriggeredSymbolList.Count == 0)
						{
							Debug.LogWarning("LinkingWildsTWModule.executeOnReelsStoppedCallback() - No data detected to be used for the linking!");
						}
						else
						{
							Debug.LogWarning("LinkingWildsTWModule.executeOnReelsStoppedCallback() - Linking between more than 2 symbols isn't supported yet!  If you need it implment it.");
						}
					}

					if(!string.IsNullOrEmpty(TW_SYMBOL_VO))
					{
						Audio.playSoundMapOrSoundKey(TW_SYMBOL_VO);
					}

					// Add a delay here before we actually start mutating the symbols
					// in case we want stuff above to reach a certain point before changing
					// the symbols out
					if (DELAY_BEFORE_STARTING_SYMBOL_MUTATIONS != 0.0f)
					{
						yield return new TIWaitForSeconds(DELAY_BEFORE_STARTING_SYMBOL_MUTATIONS);
					}

					List<TICoroutine> mutationCoroutines = new List<TICoroutine>();

					for (int k = 0; k < linkingWildsMutation.twMutatedSymbolList.Count; k++)
					{
						StandardMutation.ReplacementCell symbolInfo = linkingWildsMutation.twMutatedSymbolList[k];
						SlotSymbol targetSymbol = reelArray[symbolInfo.reelIndex].visibleSymbolsBottomUp[symbolInfo.symbolIndex];

						if (string.IsNullOrEmpty(linkSymbolMutateName))
						{
							targetSymbol.mutateTo(symbolInfo.replaceSymbol);
						}
						else
						{
							mutationCoroutines.Add(StartCoroutine(animateSymbolMutation(targetSymbol, symbolInfo.replaceSymbol)));
						}
						
						if(!string.IsNullOrEmpty(MUTATION_ANIMATION_SOUND))
						{
							Audio.playSoundMapOrSoundKey(MUTATION_ANIMATION_SOUND);
						}

						if (DELAY_BETWEEN_SYMBOL_MUTATIONS > 0.0f && k != linkingWildsMutation.twMutatedSymbolList.Count - 1)
						{
							yield return new TIWaitForSeconds(DELAY_BETWEEN_SYMBOL_MUTATIONS);
						}
					}

					if (mutationCoroutines.Count > 0)
					{
						yield return StartCoroutine(Common.waitForCoroutinesToEnd(mutationCoroutines));
					}
				}
			}
		}

		if (didWildLinking && WAIT_BEFORE_FINISH_TIME > 0.0f)
		{
			yield return new TIWaitForSeconds(WAIT_BEFORE_FINISH_TIME);
		}
	}

	private IEnumerator animateSymbolMutation(SlotSymbol targetSymbol, string replaceSymbolName)
	{
		SymbolInfo mutateSymbolInfo = reelGame.findSymbolInfo(linkSymbolMutateName);

		if (mutateSymbolInfo != null)
		{
			targetSymbol.mutateTo(linkSymbolMutateName);
			yield return StartCoroutine(targetSymbol.playAndWaitForAnimateOutcome());

		}
		else
		{
			Debug.LogWarning("LinkingWildsTWModule.animateSymbolMutation() - Couldn't find symbol info for linkSymbolMutateName = " + linkSymbolMutateName + " skipping mutation animation!");
		}

		targetSymbol.mutateTo(replaceSymbolName);
	}

	public static string getLookupKey(int leftReelSymbolIndex, int rightReelSymbolIndex)
	{
		return leftReelSymbolIndex + "_" + rightReelSymbolIndex;
	}
}
