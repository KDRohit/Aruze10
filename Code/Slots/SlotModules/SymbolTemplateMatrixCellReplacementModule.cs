using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//Adds matrix_cell_replacement mutations to a games and removes the old ones between spins
public class SymbolTemplateMatrixCellReplacementModule : BaseMatrixCellReplacementModule
{
	[SerializeField] private string outcomeSymbolName;
	[SerializeField] private string replacementSymbolTemplateName;
	[SerializeField] private string mutateToSymbolTemplateName;
	private SymbolInfo replacementSymbolInfo;
	private GameObjectCacher mutatingSymbolCacher = null;
	private List<GameObject> mutatedSymbols = new List<GameObject>();
	protected List<SlotSymbol> replacements = new List<SlotSymbol>();

	public override void Awake()
	{
		base.Awake();
		replacementSymbolInfo = reelGame.findSymbolInfo(replacementSymbolTemplateName);
		if (replacementSymbolInfo == null)
		{
			Debug.LogError("Could not find symbol info for '" + replacementSymbolTemplateName + "'!");
#if !ZYNGA_TRAMP
			Debug.Break();
#endif
		}
		mutatingSymbolCacher = new GameObjectCacher(this.gameObject, replacementSymbolInfo.symbolPrefab);
	}
	
	public override IEnumerator executeOnReelsStoppedCallback()
	{
		foreach (SlotSymbol targetSymbol in replacements)
		{			
			targetSymbol.mutateTo(mutateToSymbolTemplateName);
		}
		foreach (GameObject go in mutatedSymbols)
		{			
			Destroy(go);
		}		
		mutatedSymbols.Clear();
		replacements.Clear();
		yield break;
	}

	protected override IEnumerator mutateSymbol(SlotSymbol symbol, GameObject mutatingSymbol, int row)
	{
		replacements.Add(symbol);

		mutatingSymbol = mutatingSymbolCacher.getInstance();
		mutatingSymbol.SetActive(false);
		mutatingSymbol.transform.parent = symbol.reel.getReelGameObject().transform;
		mutatingSymbol.transform.localScale = replacementSymbolInfo.scaling;
		mutatingSymbol.transform.localPosition = new Vector3 (
			replacementSymbolInfo.positioning.x,
			replacementSymbolInfo.positioning.y + ((row - 1) * reelGame.symbolVerticalSpacingLocal),
			mutatingSymbol.transform.localPosition.z
		);

		if (particleTrail != null)
		{
			Vector3 endPos = new Vector3 (mutatingSymbol.transform.position.x, mutatingSymbol.transform.position.y, 0.0f);
			yield return StartCoroutine(particleTrail.animateParticleTrail(endPos, particleTrail.transform.parent));
		}

		//Play our tranform sound
		Audio.playSoundMapOrSoundKey(WD_TRANSFORM_SOUND);

		mutatingSymbol.SetActive(true);
		mutatedSymbols.Add(mutatingSymbol);
	}
}
