using UnityEngine;
using System.Collections;
using TMPro;

/*
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
*/

public class DbsThrowBonus : TICoroutineMonoBehaviour
{
	public TextMeshPro bonusLabel;
	public Animation goAnim;
	private int _day;
	
	public void init(DbsDayBox dbsDayBox)
	{
		bonusLabel.color = dbsDayBox.dayLabelTodayColor;
		bonusLabel.text = dbsDayBox.multiplierLabel.text;
		
		_day = dbsDayBox.day;
		StartCoroutine(throwBonus());
	}
	
	private IEnumerator throwBonus()
	{
		if (goAnim != null)
		{
			goAnim.Play("DBS Throw Day " + (_day + 1).ToString() +" Anim");
			
			// Tween the parent object to the left during the animation when using VIP bonus,
			// since the destination box is moved over to make room for the VIP BONUS box.
			iTween.MoveTo(gameObject, iTween.Hash("x", -25.0f, "time", goAnim.clip.length, "islocal", true, "easetype", iTween.EaseType.easeInOutQuad));
			
			yield return new WaitForSeconds(goAnim.clip.length);
		}
	}
}
