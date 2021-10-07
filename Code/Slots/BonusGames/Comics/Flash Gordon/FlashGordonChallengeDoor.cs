using UnityEngine;
using System.Collections;

public class FlashGordonChallengeDoor : TICoroutineMonoBehaviour 
{
	private enum RevealTypeEnum
	{
		NONE = -1,
		DALE = 0,
		VALUE,
		END_GAME
	}

	[SerializeField] private GameObject daleRevealObject = null;		// GameObject for the Dale reveal stuff
	[SerializeField] private UILabel daleText = null;					// Label for the dale special value win text, set dynamically -  To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent daleTextWrapperComponent = null;					// Label for the dale special value win text, set dynamically

	public LabelWrapper daleTextWrapper
	{
		get
		{
			if (_daleTextWrapper == null)
			{
				if (daleTextWrapperComponent != null)
				{
					_daleTextWrapper = daleTextWrapperComponent.labelWrapper;
				}
				else
				{
					_daleTextWrapper = new LabelWrapper(daleText);
				}
			}
			return _daleTextWrapper;
		}
	}
	private LabelWrapper _daleTextWrapper = null;
	
	[SerializeField] private UISprite daleSprite = null;				// Sprite for the dale icon, used to color it based on if it is picked or revealed

	[SerializeField] private GameObject valueRevealObject = null;		// GameObject for the value reveal stuff
	[SerializeField] private UILabel valueText = null;					// Label for the normal value win text, set dynamically -  To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent valueTextWrapperComponent = null;					// Label for the normal value win text, set dynamically

	public LabelWrapper valueTextWrapper
	{
		get
		{
			if (_valueTextWrapper == null)
			{
				if (valueTextWrapperComponent != null)
				{
					_valueTextWrapper = valueTextWrapperComponent.labelWrapper;
				}
				else
				{
					_valueTextWrapper = new LabelWrapper(valueText);
				}
			}
			return _valueTextWrapper;
		}
	}
	private LabelWrapper _valueTextWrapper = null;
	

	[SerializeField] private GameObject gameEndRevealObject = null;		// GameObject shown when the player gets a game ended result
	[SerializeField] private UILabel gameEndText = null;				// Need this label so I can control the color of it -  To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent gameEndTextWrapperComponent = null;				// Need this label so I can control the color of it

	public LabelWrapper gameEndTextWrapper
	{
		get
		{
			if (_gameEndTextWrapper == null)
			{
				if (gameEndTextWrapperComponent != null)
				{
					_gameEndTextWrapper = gameEndTextWrapperComponent.labelWrapper;
				}
				else
				{
					_gameEndTextWrapper = new LabelWrapper(gameEndText);
				}
			}
			return _gameEndTextWrapper;
		}
	}
	private LabelWrapper _gameEndTextWrapper = null;
	
	[SerializeField] private UISprite gameEndSprite = null;				// Sprite for the game end icon, used to color it based on if it is picked or revealed
	
	[SerializeField] private UISprite doorSprite = null;				// Sprite for the door, changed based on what stage of the game we're on
	[SerializeField] private Animation doorAnimation = null;			// The animation control object for the door

	private Color valueTextColor = Color.white;							// Place to store what the default color is
	private Color daleTextColor = Color.white;							// Place to store what the default color is
	private Color gameEndTextColor = Color.white;						// Place to store what the default color is

	private static readonly string[] DOOR_SPRITE_NAMES = {"Com03_Lvl1_Door", "Com03_Lvl2_Door", "Com03_Lvl3_Door", "Com03_Lvl4_Door"};

	// Animation Constants
	private const string IDLE_ANIMATION_NAME = "com03_DRC_Idle_Animation";
	private const string PICK_ME_ANIMATION_NAME = "com03_DRC_PickMe_Animation";
	private const string REVEAL_DALE_ANIMATION_NAME = "com03_DRC_Reveal_Dale_Animation";
	private const string END_GAME_REVEAL_ANIMATION_NAME = "com03_DRC_Reveal_Ends Bonus_Animation";
	private const string REVEAL_NUMBER_ANIMATION_NAME = "com03_DRC_Reveal_Number_Animation";

