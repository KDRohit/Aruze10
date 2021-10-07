using UnityEngine;
using System.Collections;
using System.Collections.Generic;
/*
	Gen18 basegame class
	Used to handle the choreography of the ambient effects
 */
public class Gen18 : IndependentReelBaseGame {

	protected CoroutineRepeater ambientFireWork1Controller;
	protected CoroutineRepeater ambientFireWork2Controller;
	protected CoroutineRepeater ambientFireWork3Controller;

	[SerializeField] private float MIN_TIME_AMBIENT_WAIT;								// Minimum time pickme animation might take to play next
	[SerializeField] private float MAX_TIME_AMBIENT_WAIT;

	[SerializeField] private float FIREWORK_DURATION;

	[SerializeField] private GameObject ambientFireWork1;
	[SerializeField] private GameObject ambientFireWork2;
	[SerializeField] private GameObject ambientFireWork3;

	protected override void Awake()
	{
		ambientFireWork1Controller = new CoroutineRepeater (MIN_TIME_AMBIENT_WAIT, MAX_TIME_AMBIENT_WAIT, ambientFireWorkCallback1);
		ambientFireWork2Controller = new CoroutineRepeater (MIN_TIME_AMBIENT_WAIT, MAX_TIME_AMBIENT_WAIT, ambientFireWorkCallback2);
		ambientFireWork3Controller = new CoroutineRepeater (MIN_TIME_AMBIENT_WAIT, MAX_TIME_AMBIENT_WAIT, ambientFireWorkCallback3);
		base.Awake ();
	}

	protected override void Update()
	{
		base.Update();
		ambientFireWork1Controller.update();
		ambientFireWork2Controller.update();
		ambientFireWork3Controller.update();
	}

	protected virtual IEnumerator ambientFireWorkCallback1()
	{
		if (ambientFireWork1 != null)
		{
			ambientFireWork1.SetActive(true);
			yield return new TIWaitForSeconds(FIREWORK_DURATION);
			if (ambientFireWork1 != null)
			{
				ambientFireWork1.SetActive(false);
			}
		}
	}

	protected virtual IEnumerator ambientFireWorkCallback2()
	{
		if (ambientFireWork2 != null)
		{
			ambientFireWork2.SetActive(true);
			yield return new TIWaitForSeconds(FIREWORK_DURATION);
			if (ambientFireWork2 != null)
			{
				ambientFireWork2.SetActive(false);
			}
		}
	}

	protected virtual IEnumerator ambientFireWorkCallback3()
	{
		if (ambientFireWork3 != null)
		{
			ambientFireWork3.SetActive(true);
			yield return new TIWaitForSeconds(FIREWORK_DURATION);
			if (ambientFireWork3 != null)
			{
				ambientFireWork3.SetActive(false);
			}
		}
	}



}
