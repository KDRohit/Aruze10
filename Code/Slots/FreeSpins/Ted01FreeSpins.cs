using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Ted01FreeSpins : FreeSpinGame {

	public GameObject[] wildLights;
	public UITexture[] topWildOverlays;
	public Animator[] topWildAnimators;
    public GameObject trailEffect;

    //Speed at which the trail moves from the reels to the light at the top of the game
    public float tweenSpeed = 1500.0f;

    public GameObject boxPresenterEffect;

    public Camera reelCamera;

	private int lightIndex = 0;
	private string[] possibleWilds = { "M1", "M2", "M3", "M4"};
    private Camera nguiCamera;

    //HANS - This seems a little hacky, may want to consider reworking the background and the effect so the ted images
    //are seperate and the box effect can just be parented to which needs to be highlighted (would require the children
    //particle effects to be properly space in the parent), however for expediency's sake this works.
    private float[] boxPresenterXValues = { 38.6f, 41.2f, 43.8f, 46.5f };

	private int lastLightIndex = 0;
	
	public const string TOP_WILD_ANIM_NAME = "reveal";
	
	public override void initFreespins()
	{
		base.initFreespins();
        
        //Find the ngui camera
        int layerMask = 1 << wildLights[0].layer;
        nguiCamera = CommonGameObject.getCameraByBitMask(layerMask);
        
		foreach (GameObject obj in wildLights)
		{
			obj.SetActive(false);
		}

		foreach(UITexture tex in topWildOverlays)
		{
			tex.enabled = false;
		}
	}
	
	private IEnumerator playReelsStopped()
	{
		// Handle Thunder Wilds.
		if (engine.getSymbolCount("TW") > 0 && lightIndex < wildLights.Length)
		{
            List<SlotSymbol> twSymbols = new List<SlotSymbol>();
			SlotReel[] reelArray = engine.getReelArray();

            foreach (SlotSymbol ss in reelArray[4].visibleSymbols)
            {
                if (ss.name.Contains("TW"))
                {
                    twSymbols.Add(ss);
                }
            }

			Audio.play("TedScaredThunderVO");
            for (int i = 0; i < twSymbols.Count; i++)
			{
				if (lightIndex < 8) // we only have 8 lights, so don't let it try to light more than 8
				{
	                //Set starting point
	                Vector3 startPos = reelCamera.WorldToViewportPoint(twSymbols[i].animator.gameObject.transform.position);
	                //Convert it into the NGUI camera space
	                startPos = nguiCamera.ViewportToWorldPoint(new Vector3(startPos.x,startPos.y, 0));

	                //Set end point
	                Vector3 endPos = wildLights[lightIndex].transform.localPosition;
	                
	                //Position the trail effect
	                trailEffect.transform.position = startPos;

	                //Turn it on
	                trailEffect.SetActive(true);
	                
	                //Tween to the light!
	                Hashtable tween = iTween.Hash("position", endPos, "isLocal", true, "speed", tweenSpeed, "easetype", iTween.EaseType.linear);
					Audio.play("value_move");
	                yield return new TITweenYieldInstruction(iTween.MoveTo(trailEffect, tween));
					Audio.play("value_land");
					//slight extra delay so the particle trail can catch up, and then we carry on
	                yield return new WaitForSeconds(0.75f);

	                //Turn off trail
	                trailEffect.SetActive(false);

	                //Turn on the light
	                wildLights[lightIndex].SetActive(true);

					//Position the highlight box effect
					if (boxPresenterEffect != null)
					{
						boxPresenterEffect.transform.position = new Vector3
						(boxPresenterXValues[lightIndex / 2],
						boxPresenterEffect.transform.position.y,
						boxPresenterEffect.transform.position.z);
						
						//Turn on the highlight
						boxPresenterEffect.SetActive(true);
					}

					lightIndex++;
					if (lightIndex == 2 && !permanentWildReels.Contains("M1"))
					{
						if (topWildOverlays.Length > 0)
						{
							topWildOverlays[0].enabled = true;
							topWildOverlays[0].gameObject.SetActive(true);
						}
						
						if (topWildAnimators.Length > 0)
						{
							topWildAnimators[0].Play(TOP_WILD_ANIM_NAME);
						}
						
						permanentWildReels.Add("M1");
						Audio.play("TedWildLightningStrike");
	                }
	                
					if (lightIndex == 4 && !permanentWildReels.Contains("M2"))
					{
						if (topWildOverlays.Length > 1)
						{
							topWildOverlays[1].enabled = true;
							topWildOverlays[1].gameObject.SetActive(true);
						}
						
						if (topWildAnimators.Length > 1)
						{
							topWildAnimators[1].Play(TOP_WILD_ANIM_NAME);
						}
						
						permanentWildReels.Add("M2");        
						Audio.play("TedWildLightningStrike");
					}
					
					if (lightIndex == 6 && !permanentWildReels.Contains("M3"))
					{
						if (topWildOverlays.Length > 2)
						{
							topWildOverlays[2].enabled = true;
							topWildOverlays[2].gameObject.SetActive(true);
						}
						
						if (topWildAnimators.Length > 2)
						{
							topWildAnimators[2].Play(TOP_WILD_ANIM_NAME);
						}
						
						permanentWildReels.Add("M3");        
						Audio.play("TedWildLightningStrike");
					}
					
					if (lightIndex == 8 && !permanentWildReels.Contains("M4"))
					{
						if (topWildOverlays.Length > 3)
						{
							topWildOverlays[3].enabled = true;
							topWildOverlays[3].gameObject.SetActive(true);
						}
						
						if (topWildAnimators.Length > 3)
						{
							topWildAnimators[3].Play(TOP_WILD_ANIM_NAME);
						}
						
						permanentWildReels.Add("M4");
						Audio.play("TedWildLightningStrike");
					}		
				}
			}
			yield return new WaitForSeconds(1);
			//yield return StartCoroutine();

            //Turn of the highlight box
            if (boxPresenterEffect != null)
            {
            	boxPresenterEffect.SetActive(false);
            }
		}

		if (lastLightIndex != lightIndex)
		{
			if (lightIndex == 2)
			{
				showWilds(possibleWilds[0], 0);
			}
			if (lightIndex == 4)
			{
				showWilds(possibleWilds[1], 0);
			}
			if (lightIndex == 6)
			{
				showWilds(possibleWilds[2], 0);
			}
			if (lightIndex == 8)
			{
				showWilds(possibleWilds[3], 0);
			}
		}
		lastLightIndex = lightIndex;
		
		base.reelsStoppedCallback();
	}

	/// reelsStoppedCallback - called when all reels have come to a stop.
	override protected void reelsStoppedCallback()
	{
		StartCoroutine(playReelsStopped());
	}

	public override SymbolAnimator getSymbolAnimatorInstance(string name, int columnIndex = -1, bool forceNewInstance = false, bool canSearchForMegaIfNotFound = false)
	{
		if (columnIndex > 1)
		{
			if (lightIndex > 1)
			{
				if (name == "M1")
				{
					name += "_WILD";
				}
			}
			if (lightIndex > 3)
			{
				if (name == "M2")
				{
					name += "_WILD";
				}
			}
			if (lightIndex > 5)
			{
				if (name == "M3")
				{
					name += "_WILD";
				}
			}
			if (lightIndex > 7)
			{
				if (name == "M4")
				{
					name += "_WILD";
				}
			}
		}
		return base.getSymbolAnimatorInstance(name, columnIndex, forceNewInstance);
	}
}
