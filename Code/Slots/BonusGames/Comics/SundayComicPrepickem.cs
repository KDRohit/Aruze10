using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions; // Used to get the progressivePool and the winning value.

// This class is designed to hold the logic that should get run during the banner picking stage of the Sunday
// Comic Pickem. When it finishes it's operations it tells SundayComicBonus that it's done.
public class SundayComicPrepickem : TICoroutineMonoBehaviour 
{

	public GameObject[] pickBanners;								// The banners the scene that contain all of the picks.
	public GameObject choiceCardAnimaitonPrefab;					// The animation that gets played when a pick is chosen.
	private string localizedPickText;								// The localizedPickText so that it only gets looked up once.
	private float bannerScaleAmount = .98f;							// The amount to scale the banner that were not picked.
	private bool pickSelected = false;								// Put a lock on the picks so you can only select one.
	private SkippableWait revealWait = new SkippableWait();

	// Constant variables
	private const int MIN_BANNER_NUMBER = 1;						// Lowest value that could show up as a choice.
	private const int MAX_BANNER_NUMBER = 12;						// Highest value that could show up as a choice. 
	private const float TIME_AFTER_PICK_ROLLUP = 0.5f;				// Amount of time we should wait after the pick has rolled up so players can revel in their choice.
	private const float TIME_BEFORE_ENDING_GAME = 0.5f;				// Amount of time to wait after all the reveals have been done so players can soak it in.
	private const float TIME_BETWEEN_REVEALS = 0.2f;				// Amount of time to wait in between reveals.
	private const float TIME_FOR_PICK_ANIMATION = 1.0f;				// Amount of time to wait after starting the pick animation.
	// Localization Strings
	private const string PICKS = "picks";							// Loc key for the word picks.
	// Sound names
	private const string BACKGROUND_MUSIC = "BonusBgComix";			// The name of the background music.
	private const string ON_PICK_SELECTED1 = "SummaryTotal";		// The name of a sound that gets played when a pick is selcted.
	private const string ON_PICK_SELECTED2 = "ComixPow";			// The name of a sound that gets played when a pick is selcted.
	private const string REVEAL_NOT_CHOSEN = "reveal_not_chosen";	// The name of the sound mapped to be played when revealing the missised choices.


	public SundayComicBonus comicBonus;

	void Awake ()
	{
		localizedPickText = Localize.text(PICKS);
		
		Audio.switchMusicKey(BACKGROUND_MUSIC);
		Audio.stopMusic();
	}

	private void endGame()
	{
		comicBonus.endPrepickem();
	}

	/// Method called when ever a button is clicked.
	public void onPickButtonSelected(GameObject button)
	{
		if (!pickSelected)
		{
			pickSelected = true;
			Audio.play(ON_PICK_SELECTED1);
			Audio.play(ON_PICK_SELECTED2);
			StartCoroutine(startReveals(button));
		}
	}

	/// Starts the entire reveal process. Calling revealPickBanner() on each banner from left to right.
	private IEnumerator startReveals(GameObject button)
	{
		// Go through everything and turn off the glow effect that is supposed to entice the played to click.
		foreach (GameObject pickBanner in pickBanners)
		{
			GameObject eachButton = CommonGameObject.findDirectChild(pickBanner,"Button");
			TweenColor tween = eachButton.GetComponent<TweenColor>();
			if (tween)
			{
				UISprite uiSp = eachButton.GetComponent<UISprite>();
				uiSp.color = Color.white;
				Destroy(tween);
			}
		}

		GameObject clickedButtonParent = button.transform.parent.gameObject;
		yield return StartCoroutine(revealPickBanner(clickedButtonParent, comicBonus.pickemOutcome.entryCount, false));

		yield return StartCoroutine(comicBonus.rollupNumberOfPicks(comicBonus.pickemOutcome.entryCount));
		yield return new TIWaitForSeconds(TIME_AFTER_PICK_ROLLUP);

		foreach (GameObject pickBanner in pickBanners)
		{
			if (pickBanner != clickedButtonParent) // We don't want to rereveal the one that we clicked on.
			{
				Audio.play(Audio.soundMap(REVEAL_NOT_CHOSEN));
				//This should be server driven, but it isn't so we're copying web and making a random number.
				yield return StartCoroutine(revealPickBanner(pickBanner,Random.Range(MIN_BANNER_NUMBER, MAX_BANNER_NUMBER + 1),true));
			}
		}

		yield return new TIWaitForSeconds(TIME_BEFORE_ENDING_GAME);
		endGame();
		yield break;
	}

	/// Reveals and individual pick attached to the button parent with the set amount. If it's greyed then it's a value that wasn't clicked.
	private IEnumerator revealPickBanner(GameObject buttonParent, int amount, bool isGreyed)
	{
		GameObject button = CommonGameObject.findDirectChild(buttonParent,"Button");
		GameObject slideButton = CommonGameObject.findDirectChild(buttonParent,"Slide Button"); // The parent object of the Background and and the Text that slides in.
		GameObject labelGO = CommonGameObject.findChild(slideButton,"Text");
		GameObject backgroundGO = CommonGameObject.findDirectChild(slideButton, "Background");
		UILabel label = labelGO.GetComponent<UILabel>();
		UISprite backgroundSprite = backgroundGO.GetComponent<UISprite>();
		UISprite bannerSprite = button.GetComponent<UISprite>();
		if (label == null || bannerSprite == null || backgroundSprite == null)
		{
			Debug.LogWarning("Not everything required for the banners is defined.");
			yield break; // Something went wrong and were just going to skip it.
		}
		NGUITools.SetActive(slideButton, true); // We need to activate the button, the text can't be behind the banners so we cheat it here.
		label.text = string.Format("{0}\n{1}", amount, localizedPickText); // We want the number to be on a different line
		if (isGreyed)
		{
			//Scale down all the objects in the banner
			buttonParent.transform.localScale *= bannerScaleAmount;
			//Grey out all of the widgets
			label.color = Color.grey;
			backgroundSprite.color = Color.grey;
			bannerSprite.color = Color.grey;
		}
		else // This is the winning pick. So we want to play the correct animations.
		{
			slideButton.SetActive(false); // There is a background in the animaiton, we can just use that so it doesn't look jumpy.
			yield return StartCoroutine(playChoiceCardAnimation(button));
			slideButton.SetActive(true); // Need to reactivate this because we are about to destroy the animaiton.
		}

		// Some delay so that you can see what you didn't win. (There are 6 choices so this lasts 6 times this amount)
		yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_REVEALS));
	}

	/// Plays the animation that should happen when a button is clicked on.
	private IEnumerator playChoiceCardAnimation(GameObject button)
	{
		GameObject cardAnimation = CommonGameObject.instantiate(choiceCardAnimaitonPrefab) as GameObject;
		cardAnimation.transform.parent = button.transform;
		// We want to make sure that the animation is at -10.0f so it renders over everything.
		cardAnimation.transform.localPosition = new Vector3(0.0f, 0.0f, -10.0f);
		yield return new TIWaitForSeconds(TIME_FOR_PICK_ANIMATION);
		Destroy(cardAnimation);
	}
}
