using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
 The Beveverly Hillbillies Class that controls the free spins game.
 There are 2 stages to this freespins game.
 1. Is a pickem stage that the player picks a door knocker that reveals a symbol
    This symbol will be the the large symbol in the actual freespins part (done on backend)
    During this stage the normal bonus game wings are shown, and the overlay is hidden (like a challenge game)
 2. Is the actuall freespins game. 34443 offset.
    -123-
    01234
    01234
    01234
 There is no way to get more freespins in this game.
 */

public class Bev01FreeSpins : PickMajorFreeSpins
{

	public List<Transform> finalSpitePositions;						// The locations that the sprites should slide too while the knockers are moving.
	public GameObject doorObject;									// The door object that houses the animation that opens the door
	public UISprite bigSymbol;										// The big symbol that goes behind the door.
	public GameObject leftDoorKnockerSpot;							// The spot on the left door where we want to place the knocker
	public GameObject rightDoorKnockerSpot;							// The spot on the right door where we want to place the knocker

	// Constant variables
	private const int NUMBER_OF_TIMES_TO_KNOCK = 3;					// The number of times that we should knock the knocker once it's been selected.
	private const float TIME_PICKME_SHAKE = 0.7f;					// The amount of time to shake the knockers for to entice the player to click.
	private const float TIME_MOVE_KNOCKER = 0.5f;					// The amount of time to move the knocker from the bottom of the screen to the left or right door. 
	private const float TIME_AFTER_DOOR_OPENS = 1.0f;				// The amount of time to wait after the door opens.
	
	private const float TIME_MOVE_FS_TEXT = 0.75f;					// How long to move the stage 2 text.
	private const float TIME_TO_SHOW_FS_TEXT = 2.5f;				// After the text is moved how long to hold it there.

	// Sound names
	private string REVEAL_SOUND;										// The name of the reveal sound that will be played, set in init();
	private const string REVEAL_M1_SOUND = "FreespinM1Beverly";			// The name of the M1 reveal sound
	private const string REVEAL_M2_SOUND = "FreespinM2Beverly";			// The name of the M2 reveal sound
	private const string REVEAL_M3_SOUND = "FreespinM3Beverly";			// The name of the M3 reveal sound
	private const string REVEAL_M4_SOUND = "FreespinM4Beverly";			// The name of the M4 reveal sound
	private const string REVEAL_STINGER = "PreWinFreespinBeverly";		// Name of the sound played when the big symbol is revealed.
	private const string REVEAL_OTHERS = "PickAKnockerRevealOthers";	// Colection name played when the other symbols are revealed.
	private const string KNOCK_SOUND = "PickAKnocker";					// Sound name played when the animation is knocking.
	private const string PICK_ME_SOUND = "KnockerPickMe";				// Sound played when the knocker starts to shake for the pick me.
	private const string KNOCKER_MOVE_SOUND = "value_move";				// Sound name played when the knocker is moving up to the door.
	private const string INTRO_VO = "PickAKnockerVO";					// Sound name played when the games starts.
	private const string SUMMARY_VO = "SummaryVOBeverly";				// Sound name played once the summary screen comes up for this game.

	protected override void setupStage()
	{
		if (BonusGameManager.instance.bonusGameName.Contains("_M1"))
		{
			REVEAL_SOUND = REVEAL_M1_SOUND;
			stageType = Stage1Type.M1;
		}
		else if (BonusGameManager.instance.bonusGameName.Contains("_M2"))
		{
			REVEAL_SOUND = REVEAL_M2_SOUND;
			stageType = Stage1Type.M2;	
		}
		else if (BonusGameManager.instance.bonusGameName.Contains("_M3"))
		{
			REVEAL_SOUND = REVEAL_M3_SOUND;
			stageType = Stage1Type.M3;
		}
		else if (BonusGameManager.instance.bonusGameName.Contains("_M4"))
		{
			REVEAL_SOUND = REVEAL_M4_SOUND;
			stageType = Stage1Type.M4;
		}
		else
		{
			stageType = Stage1Type.NONE;
			Debug.LogError("There was an unexpected format for the name of the Bev01Freespins game, don't know what symbol to reveal for " +
			               BonusGameManager.instance.summaryScreenGameName);
		}
		// Set up the stage 2 stuff
		freeSpinTextStartingPos = freeSpinText != null ? freeSpinText.transform.position : Vector3.zero;
		megaSymbolTextStartingPos = megaSymbolText != null ? megaSymbolText.transform.position : Vector3.zero;
		megaSymbolImageStartingPos = megaSymbolImage != null ? megaSymbolImage.transform.position : Vector3.zero;
		// Check and make sure that we got into one of the normal stage.
		if (stageType != Stage1Type.NONE)
		{
			setupAnimationComponents();
		}
		else // If these wasn't an image to load we should just jump right into the freespins game.
		{
			inputEnabled = false;
			StartCoroutine(transitionIntoStage2());
		}
	}

