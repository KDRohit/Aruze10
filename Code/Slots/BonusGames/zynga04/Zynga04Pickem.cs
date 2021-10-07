using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// This class is used to represent all tiles in the zynga04 game.
[System.Serializable]
public class CrosswordTile
{
	[HideInInspector] public char character;
	
	public LabelWrapperComponent characterLabel;
	public LabelWrapperComponent characterLabelGray;
	public Animator animator;

	private bool _isRevealed = false;
	public bool isRevealed
	{
		get { return _isRevealed; }
	}
	
	public void setLetter(char c, bool isLetterJUsingCustomYPos, float customJYPos)
	{
		character = c;
		characterLabel.text = c.ToString();
		characterLabelGray.text = c.ToString();

		if (c == 'J' && isLetterJUsingCustomYPos)
		{
			Vector3 currentPos = characterLabel.transform.parent.localPosition;
			characterLabel.transform.parent.localPosition = new Vector3(currentPos.x, customJYPos, currentPos.z);
		}
	}

	/// Plays reveal animation and tracks if this letter is revealed so I know when a word is fully revealed
	public void playLetterRevealAnim(string animName)
	{
		animator.Play(animName);
		_isRevealed = true;
	}
}

// This class represents a word as seen in the side panel of the game. 
[System.Serializable]
public class CrosswordWord
{
	[HideInInspector] public string word;
	[HideInInspector] public long score;

	[SerializeField] private  GameObject redScoreBarObject;
	[SerializeField] private UILabel scoreLabel;	// To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent scoreLabelWrapperComponent;

	public LabelWrapper scoreLabelWrapper
	{
		get
		{
			if (_scoreLabelWrapper == null)
			{
				if (scoreLabelWrapperComponent != null)
				{
					_scoreLabelWrapper = scoreLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_scoreLabelWrapper = new LabelWrapper(scoreLabel);
				}
			}
			return _scoreLabelWrapper;
		}
	}
	private LabelWrapper _scoreLabelWrapper = null;
	
	public List<CrosswordTile> tiles;
	[SerializeField] private Animator wordCelebAnimator;
	[SerializeField] private GameObject wordCelebSparkleObject;
	[SerializeField] private GameObject tilesShadowObject;

	private string wordCelebAnimName;
	
	/// Set the score value for this word
	public void setScore(long score)
	{
		this.score = score;
		scoreLabelWrapper.text = CreditsEconomy.convertCredits(this.score, true);
	}
	
	/// Set this word, taking care of hiding extra tiles and sizing and positioning objects that are attached to the word
	public void setWord(string word, float lastCharacterOffset, bool isLetterJUsingCustomYPos, float wordCelebScaleOffset, 
							float wordCelebXPosOffset, string wordCelebAnimName, float wordShadowScaleOffset, float wordShadowXPosOffset)
	{
		this.word = word;
		this.wordCelebAnimName = wordCelebAnimName;
		
		float lastShownTileX = 0;
		for (int i = 0; i < tiles.Count; i++)
		{
			if (i < this.word.Length)
			{
				tiles[i].setLetter(this.word[i], isLetterJUsingCustomYPos, Zynga04Pickem.NGUI_J_CHAR_WORD_LEGEND_Y_POS);
				lastShownTileX = tiles[i].animator.transform.parent.localPosition.x;
			}
			else
			{
				tiles[i].animator.transform.parent.gameObject.SetActive(false);
			}
		}

		// correctly scale and position the tile shadow
		if (tilesShadowObject != null)
		{
			float shadowScale = wordShadowScaleOffset * word.Length;
			tilesShadowObject.transform.localScale = new Vector3(shadowScale, 1.0f, 1.0f);
			Vector3 currentShadowPos = tilesShadowObject.transform.localPosition;
			tilesShadowObject.transform.localPosition = new Vector3(wordShadowXPosOffset * (tiles.Count - this.word.Length), currentShadowPos.y, currentShadowPos.z);
		}

		// correctly scale and position the sparkle celebration effect
		if (wordCelebSparkleObject != null)
		{
			float sparkleScale = wordCelebScaleOffset * word.Length;
			wordCelebSparkleObject.transform.localScale = new Vector3(sparkleScale, 1.0f, 1.0f);
			Vector3 currentCelebSparklePos = wordCelebSparkleObject.transform.localPosition;
			wordCelebSparkleObject.transform.localPosition = new Vector3(wordCelebXPosOffset * (tiles.Count - this.word.Length), currentCelebSparklePos.y, currentCelebSparklePos.z);
		}

		// adjust score bar to sit next to the last character shown
		Vector3 currentScoreBarPos = redScoreBarObject.transform.localPosition;
		redScoreBarObject.transform.localPosition = new Vector3(lastShownTileX + lastCharacterOffset, currentScoreBarPos.y, currentScoreBarPos.z);
	}

