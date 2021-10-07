using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Wynonna01ReplacementCellWildModule : ReplacementCellWildModule
{
	[SerializeField] private List<GameObject> WD1noteBombPrefabs = new List<GameObject>(); //Types of notes that can fall from the plane's banner
	[SerializeField] private List<GameObject> W2noteBombPrefabs = new List<GameObject>(); //Notes that fall when mutating into a W2
	[SerializeField] private float noteBombStartingY = 0.0f;
	[SerializeField] private float noteBombTweenTime = 1.0f;
	[SerializeField] private Vector3 effectPrefabScale;
	[SerializeField] private Transform noteBombparentTransform;

	[SerializeField] private string WILD_REVEAL_KEY;

	private int wildRevealNumber = 0; //Keep track of how many WDs we've mutated so we can play the correct sound number

	private RandomGameObjectCacher WD1noteBombsCache = null;
	private RandomGameObjectCacher W2noteBombsCache = null;

	public override void Awake()
	{
		WD1noteBombsCache = gameObject.AddComponent<RandomGameObjectCacher>();
		WD1noteBombsCache.createCache(WD1noteBombPrefabs);

		W2noteBombsCache = gameObject.AddComponent<RandomGameObjectCacher>();
		W2noteBombsCache.createCache(W2noteBombPrefabs);
		base.Awake ();

		//If there is no parent for the note bombs then just parent them to a new object parented to the reel game
		if (noteBombparentTransform == null)
		{
			GameObject noteBombParent = new GameObject("Note Bomb Parent");
			noteBombParent.transform.parent = this.transform;
			noteBombparentTransform = noteBombParent.transform;
		}
	}

	protected override IEnumerator mutateOneSymbol (SlotSymbol targetSymbol, GameObject effectPrefab, string symbolName, int reelIndex, int symbolIndex)
	{
		GameObject noteBomb = null;
		//Grab a random type of note from our array and spawn it at the same height of the plane
		if (symbolName == "WD1")
		{
			noteBomb = WD1noteBombsCache.getInstance();
		}
		else
		{
			//Only 2 typs of wilds so if its not a wd1 then we know it has to be a wd2
			noteBomb = W2noteBombsCache.getInstance();
		}
		noteBomb.SetActive(true);
		noteBomb.transform.parent = noteBombparentTransform;
		noteBomb.transform.position = new Vector3 (targetSymbol.transform.position.x, noteBombStartingY, 0.0f);

		//Figure out the position of the symbol we're going to mutate and tween the falling note to it.
		Vector3 targetPosition = reelGame.engine.getReelRootsAt (reelIndex, symbolIndex).transform.position;
		targetPosition = new Vector3 (targetPosition.x, targetPosition.y + (symbolIndex * reelGame.symbolVerticalSpacingWorld), targetPosition.z);
		yield return new TITweenYieldInstruction(iTween.MoveTo(noteBomb, iTween.Hash("position", targetPosition, "time", noteBombTweenTime, "islocal", false, "easetype", iTween.EaseType.linear)));

		//Destroy our note now and play the mutation
		if (wildRevealNumber >= 3)
		{
			wildRevealNumber = 0;
		}
		wildRevealNumber++;
		Audio.play(Audio.soundMap(WILD_REVEAL_KEY + wildRevealNumber));

		if (symbolName == "WD1")
		{
			WD1noteBombsCache.releaseInstance(noteBomb);
		}
		else
		{
			W2noteBombsCache.releaseInstance(noteBomb);
		}

		yield return StartCoroutine(base.mutateOneSymbol (targetSymbol, effectPrefab, symbolName, reelIndex, symbolIndex));
	}
}