	protected override void setupAnimationComponents()
	{
		// Saftey check to make sure we didn't do something stupid.
		if ((int)stageType >= 0 && (int)stageType < revealSprites.Count)
		{
			Audio.play(INTRO_VO);
			// Remove the spirte that was selected from the list.
			GameObject winningSpriteObject = revealSprites[(int)stageType];
			revealSprites.RemoveAt((int)stageType);
			if (winningSpriteObject != null)
			{
				UISprite winningSprite = winningSpriteObject.GetComponent<UISprite>();
				// Check and see if we can set the resulting symbol
				if (winningSprite != null)
				{
					if (bigSymbol != null && winningSprite != null)
					{
						// We want to get the UISprite from the sprite we just removed.
						bigSymbol.spriteName = winningSprite.spriteName;
					}
					else
					{
						Debug.LogWarning("The big reveal symbol isn't set.");
					}
					
					// Set the stage 2 image to be correct too
					UISprite megaSymbolSprite = megaSymbolImage.GetComponent<UISprite>();
					if (megaSymbolSprite != null && winningSprite != null)
					{
						megaSymbolSprite.spriteName = winningSprite.spriteName;
					}
					else
					{
						Debug.LogWarning("Mega Symbol sprite in stage 2 was null.");
					}
				}
				else
				{
					Debug.LogWarning("There is no UISprite on the selected object.");
				}
			}
			else
			{
				Debug.LogWarning("There is no object set at " + (int)stageType + " in the revealSprite array.");
			}
		}
		else
		{
			Debug.LogWarning("There are less revealSprites than expected.");
		}
	}

	/////////////////////////////////////
	////////// Stage 1 Methods //////////
	/////////////////////////////////////

	protected override IEnumerator pickMeCallback()
	{
		// Get one of the available knocker game objects
		int randomKnockerIndex = Random.Range(0, buttonSelections.Count);
		GameObject pickMeObject = buttonSelections[randomKnockerIndex];

		// Start the animation
		Animator knockerAnimator = pickMeObject.GetComponent<Animator>();
		if (knockerAnimator != null)
		{
			Audio.play(PICK_ME_SOUND);
			yield return StartCoroutine(
				CommonAnimation.playAnimAndWait(knockerAnimator, "Knocker_PickMe"));
		}
		else
		{
			Debug.LogWarning("There was no animator attached to the pickme Knocker.");
		}
	}

	protected override IEnumerator showPick(GameObject button)
	{
		// Get the animator from the knocker gameObject so that we can play animations on it.
		Animator knockerAnimator = button.GetComponent<Animator>();
		if (knockerAnimator == null)
		{
			Debug.LogWarning("There was no animator attached to the selected Knocker.");
		}

		// Move the knocker from where it was to either the right or left door.
		GameObject objectToMoveTo = button.transform.localPosition.x < 0 ? leftDoorKnockerSpot : rightDoorKnockerSpot;
		Audio.play(KNOCKER_MOVE_SOUND);
		iTween.MoveTo(button, objectToMoveTo.transform.position, TIME_MOVE_KNOCKER);
		// Now that we have moved to the door we should attach to it so when it swings open it's connected.
		button.transform.parent = objectToMoveTo.transform;

		// Super hack because Unity has an issue where the scale isn't being maintained for some reason
		// Someday maybe we can remove these couple lines
		button.SetActive(false);
		button.SetActive(true);
		// End of the hack for Unity issue

		// Remove this knocker from the list because we don't want to use it anymore.
		buttonSelections.Remove(button);
		// Move the other knockers into the right position.
		for (int i = 0; i < finalSpitePositions.Count; i++)
		{
			buttonSelections[i].transform.parent = finalSpitePositions[i].transform;

			// Super hack because Unity has an issue where the scale isn't being maintained for some reason
			// Someday maybe we can remove these couple lines
			buttonSelections[i].SetActive(false);
			buttonSelections[i].SetActive(true);
			// End of the hack for Unity issue

			iTween.MoveTo(buttonSelections[i], finalSpitePositions[i].position, TIME_MOVE_KNOCKER);
		}
		yield return new WaitForSeconds(TIME_MOVE_KNOCKER);

		// Knock the knocker.
		if (knockerAnimator != null)
		{
			for (int i = 0; i < NUMBER_OF_TIMES_TO_KNOCK; i++)
			{
				Audio.play(KNOCK_SOUND);
				knockerAnimator.Play("bev01_FS_PickIntro_Knocker_Animation");
				// Wait for the animation to start
				while (!knockerAnimator.GetCurrentAnimatorStateInfo(0).IsName("bev01_FS_PickIntro_Knocker_Animation"))
				{
					yield return null;
				}
				// Wait for the animation to stop.
				while (knockerAnimator.GetCurrentAnimatorStateInfo(0).IsName("bev01_FS_PickIntro_Knocker_Animation"))
				{
					yield return null;
				}
			}
		}

		// Open up the doors, the sound for this is attached to the knock sound for whatever reason.
		Animation doorAnimation = doorObject.GetComponent<Animation>();
		if (doorAnimation != null)
		{
			float lengthOfAnimation = doorAnimation.clip.length;
			float age = 0.0f;

			// While the animation is playing we want to fade out the banner.
			doorAnimation.Play();
			while (age < lengthOfAnimation)
			{
				age += Time.deltaTime;
				if (banner != null)
				{
					CommonGameObject.alphaUIGameObject(banner, 1 - age / lengthOfAnimation);
				}
				yield return null;
			}
		}
		else
		{
			Debug.LogWarning("The Animaiton attached to the door couldn't be found.");
		}
		// Just make sure that the banner is all the way invisible, plus if the door didn't have an animation it would still be hidden.
		if (banner != null)
		{
			CommonGameObject.alphaUIGameObject(banner, 0.0f);
		}

		// Play the fanfare for opening up the door and picking something.
		Audio.play(REVEAL_STINGER);
		Audio.play(REVEAL_SOUND);

		yield return new WaitForSeconds(TIME_AFTER_DOOR_OPENS);
	}

