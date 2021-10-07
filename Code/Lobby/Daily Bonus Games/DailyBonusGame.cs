using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class DailyBonusGame : TICoroutineMonoBehaviour
{
	protected bool _isDone = false;
	protected int _winAmount = 0;
	
	public DbsScoreBox coinsScoreBox;
	public DbsScoreBox multiplierScoreBox;
	public DbsScoreBox totalScoreBox;
	public int amountWon = 0;
	
	public void setScoreBoxes(DbsScoreBox newCoinsScoreBox,DbsScoreBox newMultiplierScoreBox,DbsScoreBox newTotalScoreBox)
	{
		coinsScoreBox = newCoinsScoreBox;
		coinsScoreBox.scoreLabel.text = "";
		
		multiplierScoreBox = newMultiplierScoreBox;
		multiplierScoreBox.scoreLabel.text = "";
		
		totalScoreBox = newTotalScoreBox;
		totalScoreBox.scoreLabel.text = "";
	}

	public abstract void init(JSON data);

	public bool isDone()
	{
		return (_isDone);
	}
	
	public virtual void Update()
	{
	
	}
}
