using UnityEngine;
using System.Collections;

public class LuckyElvesBonusPoolsComponent : BaseBonusPoolsComponent 
{
[System.Serializable]
	public class PresentElements
	{
		public GameObject button;
		public GameObject staticPresent;
		public GameObject revealPresent;
		public UISprite reindeer;
		public UISprite multiplier;
		public UISprite wild;
		public UITexture symbol;
		public Transform sizer;
	}
	
	public PresentElements[] presents;

	// override to handle reseting of the elements that the bonus pool you are doing is using
	protected override void resetBonusButtonElements()
	{
		// Hide the some graphics by default.
		for (int i = 0; i < 3; i++)
		{
			presents[i].reindeer.gameObject.SetActive(false);
			presents[i].multiplier.gameObject.SetActive(false);
			presents[i].wild.gameObject.SetActive(false);
			presents[i].symbol.gameObject.SetActive(false);
			presents[i].staticPresent.SetActive(true);
			presents[i].revealPresent.SetActive(false);
			CommonGameObject.colorUIGameObject(presents[i].sizer.gameObject, Color.white); // Shades Multiplier/Deer/Wild.
			CommonGameObject.colorGameObject(presents[i].sizer.gameObject, Color.white); // Shades symbol.
		}
	}

	// override to play sounds when the game is starting
	protected override void playBonusStartSounds()
	{
		// Play a randomly selected vo.
		if (Random.Range(0, 1) == 0)
		{
			Audio.play("BEPickAPresent");	
		}
		else
		{
			Audio.play("GEPickOne");
		}
	}
	
	/// Opens a single casket.
	protected override IEnumerator revealButtonObject(int index, BonusPoolItem poolItem, bool selected = true)
	{
		if (indicesRemaining.IndexOf(index) == -1)
		{
			Debug.LogError("Trying to open the same casket twice: " + index);
			yield break;
		}
		
		PresentElements present = presents[index];	// Shorthand.
		
		indicesRemaining.Remove(index);
		if (poolItem != pick)
		{
			// This is a reveal, not the pick. So remove it from the reveals list.
			reveals.Remove(poolItem);
		}
		
		// Rotate the present door open.
		float duration = 1f;
		
		// Hide the static present and show the animated one.
		// Animation will start automatically as it is activated.
		present.staticPresent.SetActive(false);
		present.revealPresent.SetActive(true);
		
		yield return new WaitForSeconds(duration * .5f);
		
		if (poolItem == pick)
		{
			Audio.play("kill_fanfares");
			Audio.play("PresentRevealContentsHol02");
			if (poolItem.reevaluations != null)
			{
				Audio.play("SCOhHOThatsWild", 1, 0, 1f);
			}
			else if (poolItem.multiplier > 1)
			{
				Audio.play("BEWoahNellie", 1, 0, 1f);
			}
		}
		
		if (poolItem.reevaluations != null)
		{
			SymbolInfo info = SlotBaseGame.instance.findSymbolInfo(fromSymbol);
			if ((info == null) || (info.getTexture() == null))
			{
				Debug.LogError("Can't find image for hol02 wild symbol: " + fromSymbol);
			}
			else
			{
				NGUIExt.applyUITexture(presents[index].symbol, info.getTexture());
			}
			
			presents[index].symbol.gameObject.SetActive(true);
			presents[index].wild.gameObject.SetActive(true);
		}
		else if (poolItem.multiplier > 1)
		{
			showMultiplier(index, string.Format("{0}x_m", poolItem.multiplier));
		}
		else
		{
			Debug.LogWarning("poolItem doesn't have anything useful.", gameObject);
		}

		// Scale the sizer after possibly attaching the reel symbol to it, so the reel symbol scales too.
		present.sizer.gameObject.transform.localScale = Vector3.one * .1f;
		// Gray out the pick.
		if (poolItem != pick)
		{
			CommonGameObject.colorUIGameObject(present.sizer.gameObject,Color.gray); // Shades Multiplier/Deer/Wild.
			CommonGameObject.colorGameObject(present.sizer.gameObject,Color.gray); // Shades symbol.
		}

		// Start revealing the contents of the box scaling it up.
		iTween.ScaleTo(present.sizer.gameObject, iTween.Hash("scale", Vector3.one, "time", duration * .5f, "easetype", iTween.EaseType.easeOutElastic));
		
		yield return new WaitForSeconds(duration * .5f);
		yield return null;	// Make sure the iTween is actually finished.
		
		// Make sure the sizer is at 1,1,1 after the delay, since it seems to get stuck mid-animation on slower devices.
		present.sizer.localScale = Vector3.one;
	
		yield return new WaitForSeconds(1f);
	}
		
	/// Shows a multiplier-type pool item.
	protected override void showMultiplier(int index, string spriteName, bool selected = true)
	{
		presents[index].reindeer.gameObject.SetActive(true);
		presents[index].multiplier.gameObject.SetActive(true);
		presents[index].multiplier.spriteName = spriteName;
		presents[index].multiplier.MakePixelPerfect();
	}
}