	/// Tell if this word is fully revealed
	public bool isWordFullyRevealed()
	{
		for (int i = 0; i < this.word.Length; i++)
		{
			if (!tiles[i].isRevealed)
			{
				return false;
			}
		}

		return true;
	}

	/// Play the celebration animation for this word
	public void playCelebrationAnim(string changeScoreWordColorOnWinAnimName)
	{
		if (changeScoreWordColorOnWinAnimName != "")
		{
			foreach (CrosswordTile tile in tiles)
			{
				tile.animator.Play(changeScoreWordColorOnWinAnimName);
			}
		}

		if (wordCelebAnimator != null && wordCelebAnimName != "")
		{
			wordCelebAnimator.Play(wordCelebAnimName);
		}
	}
}

public class Zynga04Pickem : PickingGame<CrosswordOutcome> 
{
	[SerializeField] private float revealAnimationDelay = 1.0f;
	[SerializeField] private float delayBeforeScoreRollup = 1.0f;
	[SerializeField] private float delayBeforeEndingGame = 3.0f;
	[SerializeField] private float delayBetweenBoardLetterReveals = 0.1f;
	[SerializeField] private float delayBetweenWordListLetterReveals = 0.1f;
	[SerializeField] private float delayBeforeRevealLeterVO = 0.2f;
	[SerializeField] private float redScoreBarLastCharacterOffset = 0.0f;				// sets how far off the last char the red bar will be placed
	[SerializeField] private bool isLetterJUsingCustomYPos = false;				// the NGUI font we use has a strange J character that will need to be positioned slightly differently
	
	[SerializeField] private string bonusRevealRoot = "WWF01BonusReveal";
	
	[SerializeField] private string bonusRevealWord = "WWF01BonusRevealWordVO";
	[SerializeField] private string tilePickSoundName = "tile_pick_up";
	
	[SerializeField] private string populateGameBoardLetter = "PopulateRevealedLettersWWF01";
	[SerializeField] private string populateJackpotListLetter = "tiles_load_1tile";
	
	[SerializeField] private string pickTileRevealAnimationName = "reveal";
	[SerializeField] private string wordTileRevealAnimationName = "small_reveal";
	[SerializeField] private string boardTileRevealAnimationName = "main_reveal";
	[SerializeField] private string winboxAnimationName = "winbox_celebration";
	[SerializeField] private string changeBoardTileColorOnWinAnimName;
	[SerializeField] private string changeScoreWordColorOnWinAnimName;
	
	[SerializeField] private string bgMusicAudioKey = "bonus_bg";
	
	[SerializeField] private Animator winboxAnimator = null;
	
	[SerializeField] private List<CrosswordWord> words;
	[SerializeField] private string wordCelebAnimName;
	[SerializeField] private float wordCelebScaleOffset;				// Offset used for each character displayed so sparkles lineup, i.e. if offset is 0.2 and there are 3 chars then total offset is 0.6
	[SerializeField] private float wordCelebXPosOffset;					// Offset used for each character displayed so sparkles lineup, i.e. if offset is -45 and there are 3 chars then total offset is -135
	[SerializeField] private float wordShadowScaleOffset;				// Offset used for each character displayed so sparkles lineup, i.e. if offset is 0.2 and there are 3 chars then total offset is 0.6
	[SerializeField] private float wordShadowXPosOffset;				// Offset used for each character displayed so sparkles lineup, i.e. if offset is -45 and there are 3 chars then total offset is -135

	[SerializeField] private List<CrosswordTile> boardTiles;
	[SerializeField] private List<PositionList> boardVerticalWordPositions;	// Word position ids from top to bottom
	[SerializeField] private List<Animator> boardVerticalCelebAnimators;	// Vertical word celebrations from left to right
	[SerializeField] private List<PositionList> boardHorizontalWordPositions;	// Word position ids from left to right
	[SerializeField] private List<Animator> boardHorizontalCelebAnimators;	// Horizontal word celebrations from top to bottom
	[SerializeField] private float boardCelebScaleOffset;				// Offset used for each character displayed so sparkles lineup with word length, this will apply.

	[System.Serializable] private class PositionList
	{
		public List<int> positions = null;
	}

	private int curPickIndex = 0;
	private List<List<CrosswordTile>> boardVerticalWords = new List<List<CrosswordTile>>();
	private List<List<CrosswordTile>> boardHorizontalWords = new List<List<CrosswordTile>>();

