using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
 * Shows picking game using dynamically placed and instanced picks as an overlay on symbols on a reel
 * This means that the normal picks that are defined in the PickingGameVariant aren't required to be setup
 * in the usual manner. A pick template is setup and used in pickAnchorTemplate and that is instantiated
 * at runtime.
 * 
 * Games: orig012
 *
 * Original Author: Shaun Peoples <speoples@zynga.com>
 */
public class PickingGameDynamicallyPlaceOverlayPicksOnSymbolsModule : PickingGameModule
{
    [SerializeField] private string symbolToMatchForPickPlacement;
    [SerializeField] private GameObject pickAnchorTemplate; 
    [SerializeField] private GameObject picksRoot;
    [SerializeField] private Vector3 pickScale;
    private readonly List<SlotSymbol> matchingSymbols = new List<SlotSymbol>();
    private readonly List<GameObject> dynamicPickAnchors = new List<GameObject>();

    private GameObjectCacher pickGameObjectCacher = null;
    
    private bool isInit;
    
    [Tooltip("Link to the pick game bonus presenter")]
    [SerializeField] private BonusGamePresenter pickGameBonusPresenter;
    [Tooltip("Link to the freespins game bonus game presenter (if this pick game is nested inside freespins)")]
    [SerializeField] private BonusGamePresenter freespinsBonusGamePresenter;
    
    [Header("Audio Settings")]
    [Tooltip("Sound that gets played as soon as the last scatter lands in pick game")]
    [SerializeField] private AudioListController.AudioInformationList pickStartSounds;
    [Tooltip("Sound that gets played after the last pick/reveal happens")] 
    [SerializeField] private AudioListController.AudioInformationList pickEndSounds;

    protected override void OnEnable()
    {
        if (isInit)
        {
            return;
        }
        
        pickAnchorTemplate.SetActive(false);

        ModularChallengeGameVariant modularChallengeGameVariant = GetComponent<ModularChallengeGameVariant>();
        
        if(modularChallengeGameVariant == null)
        {
            Debug.LogError("No ModularChallengeGameVariant component found for " + this.GetType().Name + " - Destroying script.");
        }
        
        pickingVariantParent = modularChallengeGameVariant as ModularPickingGameVariant;

        if (pickGameObjectCacher == null && pickingVariantParent != null)
        {
            pickGameObjectCacher = new GameObjectCacher(gameObject, pickAnchorTemplate, true);
        }

        List<SlotSymbol> visibleSymbols =  ReelGame.activeGame.engine.getAllVisibleSymbols();

        matchingSymbols.Clear();
        foreach (SlotSymbol slotSymbol in visibleSymbols)
        {
            if (slotSymbol.serverName == symbolToMatchForPickPlacement)
            {
                matchingSymbols.Add(slotSymbol);
            }
        }

        //clear pickAnchors from the picking variant parent
        pickingVariantParent.pickAnchors.Clear();
        
        //clear our dynamic pick anchors for reuse
        foreach (GameObject pickAnchor in dynamicPickAnchors)
        {
            pickGameObjectCacher.releaseInstance(pickAnchor);
        }
        dynamicPickAnchors.Clear();
        
        foreach (SlotSymbol symbol in matchingSymbols)
        {
            GameObject pickGameObject = pickGameObjectCacher.getInstance();
            pickGameObject.SetActive(true);
            pickGameObject.transform.parent = picksRoot.transform;
            pickGameObject.transform.position = symbol.transform.position;
            pickGameObject.transform.localScale = pickScale;
            pickingVariantParent.pickAnchors.Add(pickGameObject);
            dynamicPickAnchors.Add(pickGameObject);
        }
        
        StartCoroutine(AudioListController.playListOfAudioInformation(pickStartSounds));

        isInit = true;
    }

    public override bool needsToExecuteOnRoundEnd(bool isEndOfGame)
    {
        return pickGameBonusPresenter != null;
    }

    public override IEnumerator executeOnRoundEnd(bool isEndOfGame)
    {
        // Set this so that we don't try and go back to the base game when this multiplier pick is over
        pickGameBonusPresenter.isReturningToBaseGameWhenDone = false;
        
        yield return StartCoroutine(AudioListController.playListOfAudioInformation(pickEndSounds));
    }

    public override bool needsToExecuteOnBonusGamePresenterFinalCleanup()
    {
        isInit = false;
        return freespinsBonusGamePresenter != null;
    }

    public override IEnumerator executeOnBonusGamePresenterFinalCleanup()
    {
        if (freespinsBonusGamePresenter == null)
        {
            yield break;
        }
        //restore the BonusGamePresenter.instance to the FS since this pick game is nested inside one
        BonusGamePresenter.instance = freespinsBonusGamePresenter;
        BonusGamePresenter.instance.isReturningToBaseGameWhenDone = true;
    }
}
