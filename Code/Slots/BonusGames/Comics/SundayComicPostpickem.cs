using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions; // Used to get the progressivePool and the winning value.

// This class is designed to hold the logic that should get run after the pickem stage of the Sunday
// Comic Pickem. When it finishes it's operations it tells SundayComicBonus that it's done.
public class SundayComicPostpickem : TICoroutineMonoBehaviour 
{
	public enum GameType
	{
		Blondie = 0,
		Beetle = 1
	}

	public Transform newCharacterPosition; // The position that the character parent is going to move to.
	public Transform newProgressivePosition; // The position that the progressive parent is going to move to.

	public GameObject characterParent;				// The character parent holding all of the chacter information.
	public GameObject progressiveParent;			// The parent to all of the progressive children.
	public GameObject[] progressiveChildren;		// The positions of the progressive objects.
	public GameObject[] progressiveWinAnimation;	// Animations that are played when you do get a progressive.
	public GameObject[] progressiveLoseAnimation;	// Animations that are played when you don't get a progressive.

	public SundayComicBonus comicBonus;				// A link to the controller for this class.

	public UISprite kissSprite;						// The sprite that is blown when blondie makes a kiss

	public  Transform[] midpointsForProgressives; 	// The middle points that are used for the iTween path.
	// Poses used for revealing the progressive.
	public UISprite[] poses;						// The different poses that you the character can make.
	public GameType gameType;						// The type of comic game this is so that we play the poses properly.

	private UISprite characterSprite;				// The sprite of the comic character that's going to get changed on animaitons.
	private GameObject headPosition;				// The position of the blondies head for when she blows a kiss.
	private Transform[] pathForiTween; 				// The path for each iTween goes Head -> Midpoint -> Progressive.
	private long progressiveAmount; 				// The winning progressive amount.
	// Constant variables
	private const float TIME_BEFORE_HIDING_PROGRESSIVES = 1.0f;				// Time after the character has moved before progressives start disapearing.
	private const float TIME_TO_SHOW_WINNING_PROGRESSIVE = 2.0f;			// Time to show the winning progressive before the roll up.
	private const float TIME_TO_SHOW_LOSING_PROGRESSIVE = 1.0f;				// Time to wait before moving to the next losing progressive.
	private const float TIME_TO_TWEEN_CHARACTER = 1.5f;						// Time to move the character from the LHS of the screen to the middle.
	private const float TIME_OF_LOSING_ANIMATION = 0.5f;					// How long we want to play the losing animation.
	// Sound Names
	private const string BLONDIE_KISS = "BlondieKiss";						// ooh la la
	private const string BEETLE_FIGHT_ACTION = "BeetleFightAction";			// Name of the sound played when Beetle kicks away a progressive.
	private const string REMOVE_PROGESSIVE = "EliminateProgressiveComix";	// Name of the sound played when a progressive value is removed.
	private const string REVEAL_PROGRESSIVE = "RevealProgressiveComix";		// Name of the sound played when this stage starts.

	void Awake ()
	{

	}

	public IEnumerator startGame ()
	{
		iTween.MoveTo(characterParent, iTween.Hash("position", newCharacterPosition.position, "islocal", false, "time", TIME_TO_TWEEN_CHARACTER, "easetype", iTween.EaseType.easeInOutQuad));
		iTween.MoveTo(progressiveParent, iTween.Hash("position", newProgressivePosition.position, "islocal", false, "time", TIME_TO_TWEEN_CHARACTER, "easetype", iTween.EaseType.easeInOutQuad));
		GameObject characterSpriteParent = CommonGameObject.findDirectChild(characterParent,"Character");
		if (characterSpriteParent != null)
		{
			characterSprite = characterSpriteParent.GetComponent<UISprite>();
		}
		else
		{
			Debug.LogWarning("The Character sprite is not set properly");
		}
		headPosition = CommonGameObject.findDirectChild(characterParent,"Head"); // Blondie specific.
		yield return new WaitForSeconds(TIME_TO_TWEEN_CHARACTER + TIME_BEFORE_HIDING_PROGRESSIVES); // Wait an extra second here so we don't start removing things before everything is into position.
		yield return StartCoroutine(hideProgressives());
		yield return StartCoroutine(animateWinningProgressive());
		BonusGamePresenter.instance.currentPayout += progressiveAmount;
		endGame(progressiveAmount);
	}

