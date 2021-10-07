using UnityEngine;
using System.Collections;

/**
 Base abstract class for all types of effects to be used in the special effects manager.  
 All of the descendents of this interface will be able to be run through the Special
 Effects Manager.  The code for what happens on a given update loop should happen 
 within UpdateEffect.  
 */
public class Effect : TICoroutineMonoBehaviour
{
	//Note: A constructor with an implementation of effects will be required as well.
	public string effectName;
	protected bool readyToUpdate = false;

	// Place effects for the update loop here.
	public virtual void updateEffect () 
	{
	}

	// a boolean function asking if a finished state is reached.
	public virtual bool isFinished ()
	{
		if (this.gameObject == null)
		{
			return true;
		}
		return false;
	}

	void Update()
	{
		if (readyToUpdate)
		{
			updateEffect();
		}
	}
}

