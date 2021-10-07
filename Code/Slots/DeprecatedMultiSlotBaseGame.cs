using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DeprecatedMultiSlotBaseGame : LayeredSlotBaseGame
{
	protected string foregroundReelPaytableName = "";

	public GameObject getReelRootAtLayer(int rootIndex, int layerIndex)
	{
		return (engine as MultiSlotEngine).reelLayers[layerIndex].getReelArray()[rootIndex].getReelGameObject();
	}

	protected override void handleSetReelSet(string reelSetKey)
	{
			foreach (JSON info in reelInfo)
			{
				string type = info.getString("type", "");
				if (type == "foreground" || type == "background")
				{
					// Get the reel set
					// int multiGameId = info.getInt("multi_game_id", -1); // These types of multi games use z_index instead of multi_game_ids due to some web limitation.
					int z_index = info.getInt("z_index", -1);
					// New way the data is set up: (Changing to this in the update after 3/3/2016)
					if (z_index == 1) // some games (hi03) have different paytables for forward reels that we need to know
					{
						string paytableName = info.getString("pay_table", "No Paytable");
						if (string.IsNullOrEmpty(foregroundReelPaytableName))
						{
							foregroundReelPaytableName = paytableName;
						}
						else
						{
							Debug.LogWarning("Trying to set foregroundReelPaytableName with " + paytableName + ", but it's already been set to " + foregroundReelPaytableName + ".");
						}
					}
					// Old way the data was set up:
					if (type == "foreground") // some games (hi03) have different paytables for forward reels that we need to know
					{
						string paytableName = info.getString("pay_table", "No Paytable");
						if (string.IsNullOrEmpty(foregroundReelPaytableName))
						{
							foregroundReelPaytableName = paytableName;
						}
						else
						{
							Debug.LogWarning("Trying to set foregroundReelPaytableName with " + paytableName + ", but it's already been set to " + foregroundReelPaytableName + ".");
						}
					}
				}
			}
			base.handleSetReelSet(reelSetKey);
	}
	
	public GameObject getReelRootsAtAllLayers(int index)
	{
		int length = 0;
		int previousLength = 0;
		foreach(ReelLayer layer in (engine as MultiSlotEngine).reelLayers)
		{
			SlotReel[] reelArray = layer.getReelArray();

			length += reelArray.Length;
			if (index < length)
			{
				return reelArray[index - previousLength].getReelGameObject();
			}
			previousLength = length;
		}
		return null;
	}
	
	public override ReelLayer getReelLayerAt(int index)
	{
		foreach (ReelLayer layer in reelLayers)
		{
			if (layer.layer == index)
			{
				return layer;
			}
		}
		Debug.LogWarning("Couldn't find a layer with value " + index);
		return null;
	}
	
	protected override void setEngine()
	{
		engine = new MultiSlotEngine(this);
	}

	public override bool isGameWithSyncedReels()
	{
		return false;
	}
	
	protected override void Awake()
	{
		base.Awake();
		// Make all of the reelLayers into SlidingLayers
		for (int i = 0; i < reelLayers.Length; i++)
		{
			reelLayers[i] = new MultiSlotLayer(reelLayers[i]);
		}
	}
	
	/// Shows the layered bonus outcomes (or regular non-bonus outcomes if there are no bonus outcomes left)
	public override void showNonBonusOutcomes()
	{		
		if (layeredBonusOutcomes.Count > 0)
		{
			StartCoroutine(animateBonusSymbolsThenStartBonus());
		}
		else
		{
			base.showNonBonusOutcomes();
		}
	}
	
	public override SlotOutcome getBonusOutcome(bool shouldRemoveLayeredOutcome = false)
	{
		SlotOutcome bonusOutcome = null;
		if (_outcome.isBonus && !isBaseBonusOutcomeProcessed)
		{
			bonusOutcome = _outcome;
			isBaseBonusOutcomeProcessed = true;
		}
		else if (layeredBonusOutcomes.Count > 0)
		{
			bonusOutcome = layeredBonusOutcomes[0];
			layeredBonusOutcomes.Remove(bonusOutcome);
		}
		else
		{
			Debug.LogError("Bonus game processing is broken");
		}
		setupBonusOutcome(bonusOutcome);
		return bonusOutcome;
	}
	
	// for multi games we need to process the bonus outcome before going into it
	public override void setupBonusOutcome(SlotOutcome bonusOutcome)
	{
		bonusOutcome.processBonus();
	}
	
	private IEnumerator animateBonusSymbolsThenStartBonus()
	{
		yield return null;
		yield return null;
		yield return null;
		yield return StartCoroutine(playBonusAcquiredEffectsByLayer(layeredBonusOutcomes[0].layer));
		layeredBonusOutcomes[0].processBonus();
		startBonus();
	}
	
	/**
	Function to play the bonus acquired effects (bonus symbol animaitons and an audio 
	appluase for getitng the bonus), can be overridden to handle games that need or 
	want to handle this bonus transition differently
	*/
	protected virtual IEnumerator playBonusAcquiredEffectsByLayer(int layer)
	{
		yield return StartCoroutine(engine.playBonusAcquiredEffects(layer));
	}
}