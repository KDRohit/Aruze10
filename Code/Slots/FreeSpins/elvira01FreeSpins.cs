using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/**
Oz free spins has a special thing that requires the normal FreeSpinGame class to be overridden.
*/

public class elvira01FreeSpins : FreeSpinGame
{
	public Transform nguiElements;
	public UISprite flashFullscreen;
	public GameObject flashFullscreenMeshObj;	// Moving away from NGUI since it has been kind of annoying to maintain in this game, we will now support a GameObject that contains a mesh that will be alpha'ed
	public Animator animatedFlashFullscreen;	// If there is a gameobject defined here, use it instead of the above
	public GameObject[] frames;
	public Transform[] nguiReels;	// Positioners for the reels in NGUI space.
	
	private GameObject _currentFrame = null;	// When non-null, is one of the GameObjects in the frames array.
	private bool _isFirstSpin = true;
	
	private List<string> _fSymbols = new List<string>();	// Holds all the "F" symbols so we can randomly pick one.
	[SerializeField] private string flashSound = "";

	[SerializeField] private float postRevealDelay = 1.0f;

	[SerializeField] private float linkedReelRevealAudioDelay = 0.5f;

	private const string LINKED_REEL_REVEAL_AUDIO_KEY = "linked_reel_reveal";

	public override void initFreespins()
	{
		if (animatedFlashFullscreen != null)
		{
			animatedFlashFullscreen.gameObject.SetActive(false);
		}
		
		if (flashFullscreen != null)
		{
			flashFullscreen.alpha = 0;
			flashFullscreen.gameObject.SetActive(true);
		}

		if (flashFullscreenMeshObj != null)
		{
			CommonGameObject.alphaGameObject(flashFullscreenMeshObj, 0);
		}
				
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
			reel.splitLargeSymbols();

			engine.getVisibleSymbolsAt(i)[0].mutateTo("M1-4A", null, false, true);
		}

		if (GameState.game.keyName == "osa04")
		{
			BonusSpinPanel.instance.messageLabel.text = "";
			_didInit = false;
			StartCoroutine(doFreeSpinsIntro());
		}
	}
	
	// Override to also handle scaling and positioning the NGUI elements.
	protected override IEnumerator fitViewport()
	{
		yield return StartCoroutine(base.fitViewport());

		float scale = FS_FULL_HEIGHT_SCALE * SpinPanel.getNormalizedReelsAreaHeight(true);
		nguiElements.localScale = new Vector3(
			scale,
			scale,
			1.0f
		);
		
		CommonTransform.setY(nguiElements, NGUIExt.effectiveScreenHeight * SpinPanel.getNormalizedReelsAreaCenter(true));
	}

	protected override void gameEnded()
	{
		if (GameState.game.keyName == "osa04")
		{
			Audio.play ("FreespinSummaryVOLion", 1.0f, 0.0f, 2.0f);
		}
		base.gameEnded();
	}

	private IEnumerator doFreeSpinsIntro()
	{
		// Play the audio for the free spins intro.
		Audio.tryToPlaySoundMap("freespinintro");
		yield return new TIWaitForSeconds(.6f);

		// Play the audio for the freespins intro VO.
		Audio.play ("FreespinIntroVOLion");

		// Play the free spins audio.
		yield return new TIWaitForSeconds(4.71f);
		Audio.tryToPlaySoundMap("freespin");

		_didInit = true;
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

			// Position the frame and set the initial reel position for the frame reels.
			// Determine the size and position of the frame.
			int frameLeftReel = 999;	// The leftmost reel that is covered by the framed area.
			Dictionary<int, string> reelStrips = _outcome.getReelStrips();
			
			foreach (KeyValuePair<int, string> kvp in reelStrips)
			{
				frameLeftReel = Mathf.Min(frameLeftReel, kvp.Key);
			}
			
			if (animatedFlashFullscreen != null)
			{
				StartCoroutine(flash());
				StartCoroutine(animationFlash(frameLeftReel, reelStrips.Count - 2));
			}
			else if (flashFullscreen != null || flashFullscreenMeshObj != null)
			{
				StartCoroutine(flash());
			}

			if (!_isFirstSpin)
			{
				SlotReel[] reelArray = engine.getReelArray();

				// Split up any large symbols that may cause overlap when changing linked reels.
				foreach (SlotReel reel in reelArray)
				{
					reel.splitLargeSymbols();
				}
			}

			// Add in the large symbol manually.
			engine.getVisibleSymbolsAt(frameLeftReel - 1)[0].mutateTo(string.Format("M1-4A-{0}A", reelStrips.Count), null, false, true);
				
			if (_currentFrame != null)
			{
				// First hide the previously shown frame.
				_currentFrame.SetActive(false);
			}

			// Add the feature frames.
			_currentFrame = frames[reelStrips.Count - 2];
			CommonTransform.setX(_currentFrame.transform, nguiReels[frameLeftReel - 1].transform.localPosition.x);

			_currentFrame.SetActive(true);

			Audio.tryToPlaySoundMapWithDelay(LINKED_REEL_REVEAL_AUDIO_KEY, linkedReelRevealAudioDelay);

			yield return new WaitForSeconds(postRevealDelay);

		}
		
		_isFirstSpin = false;
		base.startNextFreespin();
	}

	private IEnumerator animationFlash(int frameLeftReel, int frameSize)
	{
		Vector3 newPosition = animatedFlashFullscreen.gameObject.transform.localPosition;
		newPosition.x = getReelRootsAt(frameLeftReel - 1).transform.localPosition.x;
		newPosition.x += ((frameSize * 0.75f) + 0.75f);
		animatedFlashFullscreen.gameObject.transform.localPosition = newPosition;
		animatedFlashFullscreen.gameObject.SetActive(true);
		animatedFlashFullscreen.Play("osa04 free spins Symbol Transform");
		Audio.play("SparklyFlipBellTreeOSA04");
		yield return new WaitForSeconds(0.5f);
		animatedFlashFullscreen.Play("osa04 free spins Symbol Transform still");
		animatedFlashFullscreen.gameObject.SetActive(false);
	}
	
	/// Flashes a fullscreen flash (like lightning, I'm guessing).
	private IEnumerator flash()
	{
		float age = 0;
		float duration = .5f;
		if (flashSound != "")
		{
			Audio.playSoundMapOrSoundKey(flashSound);
		}
		while (age < duration)
		{
			age += Time.deltaTime;
			yield return null;

			if (flashFullscreen != null)
			{
				flashFullscreen.alpha = 1f - Mathf.Clamp01(age / duration);
			}
			
			if (flashFullscreenMeshObj != null)
			{
				CommonGameObject.alphaGameObject(flashFullscreenMeshObj, 1f - Mathf.Clamp01(age / duration));
			}
		}
	}
}
