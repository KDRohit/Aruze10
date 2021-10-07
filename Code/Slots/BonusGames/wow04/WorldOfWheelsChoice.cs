using UnityEngine;
using System.Collections;

/**
 Represents a 'choice' that the player may make for the wheel bonus game.  Used to register a game object
 to a wheel bonus game.
 */ 
public class WorldOfWheelsChoice : TICoroutineMonoBehaviour
{
	public Wow04BonusWheel parentToRegisterTo;
	public int index;
	
	/// Use this for initialization
	void Start ()
	{
		parentToRegisterTo.registerPick(this.gameObject);
	}
	
	/// NGUI function for informing the parent that the current object has been clicked
	void OnClick ()
	{
		parentToRegisterTo.clickBonusButton(this.gameObject);
	}
}

