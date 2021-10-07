using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 *  Played a number of reel/position and time synced animations while another main animation plays
 *  - used to play animations that occur as a symbol is mutated and a main animation occurs
 *
 *  In the case of the first use, a magic carpet flies across all reel positions, and the
 *  animations are time synced to play at specific points in the carpet path
 *
 * Author : Shaun Peoples <speoples@zynga.com>
 * Date : November 2, 2020
 * Games : orig008
 *  
 */
public class AnimatedFeatureAnimationWithSymbolMutationEffectsModule : SlotModule
{
    [Header("Symbol Particle Effects")]
    [Tooltip("A list of symbol names that can launch an Animated Particle Effect")]
    [SerializeField] private List<string> symbolNamesToMatch;
    [Tooltip("The Animated Particle Effect launched by a matching symbol name")]
    [SerializeField] private AnimatedParticleEffect symbolMatchFeatureAnimationParticleEffect;
    [Tooltip("The animations played on the tease animation criteria")]
    [SerializeField] private AnimationListController.AnimationInformationList featureTeaseAnimation;
    [Tooltip("The chance of playing the tease animation when no feature rounds occur is met, 0 - 100")]
    [Range(0, 100)]
    [SerializeField] private int noFeatureRoundPlayTeaseAnimationChance;
    
    [Header("Round Setup")] 
    [Tooltip("There should be at least one defined Feature Round, in order 0 to Max")]
    [SerializeField]private List<FeatureRound> featureRounds;
    
    private MultiRoundTransformingSymbolsMutation multiRoundTransformingSymbolsMutation;
    private const string MUTATION_NAME = "multi_round_symbols_transform";
    readonly List<TICoroutine> roundCouroutines = new List<TICoroutine>();
    readonly List<TICoroutine> symbolMatchEffectCoroutines = new List<TICoroutine>();
    private int matchedSymbolsCount;

    [System.Serializable]
    private class FeatureRound
    {
        [Tooltip("Main animation that plays on round start")]
        public AnimationListController.AnimationInformationList onRoundStartAnimation;
        [Tooltip("Animated Particle Effect that is played at each matching symbol position")]
        public AnimatedParticleEffect transformAnimatedParticleEffect;
        [Tooltip("Should be a delay per symbol position on the reels, a 3x5 is 15 of these, start at 0,0")]
        [SerializeField] public List<TransformTimings> symbolPositionTransformTimings;
        [Tooltip("Animation that plays on round end")]
        public AnimationListController.AnimationInformationList onRoundEndAnimation;
        [Tooltip("Delay at the end of round")] 
        public float postRoundDelay;
        [Tooltip("The chance of playing the tease animation after this round, 0 - 100")]
        [Range(0, 100)]
        public int ifLastRoundTeaseChance;
        
        [System.Serializable]
        public class TransformTimings
        {
            public int reel;
            public int row;
            public float delay;
        }
    }

    public override bool needsToExecutePreReelsStopSpinning()
    {
        return true;
    }
    
    public override IEnumerator executePreReelsStopSpinning()
    {
        getMutations();
        yield break;
    }

    public override bool needsToExecuteOnPreSpin()
    {
        return true;
    }

    public override IEnumerator executeOnPreSpin()
    {
        symbolMatchEffectCoroutines.Clear();
        roundCouroutines.Clear();
        multiRoundTransformingSymbolsMutation = null;
        yield break;
    }

    private void getMutations()
    {
        if (reelGame.mutationManager == null || reelGame.mutationManager.mutations == null)
        {
            return;
        }

        List<MutationBase> mutationsList = reelGame.mutationManager.mutations;
        foreach (MutationBase mutation in mutationsList)
        {
            if (mutation.type == MUTATION_NAME)
            {
                multiRoundTransformingSymbolsMutation = mutation as MultiRoundTransformingSymbolsMutation;
                break;
            }
        }
    }
    
    public override bool needsToExecuteOnReelsStoppedCallback()
    {
        return true;
    }