	public const float NGUI_J_CHAR_GRID_Y_POS = 90.29f;
	public const float NGUI_J_CHAR_WORD_LEGEND_Y_POS = 58.0f;
	public const float NGUI_J_CHAR_BUTTON_Y_POS = 81.0f;

	public override void init()
	{
		base.init();
		Audio.switchMusicKeyImmediate(Audio.soundMap(bgMusicAudioKey));

		// determine if we are using the accumulated bonus multiplier, which determines if we use the multiplier at the end of the game
		if (BonusGameManager.instance.betMultiplierOverride != -1)
		{
			BonusGamePresenter.instance.useMultiplier = false;
		}
		else
		{
			BonusGamePresenter.instance.useMultiplier = true;
		}

		// init the board word structure, used to determine when a word is complete to play a celebration animation
		foreach (PositionList posList in boardVerticalWordPositions)
		{
			boardVerticalWords.Add(new List<CrosswordTile>());
		}

		foreach (PositionList posList in boardHorizontalWordPositions)
		{
			boardHorizontalWords.Add(new List<CrosswordTile>());
		}

		populateWords();
	}
	
	private void playLetterRevealSound(char letter)
	{
		Audio.play(bonusRevealRoot + letter, 1.0f, 0.0f, delayBeforeRevealLeterVO);
	}
	
	private IEnumerator colorTilesWithLetter(char letter)
	{
		// Search board tiles for letter...
		foreach (CrosswordTile curTile in boardTiles)
		{
			if (curTile.character == letter)
			{
				Audio.play(populateGameBoardLetter);
				curTile.playLetterRevealAnim(boardTileRevealAnimationName);
				yield return new TIWaitForSeconds(delayBetweenBoardLetterReveals);
			}
		}
		
		playLetterRevealSound(letter);
		
		yield return new TIWaitForSeconds(revealAnimationDelay);
		
		// Search wordlist for the letter...
		foreach (CrosswordWord curWord in words)
		{
			foreach (CrosswordTile curTile in curWord.tiles)
			{
				if (curTile.character == letter)
				{
					Audio.play(populateJackpotListLetter);
					curTile.playLetterRevealAnim(wordTileRevealAnimationName);
					yield return new TIWaitForSeconds(delayBetweenWordListLetterReveals);
				}
			}

			if (curWord.isWordFullyRevealed())
			{
				curWord.playCelebrationAnim(changeScoreWordColorOnWinAnimName);
			}
		}

		checkBoardWordsFullyRevealedAndCelebrate();
		
		yield return new TIWaitForSeconds(revealAnimationDelay);
	}
	
	private void populateWords()
	{
		JSON paytableData = BonusGamePaytable.findPaytable("crossword", "zynga04_crossword");
		JSON[] boards = paytableData.getJsonArray("boards");
		
		foreach (JSON curBoard in boards)
		{
			if (curBoard.getString("key_name", "") == outcome.board)
			{
				JSON[] jsonWords = curBoard.getJsonArray("words");
				JSON[] sortedWords = jsonWords.OrderByDescending(x => x.getLong("credits", 0)).ToArray();
				
				for (int i = 0; i < sortedWords.Length; i++)
				{
					string theWord = sortedWords[i].getString("word", "");

					long creditsMultiplier;
					// account for a possible multiplier override such as the one used for cumulative bonuses like in zynga04
					if (BonusGameManager.instance.betMultiplierOverride != -1)
					{
						creditsMultiplier = BonusGameManager.instance.betMultiplierOverride;
					}
					else
					{
						creditsMultiplier = GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers;
					}

					long credits = sortedWords[i].getLong("credits", 0) * creditsMultiplier;
					words[i].setScore(credits);
					words[i].setWord(theWord, 
									redScoreBarLastCharacterOffset, 
									isLetterJUsingCustomYPos, 
									wordCelebScaleOffset, 
									wordCelebXPosOffset, 
									wordCelebAnimName, 
									wordShadowScaleOffset, 
									wordShadowXPosOffset);
				}
				
				JSON[] positions = curBoard.getJsonArray("positions");
				
				for (int i = 0; i < positions.Length; i++)
				{
					int position = positions[i].getInt("position", 0);
					int index = position - 1;
					
					if (index != -1)
					{
						string letter = positions[i].getString("letter", "");
						boardTiles[index].setLetter(letter[0], isLetterJUsingCustomYPos, 90.29f);

						// check what word to add this tile to in both directions
						if (boardVerticalWordPositions != null)
						{
							for (int k = 0; k < boardVerticalWordPositions.Count; k++)
							{
								if (boardVerticalWordPositions[k].positions.Contains(position))
								{
									boardVerticalWords[k].Add(boardTiles[index]);
								}
							}
						}

						if (boardHorizontalWordPositions != null)
						{
							for (int k = 0; k < boardHorizontalWordPositions.Count; k++)
							{
								if (boardHorizontalWordPositions[k].positions.Contains(position))
								{
									boardHorizontalWords[k].Add(boardTiles[index]);
								}
							}
						}
					}
					else
					{
						Debug.LogError("Position index resolved to -1. This should never happen! Something is wrong with JSON data.");
					}
				}
				
				break;
			}
		}
	}

