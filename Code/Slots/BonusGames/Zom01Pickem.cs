using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Zom01Pickem : PickingGame<PickemOutcome> 
{	
	public GameObject shotgunShellsParent;
	public ZombieHuntShotgunShell shotgunShellPrefab;
	public ZombieHuntZombie[] zombies;
	public UILabel scoreLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent scoreLabelWrapperComponent;

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
	
	public GameObject zombieRevealVfxPrefab;
	public GameObject zombieCaptionVfxPrefab;
	public GameObject multiplierExplosionVfxPrefab;
	public GameObject fireTrailVfxPrefab;
	public GameObject gameOverOverlay;
	public UILabelStyle revealStyle;

	private bool isGameOver = false;
	private List<ZombieHuntZombie> activeZombies;
	private List<ZombieHuntShotgunShell> shotgunShells;
	private PlayingAudio ambianceLoop;

	// Number of shotgun shells
	private const int NUM_SHOTGUN_SHELLS = 8;

	// Sound constants
	private const string AMBIANCE_LOOP = "ZAAmbienceLoop";
	private const string ZOMBIE_MOAN = "ZombieMoans";
	private const string GAME_OVER_SOUND = "BioHazard";
	private const string HEAD_SPLAT = "HeadSplat";
	private const string SHOTGUN_COCK = "ShotgunCock";
	private const string SPENT_SHELL_DROP = "spent_shell_drop";
	private const string SHELL_EXPLOSION = "shell_explosion";
	private const string SHOTGUN_BLAST = "ZAShotgunBlast";

	[SerializeField] private float SHUTGUN_SHELL_SPACING;
	[SerializeField] private float TIME_BETWEEN_REVEALS;
	[SerializeField] private float SHELL_ACTIVATE_DELAY;
	[SerializeField] private float ZOMBIE_PICK_DELAY;
	[SerializeField] private float FIRE_TRAIL_DELAY;
	[SerializeField] private float SCORE_SCALE_DELAY ;
	[SerializeField] private float VALUE_REVEAL_DELAY;
	[SerializeField] private float SHELL_FADE_FROM; // 1.0f,
	[SerializeField] private float SHELL_FADE_TO; // 0.0f,
	[SerializeField] private float SHELL_FADE_TIME; // 0.65f,
	
	/// Handle initialization stuff for the game
	public override void init()
	{
		base.init();
		List<PickemPick> picks = outcome.entries;
		
		ambianceLoop = Audio.play(AMBIANCE_LOOP, 1.0f, 0.0f, 0.0f, float.PositiveInfinity);
		// create the shotgun shells
		shotgunShells = new List<ZombieHuntShotgunShell>();
		for (int i = 0; i < NUM_SHOTGUN_SHELLS; i++)
		{
			GameObject shellObj = CommonGameObject.instantiate(shotgunShellPrefab.gameObject) as GameObject;
			shellObj.transform.parent = shotgunShellsParent.transform;
			shellObj.transform.localPosition = new Vector3(i * SHUTGUN_SHELL_SPACING, 0.0f, 0.0f);
			shellObj.transform.localScale = Vector3.one;
			ZombieHuntShotgunShell shell = shellObj.GetComponent<ZombieHuntShotgunShell>();
			long multiplier = 1;
			if (i < picks.Count && !picks[i].isGameOver)
			{
				multiplier = picks[i].multiplier + 1;
			}
			else
			{
				JSON modifiers = outcome.modifiers;
				List<string> modifiersKeyList = modifiers.getKeyList();
				string randomKey = modifiersKeyList[Random.Range(0, modifiersKeyList.Count)];
				multiplier = modifiers.getJSON(randomKey).getInt("instant_multiplier", 0) + 1;
			}
			shellObj.SetActive(true);
			shell.labelWrapper.text = Localize.text("{0}X",multiplier);
			shotgunShells.Add(shell);
		}
		StartCoroutine(makeShellActive(shotgunShells[0]));	// make the first shell active
		
		activeZombies = new List<ZombieHuntZombie>(zombies);
		
		scoreLabelWrapper.text = CreditsEconomy.convertCredits(0);
	}

	private IEnumerator makeShellActive(ZombieHuntShotgunShell shell)
	{
		shell.baseAnim.SetTrigger("Grow");
		
		yield return new WaitForSeconds(SHELL_ACTIVATE_DELAY);
		
		if (!shell.fired)
		{
			shell.highlightAnim.gameObject.SetActive(true);
			shell.highlightAnim.SetTrigger("Pulse");
		}
	}

	private IEnumerator revealPick(ZombieHuntZombie zombie, PickemPick pick, bool wasPicked = true)
	{
		GameObject visuals = zombie.visuals;
		ZombieHuntShotgunShell shell = null;
		
		// create the zombie reveal vfx and wait for it to finish
		
		bool isGameOverPick = (pick == null || pick.isGameOver || pick.credits <= 0);
		isGameOver = isGameOver || isGameOverPick;
		
		if (wasPicked)
		{
			activeZombies.Remove(zombie);
			shell = shotgunShells[0];
			shotgunShells.RemoveAt(0);
			
			fireShell(shell, isGameOverPick);
		}
		
		LabelWrapper label = zombie.scoreLabelWrapper;
		
		if (!wasPicked)
		{
			label.color = Color.gray;
			
			// Hide the zombie and show the score.
			visuals.SetActive(false);
			label.gameObject.SetActive(true);
		}
		
		if (isGameOverPick)
		{
			visuals.SetActive(false);
			
			if (shell != null)
			{
				shell.labelWrapper.gameObject.SetActive(false);
			}
			label.gameObject.SetActive(false);
			zombie.gameOverTexture.gameObject.SetActive(true);
			if (!wasPicked)
			{
				zombie.gameOverTexture.color = Color.gray;
				// splat sound will be played lower down in a not picked block
			}
			else
			{
				Audio.play(GAME_OVER_SOUND);
				gameOverOverlay.SetActive(true);
			}
		}
		
		if (pick != null && pick.credits > 0)
		{
			long credits = pick.credits;
			long multiplier = pick.multiplier + 1;
			long adjustedScore = credits * multiplier;
			label.text = CreditsEconomy.convertCredits(credits);
			
			// animate the multiplier flying to the score and changing it
			if (wasPicked && multiplier > 1)
			{
				// spawn a fire trail and wait for it to get to the score
				VisualEffectComponent fireTrailVfx = VisualEffectComponent.Create(fireTrailVfxPrefab, zombie.effectAnchor);
				fireTrailVfx.transform.position = shell.transform.position;
				LabelWrapper shellLabel = shell.labelWrapper;
				shellLabel.transform.parent = fireTrailVfx.transform;	// move the multiplier label so it follows the trail
				shellLabel.transform.localPosition = Vector3.zero;
				TweenPosition.Begin(fireTrailVfx.gameObject, FIRE_TRAIL_DELAY, label.transform.localPosition);
				yield return new WaitForSeconds(FIRE_TRAIL_DELAY);
				
				Audio.play(HEAD_SPLAT);
				VisualEffectComponent revealVfx = VisualEffectComponent.Create(zombieRevealVfxPrefab, zombie.effectAnchor);
				// play until the effect ends.
				while (revealVfx != null)
				{
					yield return null;
				}
				
				// Hide the zombie and show the score.
				visuals.SetActive(false);
				label.gameObject.SetActive(true);
				
				label.text = "";	// hide the text so it isn't over the explosion, will transition the new score in shortly
				
				VisualEffectComponent explosionVfx = VisualEffectComponent.Create(multiplierExplosionVfxPrefab, zombie.effectAnchor);
				yield return new WaitForSeconds(explosionVfx.Duration / 2);
				
				// about halfway through the explosion effect, start scaling up the score label with the adjusted score
				label.text = CreditsEconomy.convertCredits(adjustedScore);
				Vector3 originalScale = label.transform.localScale;
				label.transform.localScale = Vector3.zero;
				TweenScale.Begin(label.gameObject, 0.25f, originalScale);
				yield return new WaitForSeconds(SCORE_SCALE_DELAY);
			}
			
			if (wasPicked)
			{
				yield return null;	// wait until next frame so that the click from this input doesn't cancel the rollup
				yield return StartCoroutine(SlotUtils.rollup(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout + adjustedScore, scoreLabelWrapper));
				if (!isGameOverPick)
				{
					Audio.play(SHOTGUN_COCK);
				}
				BonusGamePresenter.instance.currentPayout += adjustedScore;
			}
			else
			{
				UILabelStyler styler;
				styler = label.gameObject.GetComponent<UILabelStyler>();
				
				if (styler != null)
				{
					styler.style = revealStyle;
					styler.updateStyle();
				}
				if(!revealWait.isSkipping)
				{
					Audio.play(HEAD_SPLAT);
				}
			}
		}
		
		if (!isGameOverPick && shell != null)
		{
			// rotate the spent shell out
			shell.explosionAnim.Play(SPENT_SHELL_DROP);
			
			// fade the shell as it rotates out
			iTween.FadeTo (
				shell.gameObject,
				iTween.Hash (
					"from",	// SHELL_FADE_FROM, SHELL_FADE_TO, SHELL_FADE_TIME
					SHELL_FADE_FROM, // 1.0f,
					"to",
					SHELL_FADE_TO, // 0.0f,
					"time",
					SHELL_FADE_TIME, // 0.65f,
					"onupdate",
					"fadeOutShell",
					"onupdatetarget",
					gameObject
				)
			);
			
			// slide the other shells over
			TweenPosition.Begin(shotgunShellsParent, 0.25f, shotgunShellsParent.transform.localPosition + new Vector3(-SHUTGUN_SHELL_SPACING, 0.0f, 0.0f));
			if (shotgunShells.Count > 0)
			{
				StartCoroutine(makeShellActive(shotgunShells[0]));
			}
		}
	}

	protected override IEnumerator pickMeAnimCallback()
	{
		while(activeZombies.Count > 0 && !isGameOver)
		{
			if (!inputEnabled)
			{
				yield return null;    // don't speak if input isn't enabled, come back next frame
			}
			
			ZombieHuntZombie zombie = activeZombies[Random.Range(0, activeZombies.Count)];
			if (zombie.captionAnim != null /*&& zombie != lastZombie*/)
			{
				zombie.captionAnim.SetTrigger("Speak");
				Audio.play(ZOMBIE_MOAN);
				yield return new TIWaitForSeconds(Random.Range(minPickMeTime, maxPickMeTime));
			}
			yield return null;
		}
	}

	private void fireShell(ZombieHuntShotgunShell shell, bool isGameOverPick)
	{
		// skip the shotgun blast on a gameover
		if (!isGameOverPick)
		{
			Audio.play(SHOTGUN_BLAST);
		}
		
		shell.fired = true;
		
		shell.highlightAnim.gameObject.SetActive(false);
		shell.baseAnim.gameObject.SetActive(false);
		shell.explosionAnim.gameObject.SetActive(true);
		
		shell.explosionAnim.Play(SHELL_EXPLOSION);
	}
	
	/// Called when a button is pressed
	protected override IEnumerator pickemButtonPressedCoroutine(GameObject button)
	{
		GameObject zombieObj = button.transform.parent.parent.gameObject;
		ZombieHuntZombie zombie = zombieObj.GetComponent<ZombieHuntZombie>();

		if (zombie == null) 
		{
			yield break;
		}
		
		inputEnabled = false;
		
		PickemPick pick = outcome.getNextEntry();
		yield return StartCoroutine(revealPick(zombie, pick));
		
		if (pick == null || pick.isGameOver || outcome.entryCount == 0)
		{
			// no picks left or the bad zombie was pickeed, the game is over
			// reveal the remaining values
			yield return new WaitForSeconds(VALUE_REVEAL_DELAY);
			
			foreach (ZombieHuntZombie zom in activeZombies)
			{
				PickemPick reveal = outcome.getNextReveal();
				yield return StartCoroutine(revealPick(zom, reveal, false));
				
				yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_REVEALS));
			}
			
			yield return new WaitForSeconds(ZOMBIE_PICK_DELAY);
			
			Audio.stopSound(ambianceLoop);
			
			BonusGamePresenter.instance.gameEnded();
		}
		else
		{
			inputEnabled = true;
		}

		yield return null;
	}
}

