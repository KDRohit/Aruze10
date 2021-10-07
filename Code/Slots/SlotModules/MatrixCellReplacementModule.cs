using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//Adds matrix_cell_replacement mutations to a games and removes the old ones between spins
public class MatrixCellReplacementModule : BaseMatrixCellReplacementModule
{
	[SerializeField] private GameObject mutateSymbolPrefab;
	[SerializeField] private string MUTATING_SYMBOL_NAME = "";
	[Tooltip("Adds a cumulative z offset for each wild symbol in the feature so the don't end up behind another symbol")]
	[SerializeField] private float zOffsetStep = 0f;
	private GameObjectCacher mutatingSymbolCacher = null;
	private List<GameObject> mutatedSymbols = new List<GameObject>();
	protected List<SlotSymbol> replacements = new List<SlotSymbol>();
	protected float cumulativeZOffset;

	public override void Awake()
	{
		base.Awake();
		mutatingSymbolCacher = new GameObjectCacher(this.gameObject, mutateSymbolPrefab);
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		// Keep looping until all of the mutations are done, so we ensure everything is cleaned up correctly
		while (numSymbolsCurrentlyMutating > 0)
		{
			yield return null;
		}

		foreach (SlotSymbol targetSymbol in replacements)
		{
			targetSymbol.mutateTo(MUTATING_SYMBOL_NAME);
		}

		foreach (GameObject go in mutatedSymbols)
		{			
			Destroy(go);
		}

		cumulativeZOffset = 0f;
		mutatedSymbols.Clear();
		replacements.Clear();
		yield break;
	}

	protected override IEnumerator mutateSymbol(SlotSymbol symbol, GameObject mutatingSymbol, int row)
	{
		numSymbolsCurrentlyMutating++;
		replacements.Add(symbol);

		//Make sure our symbol exists, if so create, scale, and position it
		if (mutateSymbolPrefab != null)
		{
			SymbolInfo mutateInfo = reelGame.findSymbolInfo(MUTATING_SYMBOL_NAME);
			if (mutateInfo != null)
			{
				mutatingSymbol = mutatingSymbolCacher.getInstance();
				mutatingSymbol.SetActive(false);
				mutatingSymbol.transform.parent = symbol.reel.getReelGameObject().transform;
				mutatingSymbol.transform.localScale = mutateInfo.scaling;
				mutatingSymbol.transform.localPosition = new Vector3(0.0f, mutateInfo.positioning.y + ((row - 1) * reelGame.symbolVerticalSpacingLocal), mutatingSymbol.transform.localPosition.z - cumulativeZOffset);
				cumulativeZOffset += zOffsetStep;

				if (particleTrail != null)
				{
					Vector3 endPos = new Vector3 (mutatingSymbol.transform.position.x, mutatingSymbol.transform.position.y, mutatingSymbol.transform.localPosition.z - cumulativeZOffset);
					yield return StartCoroutine(particleTrail.animateParticleTrail(endPos, particleTrail.transform.parent));
				}

				//Play our tranform sound
				Audio.playSoundMapOrSoundKey(WD_TRANSFORM_SOUND);
				//mutatingSymbol.SetActive(true);
			}
		}

		numSymbolsCurrentlyMutating--;

		foreach (SlotSymbol targetSymbol in replacements)
		{
			targetSymbol.mutateTo(MUTATING_SYMBOL_NAME);
		}

	}
}
