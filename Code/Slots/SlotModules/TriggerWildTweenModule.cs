using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TriggerWildTweenModule : TriggerPickemModule
{
	[SerializeField] private GameObject targetObject;
	[SerializeField] private GameObject tweenObjectPrefab;
	[SerializeField] float delay = 1f;
	[SerializeField] float waitForTween = 0f;
	//feature reel not stated in mutation so setting it here
	private List<GameObject> symbolObjList = new List<GameObject>();
	private GameObjectCacher tweenObjectCacher = null;
	private GameObject tweenObject = null;

	public override void Awake()
	{
		base.Awake();
		if (tweenObjectPrefab != null)
		{
			tweenObjectCacher = new GameObjectCacher(this.gameObject, tweenObjectPrefab);
		}
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		SlotReel[] reelArray = reelGame.engine.getReelArray();
		for (int i = 0; i < reelArray.Length; i++)
		{
			SlotReel featureReel = reelGame.engine.getSlotReelAt(i);
			List<SlotSymbol> symbols = featureReel.visibleSymbolsBottomUp;
			for (int j = 0; j < symbols.Count; j++)
			{
				if (symbols[j].animator != null && symbols[j].animator.symbolInfoName == "TW")
				{
					GameObject symbolObj = symbols[j].gameObject;
					StartCoroutine(startTween(symbolObj));
					
					symbolObjList.Add(symbolObj);
					
				}
			}

		}
		yield return StartCoroutine(base.executeOnReelsStoppedCallback());
	}

	private IEnumerator startTween(GameObject symbolObj)
	{
		symbolObj.SetActive(false);

		tweenObject = tweenObjectCacher.getInstance();
		tweenObject.SetActive(true);
		tweenObject.transform.parent = symbolObj.transform.parent;
		tweenObject.transform.position = symbolObj.transform.position;
			
		Vector3 targetPos = targetObject.transform.position;
		
		// If the tween object and the target position aren't in the same coordinate systems,
		// then transform the coordinates.

		if (tweenObject.layer != targetObject.layer)
		{
			int tweenMask = 1 << tweenObject.layer;
			Camera tweenCamera = CommonGameObject.getCameraByBitMask(tweenMask); 

			int targetMask = 1 << targetObject.layer;
			Camera targetCamera = CommonGameObject.getCameraByBitMask(targetMask); 
			
			Vector3 worldPos = targetCamera.WorldToScreenPoint(targetPos);
			targetPos = tweenCamera.ScreenToWorldPoint(worldPos);
		}

		iTween.MoveTo(tweenObject, iTween.Hash("position", targetPos, "isLocal", false, "delay", delay));
		yield return new TIWaitForSeconds(waitForTween);
		
		tweenObjectCacher.releaseInstance(tweenObject);
	}
	
	protected override IEnumerator endMiniPick()
	{
		foreach(GameObject symbolObj in symbolObjList)
		{
			symbolObj.SetActive(true);
		}
		symbolObjList.Clear();
		
		yield return StartCoroutine(base.endMiniPick());
	}
}
