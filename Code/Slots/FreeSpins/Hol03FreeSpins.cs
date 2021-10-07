using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/**
Oz free spins has a special thing that requires the normal FreeSpinGame class to be overridden.
*/

public class Hol03FreeSpins : FreeSpinGame
{
	public GameObject nguiElements;
	public MeshRenderer flashFullscreen;
	public GameObject[] frames;
	public Animator frameActivationEffect;
	public Camera overlayCamera;
	public Transform[] nguiReels;	///< Positioners for the reels in NGUI space.
	
	private GameObject _currentFrame = null;	///< When non-null, is one of the GameObjects in the frames array.
	private bool _isFirstSpin = true;
	
	private List<string> _fSymbols = new List<string>();	///< Holds all the "F" symbols so we can randomly pick one.
	
	private Camera _guiCamera;

	// Sound Names
	private const string KISS = "ValentineKiss";			// The kissing sound that gets played at the start of the kiss animation.

	
	public override void initFreespins()
	{
	
		CommonRenderer.alphaRenderer(flashFullscreen, 0);
		flashFullscreen.gameObject.SetActive(true);
		
		// nguiElements.transform.parent = BonusGameManager.instance.transform;
		// nguiElements.transform.localPosition = Vector3.zero;
		// nguiElements.transform.localScale = Vector3.one;
		
		// Make sure the frames are all off by default.
		for (int i = 0; i < frames.Length; i++)
		{
			frames[i].SetActive(false);
		}
		
		foreach (SymbolInfo info in symbolTemplates)
		{
			ReadOnlyCollection<string> possibleSymbolNames = info.getNameArrayReadOnly();
			foreach (string name in possibleSymbolNames)
			{
				if (name.Substring(0, 1) == "F")
				{
					_fSymbols.Add(name);
				}
			}
		}
		
		base.initFreespins();

		SlotReel[] reelArray = engine.getReelArray();

		for (int i = 0; i < reelArray.Length; i++)
		{
			SlotReel reel = reelArray[i];
			// First make sure no multi-row symbols get in the way of the visible symbols.
			reel.setSymbolsRandomly(_fSymbols);
			// Now assign the 1x4.
			engine.getVisibleSymbolsAt(i)[0].mutateTo("M1-4A", null, false, true);
		}

		UICamera uiCam = GetComponentInParents<UICamera>(transform);
		_guiCamera = uiCam.GetComponent<Camera>();
	}
		
	protected override void startNextFreespin()
	{
		StartCoroutine(prepareNextSpin());
	}
	
	/// Do things before starting the next spin.
	private IEnumerator prepareNextSpin()
	{
		if (hasFreespinsSpinsRemaining)
		{
			clearOutcomeDisplay();

			// We need to get the next outcome before starting the spin,
			// because it tells us what kind of frame to use at the start of the spin.
			// Due to this, I had to override startSpin without this call in it.
			_outcome = _freeSpinsOutcomes.lookAtNextEntry();

			//StartCoroutine(flash());

			if (!_isFirstSpin)
			{
				// First set all symbols to innocent "F" symbols to avoid having large Elvira "M" symbols overlapping other stuff.
				// Don't replace the reel symbols on the first spin since they're pre-set to show a bunch of Elviras.
				SlotReel[] reelArray = engine.getReelArray();

				foreach (SlotReel reel in reelArray)
				{
					reel.setSymbolsRandomly(_fSymbols);
				}
			}
			
			// Position the frame and set the initial reel position for the frame reels.
			// Determine the size and position of the frame.
			int frameLeftReel = 999;	// The leftmost reel that is covered by the framed area.
			Dictionary<int, string> reelStrips = _outcome.getReelStrips();
			
			foreach (KeyValuePair<int, string> kvp in reelStrips)
			{
				frameLeftReel = Mathf.Min(frameLeftReel, kvp.Key);
			}
			
			engine.getVisibleSymbolsAt(frameLeftReel - 1)[0].mutateTo(string.Format("M1-4A-{0}A", reelStrips.Count), null, false, true);
			
			if (_currentFrame != null)
			{
				// First hide the previously shown frame.
				_currentFrame.SetActive(false);
			}

			_currentFrame = frames[reelStrips.Count - 2];
			CommonTransform.setX(_currentFrame.transform, nguiReels[frameLeftReel - 1].transform.localPosition.x);

			_currentFrame.SetActive(true);

			Audio.play(KISS);
			Vector3 framePos = _guiCamera.WorldToViewportPoint(_currentFrame.GetComponentInChildren<MeshRenderer>().transform.position);//UITexture>().transform.position);
			frameActivationEffect.transform.parent.position = overlayCamera.ViewportToWorldPoint(framePos);

			frameActivationEffect.gameObject.SetActive(true);
			yield return new TIAnimatorYieldInstruction(frameActivationEffect, "lips_smootchy");
			frameActivationEffect.gameObject.SetActive(false);
		}
		
		_isFirstSpin = false;
		
		base.startNextFreespin();
	}
	
	/// Flashes a fullscreen flash (like lightning, I'm guessing).
	private IEnumerator flash()
	{
		float age = 0;
		float duration = .5f;
		
		while (age < duration)
		{
			age += Time.deltaTime;
			yield return null;
			CommonRenderer.alphaRenderer(flashFullscreen, 1f - Mathf.Clamp01(age / duration));
		}
	}
	
	/// Returns a random F symbol from the list.
	private string getRandomFSymbol()
	{
		return _fSymbols[Random.Range(0, _fSymbols.Count)];
	}

	/// Will look upwards through the hierarchy to see if the passed component is attached to any of the parents of the passed child.
	public static T GetComponentInParents<T>(GameObject child) where T : Component
	{
		return GetComponentInParents<T>(child.transform);
	}
	public static T GetComponentInParents<T>(Component child) where T : Component
	{
		T component = child.GetComponent<T>();

		if (component == null)
		{
			Transform parentTrans = child.transform.parent;

			while (parentTrans != null && component == null)
			{
				component = parentTrans.GetComponent<T>();
				parentTrans = parentTrans.parent;
			}
		}

		return component;
	}
}
