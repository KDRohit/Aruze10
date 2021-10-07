using UnityEngine;
using System.Collections;

public class TWArcSplinePathModule : SlotModule 
{
	protected StandardMutation mutations;
	private StandardMutation stickyMutation;
	private bool isFirstStop = true;

	[SerializeField] private GameObject flyingObject;
	[SerializeField] private GameObject[] flyingObjectPool;
	[SerializeField] private float 		OBJECT_FLYING_TIME = 0.7f;	
	[SerializeField] private float 		OBJECT_LANDING_TIME = 0.7f;	
	[SerializeField] private float 		OBJECT_MUTATE_TIME = 0.7f;	
	[SerializeField] private bool 		skipFirstStop = true;
	[SerializeField] private string FLYING_OBJ_SPLAT_AUDIO_SOUNDKEY = "freespin_wild_lands";   
	[SerializeField] private string FLYING_OBJ_TRAVEL_AUDIO_SOUNDKEY = "freespin_wild_travels";   
	[SerializeField] private float 		FLYING_OBJECT_STAGGER_DELAY = 0.7f;
	[SerializeField] private string     WILD_BG;


	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return true;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		if (WILD_BG != null)
		{
			Audio.switchMusicKeyImmediate(Audio.soundMap(WILD_BG));
		}

		MutationManager mutationManager = reelGame.mutationManager;

		if (mutationManager.mutations.Count > 0)
		{
			// Let's store the main mutation
			mutations = mutationManager.mutations[0] as StandardMutation;
		}

		if (mutations != null )
		{
			if (!isFirstStop || !skipFirstStop)
			{
				yield return StartCoroutine(handleTWSymbols());
			}
			isFirstStop = false;
		}
	}

	protected  IEnumerator tweenTWSymbol(int i, int j)
	{

		// find a free flying symbol
		GameObject go = null;
		while (go == null && flyingObjectPool.Length > 0)
		{
			foreach (GameObject poolObj in flyingObjectPool)
			{
				#pragma warning disable 618 // `UnityEngine.GameObject.active' is obsolete: `GameObject.active is obsolete. Use GameObject.SetActive(), GameObject.activeSelf or GameObject.activeInHierarchy.' (CS0618)
				if (!poolObj.active)
				{
					go = poolObj;
					break;
				}
				#pragma warning restore 618
			}
			yield return null;
		}

		if (go == null)
		{
			go = flyingObject;
		}


		SlotReel[]reelArray = reelGame.engine.getReelArray();

		SlotSymbol symbol = reelArray[i].visibleSymbolsBottomUp[j];
	
		go.transform.localPosition = Vector3.zero;
		go.SetActive(true);
						
		Vector3 startPosition = go.transform.position;
		Vector3 endPosition = symbol.animator.gameObject.transform.position;
		startPosition.z = endPosition.z = 0;  // leave z at zero


		// account for positioning differences in symbols, otherwise we get a position jitter when it mutates/lands.
		SymbolInfo info = reelGame.findSymbolInfo(mutations.triggerSymbolNames[i,j]);
		if (info != null)
		{
			endPosition -= Vector3.Scale(symbol.info.positioning, reelGame.gameScaler.transform.localScale);
			endPosition += Vector3.Scale(info.positioning, reelGame.gameScaler.transform.localScale);
		}

		Spline arcSpline = new Spline();
	
		Vector3 quarterDistance = (endPosition - startPosition) / 4;
		arcSpline.addKeyframe(0, 0, 0, startPosition);
		arcSpline.addKeyframe(20/4, 0.5f, 0, new Vector3(quarterDistance.x + startPosition.x, quarterDistance.y + startPosition.y + 0.3f, startPosition.z));
		arcSpline.addKeyframe((20/4) * 2, 1, 0, new Vector3(quarterDistance.x * 2 + startPosition.x, quarterDistance.y * 2 + startPosition.y + 0.50f, startPosition.z));
		arcSpline.addKeyframe((20/4) * 3, 0.5f, 0, new Vector3(quarterDistance.x * 3 + startPosition.x, quarterDistance.y * 3 + startPosition.y + 0.3f, startPosition.z));
		arcSpline.addKeyframe(20, 0, 0, endPosition);
		arcSpline.update();
	
		float elapsedTime = 0.0f;

		Audio.playSoundMapOrSoundKey(FLYING_OBJ_TRAVEL_AUDIO_SOUNDKEY);


		while (elapsedTime <= OBJECT_FLYING_TIME)
		{
			elapsedTime += Time.deltaTime;
			go.transform.position = arcSpline.getValue(20 * (elapsedTime/OBJECT_FLYING_TIME));

			yield return null;
		}
		
		Audio.playSoundMapOrSoundKey(FLYING_OBJ_SPLAT_AUDIO_SOUNDKEY);

		go.transform.position = endPosition;

		yield return new TIWaitForSeconds(OBJECT_LANDING_TIME); 

		symbol.mutateTo(mutations.triggerSymbolNames[i,j]);

		yield return new TIWaitForSeconds(OBJECT_MUTATE_TIME); 

		go.SetActive(false);

	}	

	protected  IEnumerator handleTWSymbols()
	{
		for (int i = 0; i < mutations.triggerSymbolNames.GetLength(0); i++)
		{
			for (int j = 0; j < mutations.triggerSymbolNames.GetLength(1); j++)
			{
				if (mutations.triggerSymbolNames[i,j] != null && mutations.triggerSymbolNames[i,j] != "")
				{
					if (flyingObjectPool.Length > 0)  // if we have multiple flying objects that means we want to stagger them. storage01 is first to use this
					{
						StartCoroutine(tweenTWSymbol(i,j));
						yield return new TIWaitForSeconds(FLYING_OBJECT_STAGGER_DELAY); 
					}
					else
					{
						// only one flying object at a time
						yield return StartCoroutine(tweenTWSymbol(i,j));
					}
				}
			}
		}
	}
}
