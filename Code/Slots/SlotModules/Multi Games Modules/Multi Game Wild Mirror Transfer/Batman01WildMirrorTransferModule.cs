using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Batman01WildMirrorTransferModule : MultiGameWildMirrorTransferModule {

	[SerializeField] private GameObject sparkleTrailObject = null;
	[SerializeField] private GameObject sparkleExplosionObject = null;
	[SerializeField] private GameObject mutatingWDObject = null;
	[SerializeField] private List<GameObject> wdOverlayObjects = new List<GameObject>();
	[SerializeField] private float SPARKLE_TRAIL_WAIT = 0.0f;
	[SerializeField] private float MUTATION_ANIM_LENGTH = 0.0f;
	[SerializeField] private float SPARKLE_EXPLOSION_LENGTH = 0.25f;
	[SerializeField] private float MUTATED_SYMBOL_Z_OFFSET = 0.0f;

	private RandomGameObjectCacher wdOverlayCache = null;
	private GameObjectCacher mutatingWDCache = null;
	private GameObjectCacher sparkleTrailCache = null;
	private GameObjectCacher sparkleExplosionCache = null;

	private List<GameObject> activeOverlays = new List<GameObject>();
	private List<GameObject> activeMutatedSymbols = new List<GameObject>();
	private List<GameObject> activeSparkleTrails = new List<GameObject>();
	private List<GameObject> activeSparkleExplosions = new List<GameObject>();

	private bool hasBonus = false;
	private bool transferringWDs = false;

	private const string WD_OVERLAY_SOUND_KEY = "TW_symbol_land";
	private const string WD_FEATURE_END_KEY = "TW_symbol_resolve";

	public override void Awake()
	{
		wdOverlayCache = gameObject.AddComponent<RandomGameObjectCacher>();
		wdOverlayCache.createCache(wdOverlayObjects);
		mutatingWDCache = new GameObjectCacher(this.gameObject, mutatingWDObject);
		sparkleTrailCache = new GameObjectCacher(this.gameObject, sparkleTrailObject);
		sparkleExplosionCache = new GameObjectCacher(this.gameObject, sparkleExplosionObject);
		base.Awake();
	}

	protected override IEnumerator doWildsTransfer(SlotSymbol fromSymbol, SlotSymbol toSymbol, Vector3 targetPosition)
	{
		//Play our "BAM" overlay on the WD that landed
		GameObject fromOverlay = wdOverlayCache.getInstance();
		activeOverlays.Add(fromOverlay);
		fromOverlay.transform.position = fromSymbol.transform.position;
		fromOverlay.transform.localPosition += new Vector3(0.0f, 0.0f, MUTATED_SYMBOL_Z_OFFSET);
		fromOverlay.SetActive(true);
		if (Audio.canSoundBeMapped(WD_OVERLAY_SOUND_KEY))
		{
			Audio.play(Audio.soundMap(WD_OVERLAY_SOUND_KEY));
		}

		//Have our sparkle trail transfer from the landed symbol to the other
		GameObject sparkleTrail = sparkleTrailCache.getInstance();
		activeSparkleTrails.Add(sparkleTrail);
		sparkleTrail.transform.position = fromSymbol.transform.position;
		sparkleTrail.SetActive(true);
		yield return new TITweenYieldInstruction(iTween.MoveTo(sparkleTrail, iTween.Hash("position", targetPosition, "time", SPARKLE_TRAIL_WAIT, "islocal", false, "easetype", iTween.EaseType.easeInQuart)));
		sparkleTrail.SetActive(false);

		//Now play our sparkle explosion on our newly mutated symbol
		GameObject sparkleExplosion = sparkleExplosionCache.getInstance();
		activeSparkleExplosions.Add(sparkleExplosion);
		sparkleExplosion.transform.position = targetPosition;
		sparkleExplosion.SetActive(true);
		if (Audio.canSoundBeMapped(WD_FEATURE_END_KEY))
		{
			Audio.play(Audio.soundMap(WD_FEATURE_END_KEY));
		}


		GameObject toOverlay = wdOverlayCache.getInstance();
		activeOverlays.Add(toOverlay);
		toOverlay.transform.position = targetPosition;
		toOverlay.SetActive(true);

		//Slap out mutated version on top of the spinning reels, similar to a sticky symbol
		GameObject mutatedSymbolObject = mutatingWDCache.getInstance();
		activeMutatedSymbols.Add(mutatedSymbolObject);
		mutatedSymbolObject.transform.position = targetPosition;
		mutatedSymbolObject.transform.localPosition += new Vector3(0.0f, 0.0f, MUTATED_SYMBOL_Z_OFFSET);
		mutatedSymbolObject.transform.parent = toSymbol.transform.parent;
		mutatedSymbolObject.transform.localScale = reelGame.findSymbolInfo("WD").scaling;
		mutatedSymbolObject.SetActive(true);
		yield return new TIWaitForSeconds(SPARKLE_EXPLOSION_LENGTH);
		if(toSymbol.canBeSplit() && toSymbol.isTallSymbolPart)
		{
			toSymbol.splitSymbol();
		}
		sparkleExplosion.SetActive(false);
		--numberOfTransfers;
	}

	public override IEnumerator executeOnReelsStoppedCallback ()
	{
		if(!hasBonus && !reelGame.outcome.isBonus)
		{
			while (numberOfTransfers > 0)
			{
				yield return null;
			}
			yield return new TIWaitForSeconds(MUTATION_ANIM_LENGTH); //Before we do mutations and rollups lets let our effects finish
			yield return StartCoroutine(base.executeOnReelsStoppedCallback());
			cleanUpObjects();
		}
	}

	public override bool needsToExecuteOnPreBonusGameCreated ()
	{
		return true;
	}

	public override IEnumerator executeOnPreBonusGameCreated ()
	{
		hasBonus = true;
		while (numberOfTransfers > 0)
		{
			yield return null;
		}
		foreach(SlotSymbol symbol in symbolsToMutate)
		{
			symbol.mutateTo("WD");
		}
		symbolsToMutate.Clear();
		cleanUpObjects();
		yield break;
	}

	private void cleanUpObjects()
	{
		foreach(GameObject objectToRelease in activeOverlays)
		{
			wdOverlayCache.releaseInstance(objectToRelease);
		}

		foreach(GameObject objectToRelease in activeSparkleTrails)
		{
			sparkleTrailCache.releaseInstance(objectToRelease);
		}

		foreach(GameObject objectToRelease in activeSparkleExplosions)
		{
			sparkleExplosionCache.releaseInstance(objectToRelease);
		}

		foreach(GameObject objectToRelease in activeMutatedSymbols)
		{
			mutatingWDCache.releaseInstance(objectToRelease);
		}

		activeOverlays.Clear();
		activeSparkleTrails.Clear();
		activeSparkleExplosions.Clear();
		activeMutatedSymbols.Clear();
	}

	public override IEnumerator executeOnBonusGameEnded ()
	{
		hasBonus = false;
		yield break;
	}

	public override bool needsToExecuteOnBonusGameEnded ()
	{
		return true;
	}
}
