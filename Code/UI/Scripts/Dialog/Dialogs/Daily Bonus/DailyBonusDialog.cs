using UnityEngine;
using System.Collections;
using Com.Scheduler;
using TMPro;

/*
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
*/

public class DailyBonusDialog : DialogBase
{
	// top group
	public GameObject topGroup;
	public UIGrid dayGrid;
	public DbsDayBox dayBoxPrefab;
	private DbsDayBox[] _dbsDayBoxes;

	[SerializeField] private DbsThrowBonus _throwBonusPrefab;

	// score group
	public DbsScoreBox coinsScoreBox;
	public DbsScoreBox multiplierScoreBox;
	public DbsScoreBox totalScoreBox;

	// win group
	public GameObject winGroup;
	public GameObject winCoinAnchor;
	private CoinScript winCoin;
	public TextMeshPro winCreditsLabel;
	public DbsScoreBox streakScoreBox;
	public DbsScoreBox friendsScoreBox;
	public DbsScoreBox vipBonusScoreBox;
	public DbsScoreBox winScoreBox;
	public UILabelStyle redLabelStyle;
	public UIButtonColor[] redButtonColors;
	
	private long streakCredits;
	private long friendsCredits;
	private long vipCredits;
	
	// bottom broup
	public GameObject bottomGroup;

	public GameObject gameAnchor;
	public DailyBonusGame dailyTripleWheel;
	
	public UIImageButton continueButton;
	
	public GameObject lilCoin;
	public GameObject lilCoinAnimSpot;
	public Animation lilCoinAnim;
	private CoinScript lilCoinScript;
	public GameObject lilCoinEffectPrefab;
	private ParticleSystem lilCoinEffect;
	
	private DailyBonusGame _game;
	private bool gameEnded = false;
	private long winScore = 0;
	private bool isReadyToClose = false;
		
	public override void init()
	{
		// Enable mouse input since it gets disabled when clicking the daily bonus button in lobby,
		// to prevent other things from being clicked while waiting for the response from the server.
		// Maybe we should show a "please wait" dialog while it waits, or maybe even the loading screen.
		NGUIExt.enableAllMouseInput();

		initTopGroup();
		initWinGroup();
		initGame();
		initBottomGroup();
	}

	protected void initTopGroup()
	{
		DbsDayBox.setToday();	// This is used for all the days, and beyond the 7th day.
		
		_dbsDayBoxes = new DbsDayBox[7];
		
		for (int iDay = 0; iDay < 7; iDay++)
		{
			GameObject dayBoxObject = NGUITools.AddChild(dayGrid.gameObject, dayBoxPrefab.gameObject);
			DbsDayBox dayBox = dayBoxObject.GetComponent<DbsDayBox>();

			dayBox.init(iDay);
			
			_dbsDayBoxes[iDay] = dayBox;
			
			if (iDay + 1 == DbsDayBox.today) //Index vs actual day.
			{
				float duration = 0.25f;
				float degrees = 5.0f;
				dayBox.animSpot.transform.localEulerAngles = new Vector3(0, 0, -degrees);
				iTween.RotateBy(dayBox.animSpot, iTween.Hash("z", degrees * 2 / 360.0f, "looptype", iTween.LoopType.pingPong, "easetype", iTween.EaseType.easeInOutQuad, "time", duration));
			}
		}

		if (DbsDayBox.today > 7)
		{
			// Use the bonus from day 7 if we're beyond that.
			streakCredits = _dbsDayBoxes[6].bonus;
		}
		else
		{
			streakCredits = _dbsDayBoxes[DbsDayBox.today - 1].bonus;			
		}
	}

	protected void initWinGroup()
	{
		if (SlotsPlayer.isSocialFriendsEnabled)
		{
			friendsScoreBox.buttonCollider.enabled = false;
		}

		winGroup.SetActive(false);
		streakScoreBox.gameObject.SetActive(false);
		friendsScoreBox.gameObject.SetActive(false);
		vipBonusScoreBox.gameObject.SetActive(false);
		winScoreBox.gameObject.SetActive(false);
	}

