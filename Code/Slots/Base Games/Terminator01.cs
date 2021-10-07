using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Terminator01 : SlotBaseGame
{
	private List<SymbolAnimator> mutationWildSymbols = new List<SymbolAnimator>();
	private Dictionary<SymbolAnimator, SymbolInfo> mutatedSymbols = new Dictionary<SymbolAnimator, SymbolInfo>();
	public Transform targetor;
	public Color targetorModulationColor;
	public MeshRenderer targetorOverlay;
	public GameObject targetorExplosionPrefab;
	private bool infraredMode;

	protected override void Awake()
	{
		base.Awake();
	}

	// If the spin is not from a swipe then the direction is always going to be down, if it is from a spin then all reels start at the same time.
	public override bool validateSpin(bool forcedOutcome = false, bool isFromSwipe = false, SlotReel.ESpinDirection direction = SlotReel.ESpinDirection.Down)
	{		
		bool returnVal = base.validateSpin(forcedOutcome,isFromSwipe,direction);
		
		resetMutatedSymbols();
		
		return returnVal;
	}
	
	protected override void reelsStoppedCallback()
	{
		base.reelsStoppedCallback();
		
		setInfrarredMode(false);
		clearMutationWildSymbols();	
	}

	/// slotOutcomeEventCallback - after a spin occurs, Server calls this with the results.
	protected override void slotOutcomeEventCallback(JSON data)
	{
		if (isSpinTimedOut)
		{
			// Not matching base class because this class has many changes to it.
			return;
		}

		// cancel the spin timeout since we recieved a response from the server
		setIsCheckingSpinTimeout(false);

		base.setOutcome(data);
		
		if (mutationManager.mutations.Count > 0)
		{
			this.StartCoroutine(this.doInfrarredWilds());
		}
		else
		{
			this.setEngineOutcome(_outcome);
		}

		// IF ANYONE ELSE DOES THIS I WILL HUNT YOU DOWN AND HURT YOU
		// Usually it is done in the base class!  But there is no call to 
		// the base class here so I have to jam the code in here too.
		// This is how we measure the timing between requesting a spin from
		// the server and receieving it
#if ZYNGA_TRAMP
		AutomatedPlayer.spinReceived();
#endif
	}
	
	protected override void handleSymbolAnimatorCreated(SymbolAnimator animator)
	{
		base.handleSymbolAnimatorCreated(animator);
		animator.material.color = infraredMode ? targetorModulationColor : Color.white;
	}
	
	private IEnumerator doInfrarredWilds()
	{
		StandardMutation currentMutation = mutationManager.mutations[0] as StandardMutation;
		Transform reelRoot;
		SymbolAnimator symbolAnimator;
		
		//Turn on Infrared
		this.setInfrarredMode(true);
		
		PlayingAudio ambience = Audio.play("AmbienceInfraRed", 1, 0, 0, 99);
		
		//Position Targetor in middle cell
		reelRoot = getReelRootsAt(2).transform;
		this.targetor.parent = reelRoot;
		this.targetor.localPosition = (Vector3.up * (getSymbolVerticalSpacingAt(2) * (2/*row */- 1)));
		
		//Wait
		yield return new WaitForSeconds(0.25f);

		foreach (KeyValuePair<int, int[]> mutationKvp in currentMutation.singleSymbolLocations)
		{
			reelRoot = getReelRootsAt(mutationKvp.Key - 1).transform;
			foreach (int row in mutationKvp.Value)
			{
				symbolAnimator = getSymbolAnimatorInstance("WD");
				symbolAnimator.material.shader = SymbolAnimator.defaultShader("Unlit/GUI Texture (+100)");
				symbolAnimator.material.color = this.targetorModulationColor;
				symbolAnimator.material.renderQueue = 3001;
				symbolAnimator.transform.parent = reelRoot;
				symbolAnimator.scaling = Vector3.one;
				symbolAnimator.positioning = new Vector3(0, (row - 1) * getSymbolVerticalSpacingAt(mutationKvp.Key - 1), 0);
				symbolAnimator.gameObject.name += "test_" + mutationKvp.Key + "_" + row;
				mutationWildSymbols.Add(symbolAnimator);
				
				//position tween
				Audio.play("InfraRedTargetFire");
				this.targetor.parent = reelRoot;				
				Hashtable tween = iTween.Hash("position", (Vector3.up * (getSymbolVerticalSpacingAt(mutationKvp.Key - 1) * (row - 1))), "isLocal", true, "time", 0.25f);
				yield return new TITweenYieldInstruction(iTween.MoveTo(this.targetor.gameObject, tween));
				
				GameObject explosion = CommonGameObject.instantiate(this.targetorExplosionPrefab) as GameObject;
				explosion.transform.parent = this.transform;
				explosion.transform.position = this.targetor.position;
			}
		}
		
		yield return new WaitForSeconds(0.25f);
		this.setInfrarredMode(false);
		
		// Stop the ambience.
		Audio.stopSound(ambience);
		
		this.setEngineOutcome(_outcome);
	}
	
	private void setInfrarredMode(bool on)
	{
		this.infraredMode = on;
		this.targetor.gameObject.SetActive(on);
		this.targetorOverlay.material.color = on ? this.targetorModulationColor : Color.white;
		SlotReel[] reelArray = engine.getReelArray();

		for (int i = 0 ; i < reelArray.Length ; i++)
		{
			foreach (SlotSymbol symbol in reelArray[i].symbolList)
			{
				symbol.animator.material.color = on ? targetorModulationColor : Color.white;
			}
		}
		foreach(SymbolAnimator animator in this.mutationWildSymbols)
		{
			animator.material.color = on ? targetorModulationColor : Color.white;
		}
		IEnumerable<SymbolAnimator> animators = this.getAllCachedSymbolAnimators();
		foreach (SymbolAnimator animator in animators)
		{
			animator.material.color = on ? targetorModulationColor : Color.white;
		}
	}

	private void clearMutationWildSymbols()
	{
		//Mutate Symbols
		if (mutationManager != null && mutationManager.mutations != null && mutationManager.mutations.Count > 0)
		{
			StandardMutation currentMutation = mutationManager.mutations[0] as StandardMutation;
			SlotReel[] reelArray = engine.getReelArray();

			foreach (KeyValuePair<int, int[]> mutationKvp in currentMutation.singleSymbolLocations)
			{
				int reel = mutationKvp.Key - 1;
				foreach (int row in mutationKvp.Value)
				{
					SlotSymbol symbol = reelArray[reel].visibleSymbolsBottomUp[row - 1];
					mutatedSymbols.Add (symbol.animator, symbol.animator.info);
					symbol.animator.info = mutationWildSymbols[0].info;
					symbol.animator.material.SetTexture("_MainTex", mutationWildSymbols[0].material.GetTexture("_MainTex"));
				}
			}
		}
		
		for (int i = 0; i < mutationWildSymbols.Count; i++)
		{
			this.releaseSymbolInstance(mutationWildSymbols[i]);
		}
		mutationWildSymbols.Clear();
	}
	
	private void resetMutatedSymbols()
	{
		foreach(KeyValuePair<SymbolAnimator, SymbolInfo> pair in this.mutatedSymbols)
		{
			pair.Key.info = pair.Value;
		}
		this.mutatedSymbols.Clear();
	}
}