	// Sound Constants
	private const string DOOR_PICK_ME_SOUND = "DRCellDoorPickMe";							// Sound played for the pick me animation of the door
	private const string DOOR_OPEN_SOUND = "DRPickCellDoorOpen";							// Sound when the door opens
	private const string DOOR_REVEAL_SPARKLE_SOUND = "DRSparklyWhooshUp";					// Sparkle sounds for the reveal of a pick
	private const string REVEAL_CREDITS_SOUND = "DRRevealCredit";							// Credits are revealed sound
	private const string REVEAL_MING_SOUND = "DRRevealMing";								// Ming reveal for game end
	private const string REVEAL_DALE_SOUND = "DRRevealDale";								// Dale reveal sound, needs to have 1-4 added to the end
	private const string REVEAL_OTHER_CLANG_SOUND = "DRRevealOthersClang";					// Play the other reveal sound for unpicked options
	private const string REVEAL_DALE_VO_STAGE_1 = "MGNotSoFastGordonFoundMyDecoy"; 			// Played in stage one when you find Dale
	private const string REVEAL_DALE_VO_STAGE_2 = "MGAnotherDecoy";							// Played in stage two when you find Dale
	private const string REVEAL_DALE_VO_STAGE_3 = "MGAnotherYoullNeverFindRealDale";		// Played in stage three when you find Dale
	private const string REVEAL_DALE_VO_STAGE_4 = "DRFinalRescueVO";						// Played when a value or dale is revealed in the final stage
	private const string REVEAL_MING_END_GAME_VO = "MGBwahahahBetterLuckNextTimeEarthling";	// Ming game over VO

	/// Called by the unity when the object Awakes
	private void Awake()
	{
		valueTextColor = valueTextWrapper.color;
		daleTextColor = daleTextWrapper.color;
		gameEndTextColor = gameEndTextWrapper.color;
	}

	
	public bool isRevealed
	{
		get { return _isRevealed; }
	}
	private bool _isRevealed = false;									// Tells if this door has been revealed

	/// set the door to idle
	public void setDoorIdle()
	{
		doorAnimation.Play(IDLE_ANIMATION_NAME);
	}

	/// instantly reveal a value
	public void revealUnpickedValue(long amount)
	{
		_isRevealed = true;
		doorAnimation.Play(IDLE_ANIMATION_NAME);

		// hide the door sprite
		gameObject.SetActive(false);

		Audio.play(DOOR_OPEN_SOUND);
		Audio.play(REVEAL_OTHER_CLANG_SOUND);

		changeRevealType(RevealTypeEnum.VALUE);
		valueTextWrapper.text = CreditsEconomy.convertCredits(amount);
		valueTextWrapper.color = Color.gray;
	} 

	/// reveal a regular value with the animation
	public IEnumerator playValueRevealAnimation(long amount, FlashGordonChallengeGame.GameStageEnum stage)
	{
		_isRevealed = true;

		valueTextWrapper.text = CreditsEconomy.convertCredits(amount);

		doorAnimation.Play(REVEAL_NUMBER_ANIMATION_NAME);

		Audio.play(DOOR_OPEN_SOUND);
		Audio.play(DOOR_REVEAL_SPARKLE_SOUND);
		Audio.play(REVEAL_CREDITS_SOUND);

		// wait for the animation to end
		while (doorAnimation.isPlaying)
		{
			yield return null;
		}

		if (stage == FlashGordonChallengeGame.GameStageEnum.STAGE_4)
		{
			// found a value in the final stage so play this sound
			Audio.play(REVEAL_DALE_VO_STAGE_4);
		}
	}

	/// instantly reveal a dale pick
	public void revealUnpickedDale(long amount)
	{
		_isRevealed = true;
		doorAnimation.Play(IDLE_ANIMATION_NAME);

		// hide the door sprite
		gameObject.SetActive(false);

		Audio.play(DOOR_OPEN_SOUND);
		Audio.play(REVEAL_OTHER_CLANG_SOUND);

		changeRevealType(RevealTypeEnum.DALE);
		daleSprite.color = Color.gray;
		daleTextWrapper.text = CreditsEconomy.convertCredits(amount);
		daleTextWrapper.color = Color.gray;
	}