	/// Determine if each board word is fully revealed and celebrate them if they are
	private void checkBoardWordsFullyRevealedAndCelebrate()
	{
		for (int i = 0; i < boardVerticalWords.Count; i++)
		{
			bool isWordFullyRevealed = true;

			foreach (CrosswordTile tile in boardVerticalWords[i])
			{
				if (!tile.isRevealed)
				{
					isWordFullyRevealed = false;
					break;
				}
			}

			if (isWordFullyRevealed)
			{
				// Change the color of the text on the board for words that are fully matched
				if (changeBoardTileColorOnWinAnimName != "")
				{
					foreach (CrosswordTile tile in boardVerticalWords[i])
					{
						tile.animator.Play(changeBoardTileColorOnWinAnimName);
					}
				}

				// scale the celebration to the number of characters in the word and turn it on to play it
				Animator boardCeleb = boardVerticalCelebAnimators[i];
				boardCeleb.transform.localScale = new Vector3(1.0f, boardVerticalWords[i].Count * boardCelebScaleOffset, 1.0f);
				boardCeleb.gameObject.SetActive(true);
			}
		}

		for (int i = 0; i < boardHorizontalWords.Count; i++)
		{
			bool isWordFullyRevealed = true;

			foreach (CrosswordTile tile in boardHorizontalWords[i])
			{
				if (!tile.isRevealed)
				{
					isWordFullyRevealed = false;
					break;
				}
			}

			if (isWordFullyRevealed)
			{
				// Change the color of the text on the board for words that are fully matched
				if (changeBoardTileColorOnWinAnimName != "")
				{
					foreach (CrosswordTile tile in boardHorizontalWords[i])
					{
						tile.animator.Play(changeBoardTileColorOnWinAnimName);
					}
				}
				
				// scale the celebration to the number of characters in the word and turn it on to play it
				Animator boardCeleb = boardHorizontalCelebAnimators[i];
				boardCeleb.transform.localScale = new Vector3(boardHorizontalWords[i].Count * boardCelebScaleOffset, 1.0f, 1.0f);
				boardCeleb.gameObject.SetActive(true);
			}
		}
	}
	
	private IEnumerator rollupScore()
	{
		if (winboxAnimator != null)
		{
			winboxAnimator.Play(winboxAnimationName);
		}

		long creditsMultiplier;
		// account for a possible multiplier override such as the one used for cumulative bonuses like in zynga04
		if (BonusGameManager.instance.betMultiplierOverride != -1)
		{
			creditsMultiplier = BonusGameManager.instance.betMultiplierOverride;
		}
		else
		{
			creditsMultiplier = GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers;
		}
		
		yield return StartCoroutine(addCredits(outcome.getTotalCredits() * creditsMultiplier));
	}
	
	protected override IEnumerator pickemButtonPressedCoroutine(GameObject buttonObj)
	{
		inputEnabled = false;
		
		CrosswordPick pick = outcome.getNextEntry();
		
		curPickIndex = getButtonIndex(buttonObj);
		removeButtonFromSelectableList(buttonObj);
		
		Animator anim = roundButtonList[currentStage].animatorList[curPickIndex];
		UILabel label = roundButtonList[currentStage].revealNumberList[curPickIndex];

		if (pick.letter == 'J' && isLetterJUsingCustomYPos)
		{
			Vector3 currentPos = label.transform.parent.localPosition;
			label.transform.parent.localPosition = new Vector3(currentPos.x, NGUI_J_CHAR_BUTTON_Y_POS, currentPos.z);
		}

		label.text = pick.letter.ToString();
		Audio.play(tilePickSoundName);
		anim.Play(pickTileRevealAnimationName);
		
		yield return new TIWaitForSeconds(revealAnimationDelay);
		
		yield return StartCoroutine(colorTilesWithLetter(pick.letter));
		
		// If there is nothing left in the entris list, we are done, end the game
		if (outcome.lookAtNextEntry() == null)
		{
			Audio.play(bonusRevealWord);
			yield return new TIWaitForSeconds(delayBeforeScoreRollup);
			yield return StartCoroutine(rollupScore());
			yield return new TIWaitForSeconds(delayBeforeEndingGame);
			BonusGamePresenter.instance.gameEnded();
		}
		else
		{
			inputEnabled = true;
		}
	}
}