    public override IEnumerator executeOnReelsStoppedCallback()
    {
        //play all the symbol matched particle effects (symbol to genie lamp, in orig008)
        yield return StartCoroutine(playSymbolMatchEffects());

        if(multiRoundTransformingSymbolsMutation == null)
        {
            if (matchedSymbolsCount > 0)
            {
                yield return StartCoroutine(tryPlayTeaseAnimation(noFeatureRoundPlayTeaseAnimationChance));        
            }
            yield break;
        }

        int featureRoundIndex = 0;
        int roundDataIndex = 0;
        foreach (List<MultiRoundTransformingSymbolsMutation.TransformedSymbol> transformedSymbols in multiRoundTransformingSymbolsMutation.rounds)
        {
            //didn't get any transformed symbols data, so early out here.
            if (transformedSymbols.Count < 1)
            {
                continue;
            }
            
            FeatureRound featureRound = featureRounds[featureRoundIndex];
            //advance the feature round index if we have more than one feature round, we may just have one and reuse it for different data;
            featureRoundIndex = featureRoundIndex + 1 >= featureRounds.Count ? featureRoundIndex : featureRoundIndex + 1;
            
            roundCouroutines.Clear();
            foreach (MultiRoundTransformingSymbolsMutation.TransformedSymbol transformedSymbol in transformedSymbols)
            {
                //stuff to adjust the server-provided reel/position into a client-usable one
                SlotReel slotReel = reelGame.engine.getSlotReelAt(transformedSymbol.reel);
                int symbolCount = slotReel.visibleSymbols.Length - 1;
                SlotSymbol visibleSymbol = slotReel.visibleSymbols[symbolCount - transformedSymbol.position];

                bool nameMatch = visibleSymbol.serverName == transformedSymbol.oldSymbol;

                //safety check to make sure the position information gets us the correct symbol name
                if (!nameMatch)
                {
                    Debug.LogError("Symbol name doesn't match the reel/position information");
                    continue;
                }
                
                roundCouroutines.Add(StartCoroutine(runSymbolAnimationAndMutate(featureRound, visibleSymbol, transformedSymbol, symbolCount)));
            }
            
            roundCouroutines.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(featureRound.onRoundStartAnimation)));
            
            yield return StartCoroutine(Common.waitForCoroutinesToEnd(roundCouroutines));
            
            //if the last round determine if we should play the tease animation, and what chance to use 
            if (roundDataIndex == multiRoundTransformingSymbolsMutation.rounds.Count-1)
            {
                yield return StartCoroutine(tryPlayTeaseAnimation(featureRound.ifLastRoundTeaseChance));   
            }
            roundDataIndex++;

            if (featureRound.onRoundEndAnimation.Count > 0)
            {
                yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(featureRound.onRoundEndAnimation));
            }

            if (featureRound.postRoundDelay > 0)
            {
                yield return new TIWaitForSeconds(featureRound.postRoundDelay);
            }
        }
    }
    
    private float getSymbolDelay(FeatureRound round, int reel, int position)
    {
        foreach (FeatureRound.TransformTimings timing in round.symbolPositionTransformTimings)
        {
            if (timing.reel == reel && timing.row == position)
            {
                return timing.delay;
            }
        }
        return 0;
    }

    private IEnumerator runSymbolAnimationAndMutate(FeatureRound featureRound, SlotSymbol symbolToMutate, MultiRoundTransformingSymbolsMutation.TransformedSymbol transformedSymbol, int symbolCount)
    {
        int clientSidePosition = symbolCount - transformedSymbol.position;
        float delay = getSymbolDelay(featureRound, transformedSymbol.reel, clientSidePosition); 
        
        if (delay > 0)
        {
            yield return new TIWaitForSeconds(delay);
        }

        yield return StartCoroutine(featureRound.transformAnimatedParticleEffect.animateParticleEffect(endTransform: symbolToMutate.transform));
        
        symbolToMutate.mutateTo(transformedSymbol.newSymbol);
    }

    private IEnumerator tryPlayTeaseAnimation(int featureTeaseAnimationChance)
    {
        if (matchedSymbolsCount < 1 || featureTeaseAnimationChance < 1 || featureTeaseAnimation.Count < 1)
        {
            yield break;
        }

        int playChance = Random.Range(0, 101);
        if (featureTeaseAnimationChance >= playChance)
        {
            yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(featureTeaseAnimation));
        }
    }

    private IEnumerator playSymbolMatchEffects()
    {
        if (symbolNamesToMatch.Count < 1)
        {
            yield break;
        }
        
        List<SlotSymbol> visibleSymbols = reelGame.engine.getAllVisibleSymbols();

        matchedSymbolsCount = 0;
        
        foreach (SlotSymbol slotSymbol in visibleSymbols)
        {
            if (symbolNamesToMatch.Contains(slotSymbol.serverName))
            {
                matchedSymbolsCount += 1;
                symbolMatchEffectCoroutines.Add(StartCoroutine(symbolMatchFeatureAnimationParticleEffect.animateParticleEffect(slotSymbol.transform)));
            }
        }

        yield return StartCoroutine(Common.waitForCoroutinesToEnd(symbolMatchEffectCoroutines));
    }
}
