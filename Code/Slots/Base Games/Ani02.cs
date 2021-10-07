using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Ani02 : SlotBaseGame
{
	public RhinoInfo rhinoInfo;
	public GameObject[] objectsToShake;

	const string RHINO_PRE_STAMPEDE = "TWPreStampedeRhino";
	const string RHINO_LEAD_IN = "TWLeadInVO";
	const string RHINO_STAMPEDE = "TWStampedeRhino";
	const string RHINO_TRANSFORM_SYMBOL = "TWSymbolTransformsRhino";
	const string RHINO_FINALE = "TWFinaleRhino";
	const string RHINO_LEAD_OUT = "TWLeadOutVO";

	const float X_SHAKE_MOVEMENT = 0.4f;
	const float Y_SHAKE_MOVEMENT = 0.4f;
	const float AUDIO_LEAD_IN_WAIT_TIME = 0.25f;
	const float RHINO_SHAKE_TIME = 1.0f;

	protected override void reelsStoppedCallback()
	{
		mutationManager.setMutationsFromOutcome(outcome.getJsonObject());
		this.StartCoroutine(Ani02.doRhinoWilds(this, rhinoInfo, base.reelsStoppedCallback, objectsToShake));
	}
	
	public static IEnumerator doRhinoWilds(ReelGame reelGame, RhinoInfo rhinoInfo, GenericDelegate callback, GameObject[] objectsToShake)
	{
		if (reelGame.mutationManager.mutations.Count == 0)
		{
			// No mutations, so do nothing special.
			callback();
			yield break;
		}
	
		SlotEngine engine = reelGame.engine;
		StandardMutation currentMutation = reelGame.mutationManager.mutations[0] as StandardMutation;
		TICoroutine shakeCoroutine = reelGame.StartCoroutine(CommonEffects.shakeScreen(objectsToShake, X_SHAKE_MOVEMENT, Y_SHAKE_MOVEMENT));
				
		//Wait
		Audio.play(RHINO_PRE_STAMPEDE);
		Audio.play(RHINO_LEAD_IN);
		yield return new WaitForSeconds(AUDIO_LEAD_IN_WAIT_TIME);
		
		Audio.play(RHINO_STAMPEDE);
		for (int i = 0; i < currentMutation.triggerSymbolNames.GetLength(0); i++)
		{
			for (int j = 0; j < currentMutation.triggerSymbolNames.GetLength(1); j++)
			{
				if (currentMutation.triggerSymbolNames[i,j] != null && currentMutation.triggerSymbolNames[i,j] != "")
				{
					GameObject rhino = CommonGameObject.instantiate(rhinoInfo.rhinoStampedeTemplate) as GameObject;
					if (rhino != null)
					{
						rhino.transform.parent = reelGame.getReelRootsAt(i).transform;

						Vector3 rhinoEndPoint = Vector3.up * reelGame.getSymbolVerticalSpacingAt(i) * j;
						rhinoEndPoint.y -= 0.75f;

						Vector3 rhinoOffScreenPoint = Vector3.up * reelGame.getSymbolVerticalSpacingAt(i) * j;
						rhinoOffScreenPoint.y -= 0.75f;
						rhinoOffScreenPoint.x -= 12.0f;

						// set the rhino off to the right of the screen
						rhino.transform.position = rhinoEndPoint + new Vector3(12.0f, -1.25f, 0.0f);

						Hashtable tween = iTween.Hash("position", rhinoEndPoint, "isLocal", true, "speed", rhinoInfo.speed, "easetype", iTween.EaseType.linear);
						Hashtable tween2 = iTween.Hash("position", rhinoOffScreenPoint, "isLocal", true, "speed", rhinoInfo.speed, "easetype", iTween.EaseType.linear);

						yield return new TITweenYieldInstruction(iTween.MoveTo(rhino, tween));
						SlotSymbol symbol = engine.getVisibleSymbolsBottomUpAt(i)[j];

						Audio.play(RHINO_TRANSFORM_SYMBOL);

						reelGame.StartCoroutine(Ani02.startSmokeCloud(rhinoInfo.rhinoStampedeExplodeTemplate, symbol, i, RHINO_SHAKE_TIME));

						symbol.mutateTo("TW-WD");

						yield return new TITweenYieldInstruction(iTween.MoveTo(rhino, tween2));

						Destroy(rhino);
					}
				}
			}
		}

		SlotReel[] reelArray = engine.getReelArray();
		for (int i = 0; i < reelArray.Length; i++)
		{
			// There is at least one symbol to change in this reel.
			SlotReel reel = reelArray[i];

			List<SlotSymbol> symbols = reel.visibleSymbolsBottomUp;

			for (int j = 0; j < symbols.Count; j++)
			{
				if (symbols[j].animator != null && symbols[j].animator.symbolInfoName== "TW")
				{
					symbols[j].mutateTo("TW-WD");
				}
			}
		}

		Audio.play(RHINO_FINALE);
		if (shakeCoroutine != null)
		{
			// Stop the coroutine
			shakeCoroutine.finish();
			// Put everything back into the orginal position.
			foreach (GameObject go in objectsToShake)
			{
				go.transform.localEulerAngles = Vector3.zero;
			}
		}
		yield return new WaitForSeconds(0.25f);
		Audio.play(RHINO_LEAD_OUT);
		
		callback();
	}

	private static IEnumerator startSmokeCloud(GameObject templateCloud, SlotSymbol symbol, int column, float timeToDelay)
	{
		GameObject rhinoExplode = CommonGameObject.instantiate(templateCloud) as GameObject;
		if (rhinoExplode != null)
		{
			rhinoExplode.transform.position = symbol.gameObject.transform.position;
		}

		yield return new TIWaitForSeconds(timeToDelay);

		Destroy(rhinoExplode);
	}

	// Basic data structure used on both base game and free spins game.
	[System.Serializable]
	public class RhinoInfo
	{
		public GameObject rhinoStampedeTemplate;			
		public GameObject rhinoStampedeExplodeTemplate;		
		public float speed = 12.0f;
	}
}