	protected void initGame()
	{
		JSON data = dialogArgs.getWithDefault(D.DAILY_BONUS_DATA, null) as JSON;

		if (DailyBonusButton.instance != null)
		{
			// This will be null if in a game while doing the bonus, which can happen from the path to riches feature.
			DailyBonusButton.instance.resetTimer();
		}

		// Discover the bonus game type from the outcome data
		string bonusType = "";
		foreach (JSON outcome in data.getJsonArray("outcomes"))
		{
			if (outcome.hasKey("bonus_game"))	
			{
				bonusType = outcome.getString("bonus_game", "");
				break;
			}
		}
		
		Bugsnag.LeaveBreadcrumb("Attempting to open the daily bonus game " + bonusType);
		
		if (data.hasKey("active_friend_bonus"))
		{
			friendsCredits = data.getLong("active_friend_bonus",0);
			friendsScoreBox.scoreLabel.text = CreditsEconomy.convertCredits(friendsCredits);
		}

		vipCredits = data.getLong("vip_level_bonus", 0);
		vipBonusScoreBox.scoreLabel.text = CreditsEconomy.convertCredits(vipCredits);
		
		GameObject go = null;

		switch (bonusType)
		{
			case "daily_triple_wheel":
				// Sandwich the mini-game in between the bottom layer and the top layer.
				go = NGUITools.AddChild(gameAnchor , dailyTripleWheel.gameObject);
				go.transform.localPosition = dailyTripleWheel.transform.localPosition;
				break;
		}

		if (go != null)
		{
			_game = go.GetComponent<DailyBonusGame>();
		}
		else
		{
			_game = null;
		}

		if (_game != null)
		{
			_game.setScoreBoxes(coinsScoreBox,multiplierScoreBox,totalScoreBox);
			_game.init(data);
		   
			// Stop the background music. See Chris' comments in HIR-16290.
			Audio.switchMusicKey("");
			Audio.stopMusic();
		}
		else
		{
			// Unsupported bonus game type
			Debug.LogWarning("Unsupported daily bonus game type: " + bonusType);

			long winnings = Server.shouldHaveCredits - SlotsPlayer.creditAmount;
			if (winnings > 0)
			{
				SlotsPlayer.addCredits(winnings, "daily bonus");
				Audio.play("fastsparklyup1");
			}

			Dialog.close();
		}

		StatsManager.Instance.LogCount("dialog", "free_bonus", "lobby", "day_" + SlotsPlayer.instance.dailyBonusTimer.day, "", "view");
	}

	protected void initBottomGroup()
	{
		if (_game != null)
		{
			bottomGroup.SetActive(false);
		}
	}

	public void Update()
	{
		if (_game != null)
		{
			_game.Update();

			if (_game.isDone())
			{
				if (!bottomGroup.activeSelf)
				{
					//We want to fade out the game here.
					if (!gameEnded)
					{
						StartCoroutine(endGame());
						gameEnded = true;
					}
				}
			}
		}
		
		if (isReadyToClose)
		{
			if (shouldAutoClose)
			{
				clickContinue();
			}
			AndroidUtil.checkBackButton(clickContinue);
		}
	}

