using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Used as a way for the player to choose a casket on the Elvira game, acts as a component attached to the game, instead of the old way which used a dialog
**/
public abstract class BaseBonusPoolsComponent : TICoroutineMonoBehaviour
{
	[SerializeField] private GameObject[] buttonObjects; // maintain a list of the button objects so we can atomatically mark the choice that the user made
	[SerializeField] private string REVEAL_BUTTON_SOUND = "";
	[SerializeField] private string REVEAL_LEFTOVER_BUTTON_SOUND = "";
	[SerializeField] private float PICK_POST_WAIT = 0.0f;
	[HideInInspector] public int userChoice = -1;

	protected BonusPoolItem pick = null;
	protected List<int> indicesRemaining = new List<int> { 0, 1, 2 };
	protected List<BonusPoolItem> reveals = null;
	protected string fromSymbol = "";
	
	public bool didChoose
	{
		get { return _didChoose; }
	}
	private bool _didChoose = false;
	private bool isBonusFinished = false;
	
	/// Initialization
	private void init(BonusPoolItem pickData, List<BonusPoolItem> revealsData, string fromSymbolData)
	{
		pick = pickData;
		reveals = revealsData;
		fromSymbol = fromSymbolData;
		
		// Check to see if it's the 1x4 symbol.
		if (fromSymbol == "M1-4A" || fromSymbol == "M1-2A")
		{
			// Use the first symbol name from the M1 set.
			fromSymbol = "M1";
		}

		derivedInit();
		
		// Hide the some graphics by default.
		resetBonusButtonElements();

		isBonusFinished = false;
		_didChoose = false;
		indicesRemaining = new List<int> { 0, 1, 2 };
	}

	// handle stuff a derived class might need to init, not making init() virtual since I always want to make sure that is called
	protected virtual void derivedInit()
	{
		// handle what to do in derived class, if anything
	}

	protected virtual void resetBonusButtonElements()
	{
		// override to handle reseting of the elements that the bonus pool you are doing is using
	}

	// Starts the bonus game and lets it play until it is finished
	public IEnumerator playBonus(BonusPoolItem pickData, List<BonusPoolItem> revealsData, string fromSymbolData)
	{
		init(pickData, revealsData, fromSymbolData);

		gameObject.SetActive(true);

		playBonusStartSounds();

		while (!isBonusFinished)
		{
			yield return null;
		}

		doBeforeBonusIsOver();
	
		// Bonus game is over, so make the object inactive.
		gameObject.SetActive(false);
	}

	// Handle stuff that needs to happen before the bonus ends, i.e. switching an audio key for instance
	protected virtual void doBeforeBonusIsOver()
	{
		// add code here to do before the bonus is over
	}

	protected virtual void playBonusStartSounds()
	{
		// override to play sounds when the game is starting
	}
	
	/// NGUI button callback.
	public void buttonClicked(GameObject go)
	{
		if (_didChoose)
		{
			return;
		}
		_didChoose = true;
		StartCoroutine(revealAllButtonObjects(go));
	}
	
	/// Reveals the contents of the caskets after clicking one.
	private IEnumerator revealAllButtonObjects(GameObject go)
	{
		userChoice = -1;
		
		for (int i = 0; i < buttonObjects.Length; i++)
		{
			if (go == buttonObjects[i])
			{
				userChoice = i;
				break;
			}
		}
				
		// First open the chosen one.
		if (REVEAL_BUTTON_SOUND != "")
		{
			Audio.play(REVEAL_BUTTON_SOUND);
		}
		yield return StartCoroutine(revealButtonObject(userChoice, pick, true));

		if (PICK_POST_WAIT != 0.0f)
		{
			yield return new WaitForSeconds(PICK_POST_WAIT);
		}

		//Simultaniously revel others
		if (REVEAL_LEFTOVER_BUTTON_SOUND != "")
		{
			Audio.play(REVEAL_LEFTOVER_BUTTON_SOUND);
		}
		yield return new TIParrallelYieldInstruction(StartCoroutine(revealButtonObject(indicesRemaining[0], reveals[0], false)), StartCoroutine(revealButtonObject(indicesRemaining[0], reveals[0], false)));

		isBonusFinished = true;
	}
	
	// Reveal a single button object
	protected virtual IEnumerator revealButtonObject(int index, BonusPoolItem poolItem, bool selected = true)
	{
		// override in your derived class
		yield break;
	}
		
	/// Shows a multiplier-type pool item.
	protected virtual void showMultiplier(int index, string spriteName, bool selected = true)
	{
		// override in your derived class
	}
}