	protected override IEnumerator showReveals()
	{
		yield return StartCoroutine(base.showReveals());
		// Wait for all of the reveals to finish.
		while (revealSprites.Count > 0)
		{
			yield return null;
		}
	}

	// For right now this is one of the most bland reveal animations of all time, the knocker basically just transforms into the symbol.
	protected override IEnumerator showReveal(GameObject button)
	{
		Audio.play(REVEAL_OTHERS);

		// Show the symbol that could have been picked.
		if (revealSprites.Count > 0)
		{
			// Get the object ready to be used.
			GameObject spriteToReveal = revealSprites[0];
			UISprite sprite = spriteToReveal.GetComponent<UISprite>();
			if (sprite != null)
			{
				// We want to gray out the sprite before revealing it.
				sprite.color = Color.gray;
			}
			revealSprites.RemoveAt(0);
			spriteToReveal.transform.position = button.transform.position;
			// We want these to already be active so there isn't a frame where they flicker on.
			//spriteToReveal.SetActive(true); // Sprites should already be active, but offscreen.
		}
		else
		{
			Debug.LogError("There were not enough symbols that we could reveal.");
		}
		// Hide the knocker because we are done with it.
		button.SetActive(false);
		yield return null;
	}
	

	/////////////////////////////////////
	////////// Stage 2 Methods //////////
	/////////////////////////////////////

	// A function that move the freespin text up and down.
	protected override IEnumerator moveFreeSpinText()
	{
		if (freeSpinText == null || megaSymbolImage == null || megaSymbolText == null)
		{
			yield break;
		}
		// Set up the starting position of everything, freespin on top megasymbols on bottom.
		Vector3 centerMegaTextPos = megaSymbolText.transform.position;
		centerMegaTextPos.y = 0.0f;
		Vector3 centerMegaImagePos = megaSymbolImage.transform.position;
		centerMegaImagePos.y = 0.0f;
		Vector3 centerFreeSpinText = freeSpinText.transform.position;
		centerFreeSpinText.y = 0.0f;
		megaSymbolText.transform.position = centerMegaTextPos;
		megaSymbolImage.transform.position = centerMegaImagePos;


		while (true)
		{
			// Show off the freespin text.
			yield return new WaitForSeconds(TIME_TO_SHOW_FS_TEXT);
			// FS text down Mega text up
			iTween.MoveTo(freeSpinText, iTween.Hash("y", centerFreeSpinText.y, "time", TIME_MOVE_FS_TEXT, "easetype", iTween.EaseType.easeInQuad));
			iTween.MoveTo(megaSymbolText, iTween.Hash("y", megaSymbolTextStartingPos.y, "time", TIME_MOVE_FS_TEXT, "easetype", iTween.EaseType.easeOutQuad));
			iTween.MoveTo(megaSymbolImage, iTween.Hash("y", megaSymbolImageStartingPos.y, "time", TIME_MOVE_FS_TEXT, "easetype", iTween.EaseType.easeOutQuad));
			yield return new WaitForSeconds(TIME_MOVE_FS_TEXT);
			// Show the change that was made
			yield return new WaitForSeconds(TIME_TO_SHOW_FS_TEXT);
			// FS text up, Mega text down.
			iTween.MoveTo(freeSpinText, iTween.Hash("y", freeSpinTextStartingPos.y, "time", TIME_MOVE_FS_TEXT, "easetype", iTween.EaseType.easeOutQuad));
			iTween.MoveTo(megaSymbolText, iTween.Hash("y", centerMegaTextPos.y, "time", TIME_MOVE_FS_TEXT, "easetype", iTween.EaseType.easeInQuad));
			iTween.MoveTo(megaSymbolImage, iTween.Hash("y", centerMegaImagePos.y, "time", TIME_MOVE_FS_TEXT, "easetype", iTween.EaseType.easeInQuad));
			yield return new WaitForSeconds(TIME_MOVE_FS_TEXT);

			// Loop back through and do it all again.
		}
	}

	protected override void gameEnded()
	{
		// Play the summary VO .6 seconds after the game has ended.
		Audio.play(SUMMARY_VO, 1.0f, 0.0f, 0.6f);
		base.gameEnded();
	}
}