	private IEnumerator endGame()
	{
		Audio.play ("SummaryReveal4");
		Audio.play("cheer_dailybonus");
		//Fade out the game and fade in the background
		UIPanel gamePanel = _game.GetComponent<UIPanel>();
		UIPanel winPanel = winGroup.GetComponent<UIPanel>();
		winGroup.SetActive(true);

		winCoin = CoinScript.create(winCoinAnchor.transform , winCoinAnchor.transform.position , Vector3.zero);
		winCoin.transform.localScale = new Vector3(2.5f , 2.5f , 2.5f);
		CommonGameObject.alphaGameObject(winCoin.gameObject,0f);
		winCoin.spin();

		winPanel.alpha = 0;

		winCreditsLabel.text = CreditsEconomy.convertCredits(SlotsPlayer.creditAmount);

		//gamePanel.SetAlphaRecursive(0,true); //No idea what this is going to do. The true flag that is
		const float fadeTime = 1.25f;
		StartCoroutine(fadeOutPanel(gamePanel,fadeTime));
		yield return StartCoroutine(fadeInPanel(winPanel,fadeTime));

		coinsScoreBox.gameObject.SetActive(false);
		multiplierScoreBox.gameObject.SetActive(false);
				
		if (totalScoreBox.goAnim != null)
		{
			totalScoreBox.goAnim.Play("DBS Throw Score");

			// Tween the parent object to the left during the animation when using VIP bonus,
			// since the destination box is moved over to make room for the VIP BONUS box.
			float newX = totalScoreBox.transform.localPosition.x - 70.0f;
			iTween.MoveTo(totalScoreBox.gameObject, iTween.Hash("x", newX, "time", totalScoreBox.goAnim.clip.length, "islocal", true, "easetype", iTween.EaseType.easeInOutQuad));

			yield return new WaitForSeconds(totalScoreBox.goAnim.clip.length);
		}
		yield return new WaitForSeconds(0.025f);

		//Clamp to maxof 7 days streak,  this will ensure it uses the 7 day bonus for all days after the 7th.
		DbsDayBox dbsDayBox = _dbsDayBoxes[ (Mathf.Min(_dbsDayBoxes.Length - 1, DbsDayBox.today-1)) ];
		GameObject throwObject = NGUITools.AddChild(dbsDayBox.throwAnchor , _throwBonusPrefab.gameObject);
		DbsThrowBonus throwBonus = throwObject.GetComponent<DbsThrowBonus>();
		throwBonus.init(dbsDayBox);
		
		streakScoreBox.gameObject.SetActive(true);
		streakScoreBox.scoreLabel.text = "";
		if (streakScoreBox.goAnim != null)
		{
			streakScoreBox.goAnim.Play("DBS Score Intro");
			yield return new WaitForSeconds(streakScoreBox.goAnim.clip.length);
		}
		yield return new WaitForSeconds(0.025f);
		
		friendsScoreBox.gameObject.SetActive(true);
		if (friendsScoreBox.goAnim != null)
		{
			friendsScoreBox.goAnim.Play("DBS Score Intro");
			yield return new WaitForSeconds(friendsScoreBox.goAnim.clip.length);
		}
		yield return new WaitForSeconds(0.025f);
		
		vipBonusScoreBox.gameObject.SetActive(true);
		if (vipBonusScoreBox.goAnim != null)
		{
			vipBonusScoreBox.goAnim.Play("DBS Score Intro");
			yield return new WaitForSeconds(vipBonusScoreBox.goAnim.clip.length);
		}
		yield return new WaitForSeconds(0.025f);

		winScoreBox.gameObject.SetActive(true);
		
		if (winScoreBox.effectPrefab != null)
		{
			GameObject effectObject = NGUITools.AddChild(winScoreBox.effectAnchor , winScoreBox.effectPrefab);
			ParticleSystem effect = effectObject.GetComponentInChildren<ParticleSystem>();
			effect.Play();
		}
		
		winScore = _game.amountWon + streakCredits + friendsCredits + vipCredits;
		winScoreBox.scoreLabel.text = CreditsEconomy.convertCredits(winScore);
		
		if (winScoreBox.goAnim != null)
		{
			winScoreBox.goAnim.Play("DBS Score Intro");
			yield return new WaitForSeconds(winScoreBox.goAnim.clip.length);
		}
		yield return new WaitForSeconds(0.025f);
		
		_game.gameObject.SetActive(false);
		
		bottomGroup.SetActive(true);
		Animation bottomPanelAnim = bottomGroup.GetComponent<Animation>();
		if (bottomPanelAnim != null)
		{
			bottomPanelAnim.Play("DBS Bottom Panel Intro");
			yield return new WaitForSeconds(bottomPanelAnim.clip.length);
		}

		string totalScore = totalScoreBox.scoreLabel.text;
		string amountWon = CreditsEconomy.convertCredits(_game.amountWon);
		if (totalScore != amountWon)
		{
			Debug.LogError(string.Format("Daily Bonus amount is wrong reading {0} should be {1}.", totalScore, amountWon));
		}

		// Stop the music and play the db summary sounds.
		Audio.play("RevealLoopTerm");
		Audio.play("SummaryTotal");

		isReadyToClose = true;
	}

