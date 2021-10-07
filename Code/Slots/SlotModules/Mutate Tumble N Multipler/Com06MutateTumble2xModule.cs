using UnityEngine;
using System.Collections;

/**
 * Com06MutateTumble2xModule.cs
 * Author: Nick Reynolds
 * Inherits from MutateTumble2xModule, we simply override one function to handle the punching phantom animation. 
 */ 

public class Com06MutateTumble2xModule : MutateTumbleNxModule 
{
	[SerializeField] private GameObject punchingPhantom;
	[SerializeField] private float PUNCH_WAIT_BEFORE_DESTROY_TIME;
	[SerializeField] private float WIND_UP_WAIT;
	[SerializeField] private float SMASH_WAIT;
	[SerializeField] private float VO_WAIT;
	[SerializeField] private float PRE_PUNCH_WAIT;
	[SerializeField] private float SHAKE_SCREEN_DELAY;
	[SerializeField] private float SHAKE_SCREEN_TIME;
	[SerializeField] private GameObject[] objectsToShake;
	
	// tw constants	
	[SerializeField] private float TW_ANIMATE_DELAY;
	[SerializeField] private float TW_DELAY;
	[SerializeField] private float TW_TURN_WILD_WAIT_1;
	[SerializeField] private float TW_TURN_WILD_ANIM_TIME;

	private const string WIND_UP_SOUND = "TWPhantomWindUp";
	private const string SMASH_SOUND = "TWPhantomSmashPanel";
	private const string ATTACH_SOUND = "TWPhantomTransformSmack";
	private const string POST_SMASH_VO = "TWPhantomVO";
	private const string TW_VO_SOUND = "TWPhantomPreVO";
	private const string TW_SYMBOL_ANIMATE_SOUND = "TWInitFistImpact";
	
	// animation constants
	private const string TW_WIN_ANIM = "fs_tw_anim";

	protected override IEnumerator doChoreographyBeforeAttaching()
	{
		SlotSymbol twSymbol = getTWSymbol();
		StartCoroutine(doSpecialTWAnims(twSymbol));

		yield return new TIWaitForSeconds(PRE_PUNCH_WAIT);
		punchingPhantom.SetActive(true);
		punchingPhantom.transform.position = twSymbol.animator.transform.position;
		punchingPhantom.GetComponent<Animator>().Play("anim");
		
		StartCoroutine(reelGame.waitThenDeactivate(punchingPhantom, PUNCH_WAIT_BEFORE_DESTROY_TIME));

		yield return new TIWaitForSeconds(WIND_UP_WAIT);
		Audio.play (WIND_UP_SOUND);
		yield return new TIWaitForSeconds(SMASH_WAIT);
		Audio.play (SMASH_SOUND);
		Audio.play (POST_SMASH_VO, 1.0f, 0.0f, VO_WAIT);

		StartCoroutine(shakeTheScreen());
	}

	// animate the TW symbol when it lands
	private IEnumerator doSpecialTWAnims(SlotSymbol twSymbol)
	{
		Audio.play (TW_VO_SOUND);
		yield return new TIWaitForSeconds(TW_DELAY);
		
		twSymbol.animateOutcome();
		
		yield return new TIWaitForSeconds(TW_ANIMATE_DELAY);
		
		Audio.play (TW_SYMBOL_ANIMATE_SOUND);
		
		yield return new TIWaitForSeconds(TW_TURN_WILD_WAIT_1);
		Vector3 localPos = twSymbol.transform.localPosition;

		if (twSymbolAttachment != null)
		{
			twSymbolAttachment.transform.parent = gameObject.transform;
		}
		
		twSymbol.mutateTo("TW1", null, false, true, false);
		twSymbol.name = "TW";
		twSymbol.transform.localPosition = localPos;
		twSymbol.animateOutcome();
		
		yield return new TIWaitForSeconds(TW_TURN_WILD_ANIM_TIME);
		

		twSymbol.mutateTo("TW2", null, false, true, false);
		twSymbol.name = "TW";
		twSymbol.transform.localPosition = localPos;
		if (twSymbolAttachment != null)
		{
			twSymbolAttachment.transform.parent = twSymbol.transform;
			Animator anim = twSymbolAttachment.GetComponent<Animator>();
			anim.speed = 1.0f;
			anim.Play (0);
		}
	}


	protected override IEnumerator attachMultiplier(int i, int j)
	{
		Audio.play (ATTACH_SOUND);
		yield return StartCoroutine(base.attachMultiplier(i,j));
	}

	private IEnumerator shakeTheScreen()
	{		
		yield return new TIWaitForSeconds(SHAKE_SCREEN_DELAY);
		TICoroutine shakeCoroutine = StartCoroutine(CommonEffects.shakeScreen(objectsToShake, .5f, .5f));

		yield return new TIWaitForSeconds(SHAKE_SCREEN_TIME);
		shakeCoroutine.finish();

		foreach (GameObject go in objectsToShake)
		{
			go.transform.localEulerAngles = Vector3.zero;
		}
	}
}