	/// Plays the animation that is associated with the winning progressive.
	private IEnumerator animateWinningProgressive()
	{
		for (int k = 0; k < progressiveChildren.Length; k++)
		{
			GameObject labelGO = CommonGameObject.findChild(progressiveChildren[k], "Text");
			UILabel label = labelGO.GetComponent<UILabel>();
			if (label != null && label.text.Equals(CreditsEconomy.convertCredits(progressiveAmount)))
			{
				Audio.play(REVEAL_PROGRESSIVE);
				GameObject parentObject = progressiveChildren[k].gameObject;
				GameObject gemGO = CommonGameObject.findDirectChild(progressiveChildren[k], "Gem");
				GameObject backgroundGO = CommonGameObject.findDirectChild(progressiveChildren[k], "Background");
				// We have to proper progressive animation
				GameObject animaition = CommonGameObject.instantiate(progressiveWinAnimation[k], parentObject.transform.position, parentObject.transform.rotation) as GameObject;
				if (animaition == null)
				{
					Debug.LogWarning("Reveal Animaion could not be Instantiated");
					yield break;
				}
				animaition.transform.parent = parentObject.transform;
				animaition.transform.localPosition = new Vector3(0, 0, -10f);
				// We only want the text to be active because the bacground animation has everything else.
				gemGO.SetActive(false);
				backgroundGO.SetActive(false);
				yield return new WaitForSeconds(TIME_TO_SHOW_WINNING_PROGRESSIVE);
				//Destroy(animaition); // We don't destroy this progressive because they have won and we want it to display until they get back to the game.
			}
		}
	}

	/// Checks to see what the winning progressive amount is, then goes through the progressives and calls hideProgressive() on each one that should be removed.
	private IEnumerator hideProgressives ()
	{
		if (comicBonus.progressiveOutcome.entryCount != 1)
		{
			Debug.LogWarning("Didn't get the WheelPick information that was expected");
			yield break;
		}
		WheelPick progressivePick = comicBonus.progressiveOutcome.getNextEntry() as WheelPick;
		// Needs to be reset when selected. Do it now because it won't be undated again.
		if(SlotsPlayer.instance.progressivePools.isValidProgressivePool(progressivePick.progressivePool))
		{
			progressiveAmount = SlotsPlayer.instance.progressivePools.getPoolCredits(progressivePick.progressivePool, SlotBaseGame.instance.multiplier, true);
		}
		else
		{
			progressiveAmount = progressivePick.baseCredits * SlotBaseGame.instance.multiplier * GameState.baseWagerMultiplier;
		}

		for (int k = 0; k < progressiveChildren.Length; k++)
		{
			GameObject labelGO = CommonGameObject.findChild(progressiveChildren[k], "Text");
			GameObject gemGO = CommonGameObject.findDirectChild(progressiveChildren[k], "Gem");
			GameObject backgroundGO = CommonGameObject.findDirectChild(progressiveChildren[k], "Background");
			UILabel label = labelGO.GetComponent<UILabel>();
			if (label != null && !label.text.Equals("" + CreditsEconomy.convertCredits(progressiveAmount)))
			{
				pathForiTween = new Transform[3] {headPosition.transform, midpointsForProgressives[k].transform, progressiveChildren[k].transform};
				yield return StartCoroutine(hideProgressive(progressiveChildren[k]));
				GameObject parentObject = progressiveChildren[k].gameObject;
				//We have to proper progressive animation
				GameObject animaition = CommonGameObject.instantiate(progressiveLoseAnimation[k], parentObject.transform.position, parentObject.transform.rotation) as GameObject;
				if (animaition == null)
				{
					Debug.LogWarning("Reveal Animaion could not be Instantiated");
					yield break;
				}
				animaition.transform.parent = parentObject.transform;
				animaition.transform.localPosition = new Vector3(0, 0, -10f);
				// We only want the text to be active because the bacground animation has everything else.
				gemGO.SetActive(false);
				backgroundGO.SetActive(false);
				yield return new WaitForSeconds(TIME_OF_LOSING_ANIMATION);
				Destroy(animaition); // We destroy this after the animation is over.
				progressiveChildren[k].SetActive(false); // Hide everything (all that's left is the text)
				yield return new WaitForSeconds(TIME_TO_SHOW_LOSING_PROGRESSIVE); // Wait a second after every progressive is revealed.

			}
		}
	}

