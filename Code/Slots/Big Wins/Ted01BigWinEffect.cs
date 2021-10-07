using UnityEngine;
using System.Collections;

public class Ted01BigWinEffect : BigWinEffect {

	public GameObject tedShadow; // need to fade in shadow by itself, since it has a different final alpha value

	// Use this for initialization
	void Awake()
	{
		base.setupBigWin();

		CommonGameObject.alphaGameObject(tedShadow, 0);
		
		iTween.ValueTo(this.gameObject,
		    iTween.Hash(
				"from", 0f,
				"to", .25f,
				"time", 1f,
				"onupdate", "updateShadowFade"
				)
			);
	}

	// stop all particle effects and fade all the materials
	public override void rollupComplete()
	{
		base.rollupComplete();
		iTween.ValueTo(this.gameObject,
		    iTween.Hash(
				"from", .25f,
				"to", 0f,
				"time", 1f,
				"onupdate", "updateShadowFade"
				)
			);
	}
	
	// fades out all the materials and labels
	public void updateShadowFade(float value)
	{
		CommonGameObject.alphaGameObject(tedShadow, value);
	}
	
	// Update is called once per frame
}