	/// reveal a dale pick with the animation
	public IEnumerator playDalePickRevealAnimation(long amount, FlashGordonChallengeGame.GameStageEnum stage)
	{
		_isRevealed = true;

		daleTextWrapper.text = CreditsEconomy.convertCredits(amount);

		doorAnimation.Play(REVEAL_DALE_ANIMATION_NAME);

		Audio.play(DOOR_OPEN_SOUND);
		Audio.play(DOOR_REVEAL_SPARKLE_SOUND);
		Audio.play(REVEAL_DALE_SOUND + ((int)stage + 1));

		// wait for the animation to end
		while (doorAnimation.isPlaying)
		{
			yield return null;
		}

		switch (stage)
		{
			case FlashGordonChallengeGame.GameStageEnum.STAGE_1:
				Audio.play(REVEAL_DALE_VO_STAGE_1);
				break;
			case FlashGordonChallengeGame.GameStageEnum.STAGE_2:
				Audio.play(REVEAL_DALE_VO_STAGE_2);
				break;
			case FlashGordonChallengeGame.GameStageEnum.STAGE_3:
				Audio.play(REVEAL_DALE_VO_STAGE_3);
				break;
			case FlashGordonChallengeGame.GameStageEnum.STAGE_4:
				Audio.play(REVEAL_DALE_VO_STAGE_4);
				break;
		}
	}

	/// instantly reveal a game ending pick
	public void revealUnpickedGameEnd()
	{
		_isRevealed = true;
		doorAnimation.Play(IDLE_ANIMATION_NAME);

		// hide the door sprite
		gameObject.SetActive(false);

		Audio.play(DOOR_OPEN_SOUND);
		Audio.play(REVEAL_OTHER_CLANG_SOUND);

		changeRevealType(RevealTypeEnum.END_GAME);
		gameEndTextWrapper.color = Color.gray;
		gameEndSprite.color = Color.gray;
	}

	/// reveal a game end pick with animation
	public IEnumerator playGameEndRevealAnimation()
	{
		_isRevealed = true;

		doorAnimation.Play(END_GAME_REVEAL_ANIMATION_NAME);

		Audio.play(DOOR_OPEN_SOUND);
		Audio.play(DOOR_REVEAL_SPARKLE_SOUND);
		Audio.play(REVEAL_MING_SOUND);

		// wait for the animation to end
		while (doorAnimation.isPlaying)
		{
			yield return null;
		}

		Audio.play(REVEAL_MING_END_GAME_VO);
	}

	/// plays the pick me animation
	public IEnumerator playPickMe()
	{
		doorAnimation.Play(PICK_ME_ANIMATION_NAME);

		Audio.play(DOOR_PICK_ME_SOUND);

		// wait for the animation to end
		while (doorAnimation.isPlaying)
		{
			yield return null;
		}

		if (!_isRevealed)
		{
			setDoorIdle();
		}
	}

	/// change the style of the door to match the stage of the game
	public void changeDoorStyle(FlashGordonChallengeGame.GameStageEnum gameStage)
	{
		doorSprite.spriteName = DOOR_SPRITE_NAMES[(int)gameStage];
	}

	/// Used to instantly swap what is being displayed for a reveal
	private void changeRevealType(RevealTypeEnum revealType)
	{
		switch (revealType)
		{
			case RevealTypeEnum.NONE:
				_isRevealed = false;
				daleRevealObject.SetActive(false);
				valueRevealObject.SetActive(false);
				gameEndRevealObject.SetActive(false);
				break;
			case RevealTypeEnum.DALE:
				daleRevealObject.SetActive(true);
				valueRevealObject.SetActive(false);
				gameEndRevealObject.SetActive(false);
				break;
			case RevealTypeEnum.VALUE:
				daleRevealObject.SetActive(false);
				valueRevealObject.SetActive(true);
				gameEndRevealObject.SetActive(false);
				break;
			case RevealTypeEnum.END_GAME:
				daleRevealObject.SetActive(false);
				valueRevealObject.SetActive(false);
				gameEndRevealObject.SetActive(true);
				break;
			default:
				daleRevealObject.SetActive(false);
				valueRevealObject.SetActive(false);
				gameEndRevealObject.SetActive(false);
				break;
		}
	}

	/// Reset the door when going to a new round
	public void resetDoor()
	{
		setDoorIdle();
		changeRevealType(RevealTypeEnum.NONE);
		
		// reset colors of all reveal types
		valueTextWrapper.color = valueTextColor;

		daleTextWrapper.color = daleTextColor;
		daleSprite.color = Color.white;
		
		gameEndTextWrapper.color = gameEndTextColor;
		gameEndSprite.color = Color.white;

		// make sure the door sprite is showing
		gameObject.SetActive(true);
	}
}