	// Changes blondies pose so it looks like she is blowing a kiss, then calls blowKiss(), and then deactivates the kiss.
	private IEnumerator hideProgressive(GameObject progressiveChild)
	{
		switch(gameType)
		{
		case GameType.Blondie:
			yield return StartCoroutine(blondieAnimation(progressiveChild));
			break;
		case GameType.Beetle:
			yield return StartCoroutine(beetleAnimation(progressiveChild));
			break;
		}
	}

	private IEnumerator beetleAnimation(GameObject progressiveChild)
	{
		Audio.play(REMOVE_PROGESSIVE);
		UISprite pose = poses[0];
		if(progressiveChild == progressiveChildren[0])
		{
			pose = poses[4];
			TweenPosition.Begin(progressiveChild, 1f, new Vector3(1100, -1500));
			TweenRotation.Begin(progressiveChild, 1f, Quaternion.Euler(0,0,-160));
		}
		else if(progressiveChild == progressiveChildren[1])
		{
			pose = poses[3];
			TweenPosition.Begin(progressiveChild, 1f, new Vector3(1100, progressiveChild.transform.localPosition.y));
			TweenRotation.Begin(progressiveChild, 1f, Quaternion.Euler(0,0,-160));
		}
		else if(progressiveChild == progressiveChildren[2])
		{
			pose = poses[2];
			TweenPosition.Begin(progressiveChild, 1f, new Vector3(1100, progressiveChild.transform.localPosition.y));
			TweenRotation.Begin(progressiveChild, 1f, Quaternion.Euler(0,0,-160));
		}
		else if(progressiveChild == progressiveChildren[3])
		{
			pose = poses[1];
			TweenPosition.Begin(progressiveChild, 1f, new Vector3(1100, 400));
			TweenRotation.Begin(progressiveChild, 1f, Quaternion.Euler(0,0,-160));
		}

		changePose(pose);
		Audio.play(BEETLE_FIGHT_ACTION);
		yield return new WaitForSeconds(0.25f);
		changePose(poses[0]);
		yield return new WaitForSeconds(0.75f);
	}

	private IEnumerator blondieAnimation(GameObject progressiveChild)
	{
		Audio.play(REMOVE_PROGESSIVE);
		float duration = 1;
		changePose(poses[1]);
		yield return new WaitForSeconds(0.1f);
		changePose(poses[2]);
		yield return StartCoroutine(blowKiss(progressiveChild, duration));
		changePose(poses[0]);
		if(kissSprite != null)
		{
			kissSprite.gameObject.SetActive(false); //Hide this because the kiss has already made it and now we just want to do the animation.
		}
	}

	/// Called to blow the kiss from blondies mouth to the midpoint and then to the progressiveChild.
	private IEnumerator blowKiss(GameObject progressiveChild, float duration)
	{
		if(kissSprite != null)
		{
			Audio.play(BLONDIE_KISS);
			kissSprite.transform.position = headPosition.transform.position;
			kissSprite.gameObject.SetActive(true);
			iTween.MoveTo(kissSprite.gameObject,progressiveChild.transform.position,duration);
			iTween.ValueTo(this.gameObject, iTween.Hash("from", 0.0f,"to", 1.0f,"time", duration,"onupdate", "moveKiss"));
			iTween.ScaleFrom(kissSprite.gameObject,Vector3.zero,duration);
			yield return new WaitForSeconds(duration);
		}
	}

	/// A function to move the kiss along the path evenly with the scaling.
	private void moveKiss(float percent)
	{
		if(kissSprite == null) return;
		iTween.PutOnPath(kissSprite.gameObject, pathForiTween, percent);
	}

	/// Changes the pose, and changes the scale of the sprite so that the sprite doesn't look stretched or shrunk. This is 1-1 with the Atlas size of the sprite.
	private void changePose(UISprite pose)
	{
		characterSprite.spriteName = pose.spriteName;
		characterSprite.transform.localScale = pose.transform.localScale;
		characterSprite.transform.localPosition = pose.transform.localPosition;
		characterSprite.MarkAsChanged();
	}

	private void endGame (long progressiveAmount)
	{
		comicBonus.endPostpickem(progressiveAmount);
	}

	/// Used for testing so you can see the path that the kiss is going to follow inside the editor.
	private void OnDrawGizmos()
	{
		if (pathForiTween != null)
		{
			iTween.DrawPath(pathForiTween);
		}
	}
}
