using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DuckDyn01 : SlotBaseGame
{
	public DuckInfo duckInfo;
	
	protected override void reelsStoppedCallback()
	{
		this.StartCoroutine(DuckDyn01.doDuckWilds(this, duckInfo, base.reelsStoppedCallback));
	}
	
	// This function handles the ducks flying in and mutating symbols into wilds.
	// It is used by both the base game and the free spins game, so it is a static
	// function that passes in the game (base or free spins).
	public static IEnumerator doDuckWilds(ReelGame reelGame, DuckInfo duckInfo, GenericDelegate callback)
	{
		if (reelGame.mutationManager.mutations.Count == 0)
		{
			// No mutations, so do nothing special.
			callback();
			yield break;
		}
	
		Audio.play("TriggerWildTaDa");

		SlotEngine engine = reelGame.engine;
		StandardMutation currentMutation = reelGame.mutationManager.mutations[0] as StandardMutation;
		Vector3 duckEndPoint, duckLandingPoint;

		// First mutate any TW symbols to TWWD (the duck call wild).
		SlotReel[] reelArray = engine.getReelArray();
		for (int i = 0; i < reelArray.Length; i++)
		{
			// There is at least one symbol to change in this reel.
			List<SlotSymbol> symbols = engine.getVisibleSymbolsBottomUpAt(i);
	
			for (int j = 0; j < symbols.Count; j++)
			{
				if (symbols[j].animator != null && symbols[j].animator.symbolInfoName == "TW")
				{
					symbols[j].mutateTo("TWWD", null, true);
				}
			}
		}
				
		//Wait
		yield return new WaitForSeconds(0.25f);// .25 for now...
		
		for (int i = 0; i < currentMutation.triggerSymbolNames.GetLength(0); i++)
		{
			for (int j = 0; j < currentMutation.triggerSymbolNames.GetLength(1); j++)
			{
				if (currentMutation.triggerSymbolNames[i,j] != null && currentMutation.triggerSymbolNames[i,j] != "")
				{
					// create a copy of the duck
					GameObject flyingDuck = CommonGameObject.instantiate(duckInfo.flyingDuckTemplate) as GameObject;
					if (flyingDuck != null)
					{
						flyingDuck.transform.parent = reelGame.getReelRootsAt(i).transform;
						// set the duck off to the left or right of the screen depending on the target column
						flyingDuck.transform.position = i < 3 ? duckInfo.leftDuckStart.transform.position : duckInfo.rightDuckStart.transform.position;

						// set the direction the duck is facing and its layer
						flyingDuck.transform.localRotation = Quaternion.identity;
						flyingDuck.transform.localScale = new Vector3(0.004f * (i < 3 ? 1.0f : -1.0f), 0.004f, 1.0f);
						flyingDuck.SetActive(true);
						CommonGameObject.setLayerRecursively(flyingDuck, Layers.ID_SLOT_FRAME);

						// set the end point of the duck, altered slightly for a slowing effect with the landing animation
						duckEndPoint = Vector3.up * reelGame.getSymbolVerticalSpacingAt(i) * j;
						duckEndPoint.y += 0.45f + 0.5f;
						duckEndPoint.x += 1f * (i < 3 ? -duckInfo.ducklandingOffset : duckInfo.ducklandingOffset);

						Hashtable tween = iTween.Hash("position", duckEndPoint, "isLocal", true, "speed", duckInfo.duckTweenSpeed, "easetype", iTween.EaseType.linear);

						// Setup the landing tween
						duckLandingPoint = Vector3.up * reelGame.getSymbolVerticalSpacingAt(i) * j;
						duckLandingPoint.y += 0.45f;
						Hashtable tween2 = iTween.Hash("position", duckLandingPoint, "isLocal", true, "speed", duckInfo.duckTweenSpeed * 0.75, "easetype", iTween.EaseType.easeOutCirc);

						Audio.play("TriggerWildDuck");

						// Tween the duck into position
						yield return new TITweenYieldInstruction(iTween.MoveTo(flyingDuck, tween));

						Audio.play("DuckFlockFlap");

						// Start the duck landing
						Animator animator = flyingDuck.GetComponent<Animator>();
						if (animator != null)
						{
							animator.Play("duck_land");
						}
						iTween.MoveTo(flyingDuck, tween2);
						yield return new WaitForSeconds(duckInfo.duckLandingTime);

						Audio.play("WildDuckLands");
						Destroy(flyingDuck);
					}
										
					SlotSymbol symbol = engine.getVisibleSymbolsBottomUpAt(i)[j];
					symbol.mutateTo(currentMutation.triggerSymbolNames[i,j]);
				}
			}
		}
		
		yield return new WaitForSeconds(0.25f);
		
		callback();
	}

	// Basic data structure used on both base game and free spins game.
	[System.Serializable]
	public class DuckInfo
	{
		public GameObject flyingDuckTemplate;				// template to clone for ducks flying in
		public GameObject leftDuckStart, rightDuckStart;	// off screen starting locations of the ducks
		public float duckTweenSpeed = 12.0f;				// speed at which the ducks initially come in
		public float duckLandingTime = 1.25f;				// time lenght spent in the landing animation
		public float ducklandingOffset = 8.0f;				// how far from the ducks end point to transition to the landing animation
	}
}
