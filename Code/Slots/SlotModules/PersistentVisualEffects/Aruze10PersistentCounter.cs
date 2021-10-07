using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Aruze10PersistentCounter : SlotModule
{

	[SerializeField] private List<VisualTargetSymbolData> visualTargetSymbols;
	private List<ReevaluationPersistentVisualEffect> cachedPersistentVisualEffectReevaluations; // store a list of reevaluations that operate on the persistent visual effect each spin
	private string VISUAL_DATA_COUNT_ON_SYMBOL_LAND_KEY = "visual_data_count_on_symbol_land";
	private string VISUAL_DATA_RESET_ON_BONUS_NAME_KEY = "visual_data_reset_on_bonus_name";
	private int scatterSymbolCounter = 0;

	public override bool needsToExecuteOnReelsStoppedCallback() { return true; }
	public override bool needsToExecutePreReelsStopSpinning() { return true; }

	public TextMeshPro coinTextMeshPro;

	public override IEnumerator executePreReelsStopSpinning()
	{
		cachedPersistentVisualEffectReevaluations = getPersistentVisualEffectReevaluations();
		yield break;
	}

	public void OnTrailParticleEffectComplete()
    {
		scatterSymbolCounter += 1;
		Debug.Log("_________Scatter Symbols Count_____" + scatterSymbolCounter);
		coinTextMeshPro.text = string.Empty + scatterSymbolCounter;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		List<ReevaluationPersistentVisualEffect> visualDataReevaluationForOnReelsStopped = getVisualDataReevaluationForOnReelsStopped();
		foreach (ReevaluationPersistentVisualEffect visualEffectReevaluation in visualDataReevaluationForOnReelsStopped)
		{
			scatterSymbolCounter = visualEffectReevaluation.counter;
		}
		Debug.Log("_________Scatter Symbols Count_____"+ scatterSymbolCounter);
		coinTextMeshPro.text = string.Empty + scatterSymbolCounter;
		updataSymbols(scatterSymbolCounter);
		yield break;
	}

	private void activateSymbol(VisualTargetSymbolData data)
    {
		foreach (VisualTargetSymbolData visualTargetSymbolData in visualTargetSymbols)
        {
			visualTargetSymbolData.symbol.SetActive(false);
		}
		data.symbol.SetActive(true);
		StartCoroutine(AudioListController.playListOfAudioInformation(data.audioInformationList));

	}


	private void updataSymbols(int scatterSymbolCounter)
    {
		foreach(VisualTargetSymbolData visualTargetSymbolData in visualTargetSymbols)
        {
			if(visualTargetSymbolData.values.Contains(scatterSymbolCounter) && visualTargetSymbolData.isUsed==false)
            {
				visualTargetSymbolData.isUsed = true;
				activateSymbol(visualTargetSymbolData);
			}
        }

    }
	private List<ReevaluationPersistentVisualEffect> getPersistentVisualEffectReevaluations()
	{
		List<ReevaluationPersistentVisualEffect> persistentVisualEffectReevals = new List<ReevaluationPersistentVisualEffect>();
		JSON[] arrayReevaluations = ReelGame.activeGame.outcome.getArrayReevaluations();
		for (int i = 0; i < arrayReevaluations.Length; i++)
		{
			
			string reevalType = arrayReevaluations[i].getString("type", "");
			if (reevalType == VISUAL_DATA_COUNT_ON_SYMBOL_LAND_KEY || reevalType == VISUAL_DATA_RESET_ON_BONUS_NAME_KEY)
			{
				persistentVisualEffectReevals.Add(new ReevaluationPersistentVisualEffect(arrayReevaluations[i]));
			}
		}

		return persistentVisualEffectReevals;
	}

	private List<ReevaluationPersistentVisualEffect> getVisualDataReevaluationForOnReelsStopped()
	{
		List<ReevaluationPersistentVisualEffect> visualDataReevaluationForOnReelsStopped = new List<ReevaluationPersistentVisualEffect>();

		foreach (ReevaluationPersistentVisualEffect reevaluation in cachedPersistentVisualEffectReevaluations)
		{
			if (reevaluation.type == VISUAL_DATA_COUNT_ON_SYMBOL_LAND_KEY || reevaluation.type == VISUAL_DATA_RESET_ON_BONUS_NAME_KEY)
			{
				visualDataReevaluationForOnReelsStopped.Add(reevaluation);
			}
		}

		return visualDataReevaluationForOnReelsStopped;
	}


	[System.Serializable]
	public class VisualTargetSymbolData
	{
		[Tooltip("symbol to show when meter get values of his ranges")]
		public GameObject symbol;

		[Tooltip("play sounds when meter get values of this ranges")]
		public AudioListController.AudioInformationList audioInformationList;

		[Tooltip("use only one time")]
		public bool isUsed;

		[Tooltip("ranges where this symbol get visible")]
		public List<int> values;


	}
}
