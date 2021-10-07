using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
FreeSpin implementation class for Gen10 Howling Wilds
*/
public class Gen10FreeSpins : FreeSpinGame 
{
	[SerializeField] private Gen10MoonEffect moonEffect;				// The moon effect that shows the symbol that is turning wild
	[SerializeField] private Transform[] flyTopPoints = null;			// The points at the top where the newly changed wild symbol will land
	[SerializeField] private GameObject symbolIndicatorPrefab = null;	// Prefab to create symbol indicators that show which symbols are currently wild
	[SerializeField] private float TIME_TO_FLY_TO_SYMBOL_AT_TOP = 1.0f;			// How long it takes for the symbol that appears in the moon animation to fly to the top
	[SerializeField] private float FREESPIN_SUMMARY_VO_DELAY = 1.0f;
	[SerializeField] private float SYMBOL_INDICATOR_M1_SCALE = 0.214f;			// What the local scale should be of the indicator when it shrinks down	
	[SerializeField] private float SYMBOL_INDICATOR_OTHER_SCALE = 0.366f;		// What the local scale should be of the indicator when it shrinks down

	private string mutationTarget = "";
	private List<string> activeWilds = new List<string>();

	// Constant Variables
	private const string WILD_TRAVEL_SOUND = "WildMoonWildTravelsCoyote";
	private const string WILD_ARIVES_SOUND = "WildMoonWildArrives";
	private const string FREE_SPIN_SUMMARY_VO_KEY = "freespin_summary_vo";
	
	public override void initFreespins ()
	{
		BonusGamePresenter.instance.useMultiplier = false;
		mutationManager = new MutationManager(true);
		base.initFreespins();
	}

	public override IEnumerator preReelsStopSpinning()
	{		
		if (mutationManager.mutations.Count > 0)
		{
			JSON mut = outcome.getMutations()[0];
			
			mutationTarget = mut.getString("replace_symbol", "");
			if(mutationTarget != "")
			{
				yield return StartCoroutine(this.doWilds());
			}
		}
		yield return StartCoroutine(base.preReelsStopSpinning());
	}

	/// Brings up the crystal ball and activates the wild overlay for the chosen symbol
	private IEnumerator doWilds()
	{
		// Find the symbol that is being made wild
		JSON mut = outcome.getMutations()[0];
		mutationTarget = mut.getString("replace_symbol", "");

		if (mutationTarget == "M1")
		{
			mutationTarget = "M1-2A";
		}
		
		yield return StartCoroutine(moonEffect.playMoonFeature(mutationTarget));

		// add the current wild to the list of active ones so they will start being changed as the spin
		activeWilds.Add(mutationTarget);

		// hide the feature animation as we replace it with the symbol indicator
		moonEffect.hideFeatureAnimation();

		// create the indicator instance
		GameObject indicatorObj = CommonGameObject.instantiate(symbolIndicatorPrefab) as GameObject;
		indicatorObj.transform.parent = transform;
		indicatorObj.transform.localPosition = Vector3.zero;
		Gen10FreeSpinSymbolIndicator indicator = indicatorObj.GetComponent<Gen10FreeSpinSymbolIndicator>();
		indicator.setParticleTrailVisible(true);
		indicator.playSymbolAnimation(mutationTarget);
		
		// Determine the destination position based on which symbol we have.
		int iconIndex = 0;

		// Note M1-2A will map to 0 so don't need to set it, otherwise calculate using the int part of the name
		if (mutationTarget != "M1-2A")
		{
			iconIndex = int.Parse(mutationTarget.Substring(1)) - 1;
		}

		Vector3 targetScale;
		if (mutationTarget == "M1-2A")
		{
			targetScale = new Vector3(SYMBOL_INDICATOR_M1_SCALE, SYMBOL_INDICATOR_M1_SCALE, 1.0f);
		}
		else
		{
			targetScale = new Vector3(SYMBOL_INDICATOR_OTHER_SCALE, SYMBOL_INDICATOR_OTHER_SCALE, 1.0f);
		}
		
		// Fly from the newly duplicated moon symbol to the small symbol up top. Also need to scale down to have it fit the top row symbol size.
		Vector3 targetPos = flyTopPoints[iconIndex].position;
		targetPos.z = indicatorObj.transform.position.z;
		Audio.play(WILD_TRAVEL_SOUND);
		iTween.MoveTo(indicatorObj, iTween.Hash("position", targetPos, "time", TIME_TO_FLY_TO_SYMBOL_AT_TOP, "easetype", iTween.EaseType.easeInOutQuad));
		iTween.ScaleTo(indicatorObj, iTween.Hash("scale", targetScale, "time", TIME_TO_FLY_TO_SYMBOL_AT_TOP, "easetype", iTween.EaseType.easeInOutQuad));
		
		// Wait for the flight to finish.
		yield return new TIWaitForSeconds(TIME_TO_FLY_TO_SYMBOL_AT_TOP);
		Audio.play(WILD_ARIVES_SOUND);

		indicator.setParticleTrailVisible(false);

		// turn off the shroud from the moon feature
		moonEffect.hideFeature();

		engine.setOutcome(_outcome);
	}

	public override SymbolAnimator getSymbolAnimatorInstance(string name, int columnIndex = -1, bool forceNewInstance = false, bool canSearchForMegaIfNotFound = false)
	{
		// Grab the symbol and activate its wild overlay if its the targeted symbol from the mutation
		SymbolAnimator newSymbolAnimator;

		string serverName = SlotSymbol.getServerNameFromName(name);
		if (activeWilds.Contains(serverName))
		{
			newSymbolAnimator = base.getSymbolAnimatorInstance(serverName, columnIndex, forceNewInstance, canSearchForMegaIfNotFound);
			newSymbolAnimator.showWild();
		}
		else
		{
			newSymbolAnimator = base.getSymbolAnimatorInstance(name, columnIndex, forceNewInstance, canSearchForMegaIfNotFound);
		}
		
		return newSymbolAnimator;
	}

	protected override void gameEnded()
	{
		Audio.play(Audio.soundMap(FREE_SPIN_SUMMARY_VO_KEY), 1.0f, 0.0f, FREESPIN_SUMMARY_VO_DELAY);
		base.gameEnded();
	}
}