	//Fades out a panel and all of its childen over the time with the specified number of steps. This also grabes all facebook images and fades those.
	private IEnumerator fadeOutPanel(UIPanel panel, float time)
	{
		float progress = time;

		while (progress > 0f)
		{
			progress -= Time.deltaTime;

			if (progress < 0f)
			{
				progress = 0f;
			}

			FacebookFriendInfo[] facebookFriends = panel.gameObject.GetComponentsInChildren<FacebookFriendInfo>();

			panel.SetAlphaRecursive(progress/time, false);
			foreach (FacebookFriendInfo friend in facebookFriends)
			{
				if (friend != null)
				{
					Color c = friend.image.material.color;
					c.a = progress/time;
					friend.image.material.color = c;
				}
			}

			yield return null;
		}
	}

	//Fades in a panel and all of its childen over the time with the specified number of steps.
	private IEnumerator fadeInPanel(UIPanel panel, float time)
	{
		float progress = 0f;

		while (progress < time)
		{
			progress += Time.deltaTime;

			if (progress > time)
			{
				progress = time;
			}

			panel.SetAlphaRecursive(progress/time, false);
			CommonGameObject.alphaGameObject(winCoin.gameObject , progress/time);

			yield return null;
		}
	}

	public void clickContinue()
	{
		isReadyToClose = false;	// Prevent multiple calls to this function automatically.
		continueButton.isEnabled = false;
		continueButton.GetComponent<Collider>().enabled = false;
		Audio.play("dailyBonusPickItem");
		StartCoroutine(rollUpCredits());
	}
	
	private IEnumerator rollUpCredits()
	{
		lilCoinScript = CoinScript.create(lilCoinAnimSpot.transform , lilCoinAnimSpot.transform.position , Vector3.zero);
		//lilCoin.transform.localScale = new Vector3(2.5f , 2.5f , 2.5f);
		lilCoinScript.spin();
		
		if (lilCoinEffectPrefab != null)
		{
			GameObject effectObject = NGUITools.AddChild(lilCoinAnimSpot , lilCoinEffectPrefab);
			lilCoinEffect = effectObject.GetComponentInChildren<ParticleSystem>();
			lilCoinEffect.Play();
		}
		
		Animation lilCoinAnim = lilCoin.GetComponent<Animation>();
		
		if (lilCoinAnim != null)
		{
			lilCoinAnim.Play("DBS Throw Lil Coin");
			yield return new WaitForSeconds(lilCoinAnim.clip.length);
		}
		yield return new WaitForSeconds(0.1f);
	
		GameObject.Destroy(lilCoinScript.gameObject);
		yield return new WaitForSeconds(0.5f);
		
		lilCoinEffect.Stop();
				
		yield return
			StartCoroutine(
				SlotUtils.rollup(
					SlotsPlayer.creditAmount,
					SlotsPlayer.creditAmount + winScore,
					winCreditsLabel,
					true));
		yield return new WaitForSeconds(2.5f);

		SlotsPlayer.addCredits(winScore, "daily bonus");
		
		Dialog.close();

		Audio.play("fastsparklyup1");
		
		if (GameState.isMainLobby)
		{
			// Set the background music back to the lobby.
			MainLobby.playLobbyMusic();
		}
	}
	
	public void clickedFacebookButton()
	{
		if (!SlotsPlayer.isFacebookUser)
		{
			GenericDialog.showDialog(
				Dict.create(
					D.TITLE, Localize.text("facebook_login"),
					D.MESSAGE, Localize.text("facebook_daily_bonus_message"),
					D.OPTION1, Localize.text("yes"),
					D.OPTION2, Localize.text("no"),
					D.REASON, "daily-bonus-dialog-facebook-login",
					D.CALLBACK, new DialogBase.AnswerDelegate(FacebookCallback),
					D.STACK, true
				)
			);
		}
	}

	private static void FacebookCallback(Dict answerArgs)
	{
		if ((answerArgs[D.ANSWER] as string) == "1")
		{
			SlotsPlayer.facebookLogin();
			StatsManager.Instance.LogCount("daily_bonus_game", "fbAuth", "", "", "", "click");
		}
	}
	
	/// Called by Dialog.close() - do not call directly.
	public override void close()
	{
		NotificationManager.ShowPushNotifSoftPrompt();
	}
	
	public static void showDialog(JSON data)
	{
		Scheduler.addDialog("daily_bonus", Dict.create(D.DAILY_BONUS_DATA, data), SchedulerPriority.PriorityType.IMMEDIATE);
	}
}
