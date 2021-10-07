using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Used as a way for the player to choose a casket on the Elvira game, acts as a component attached to the game, instead of the old way which used a dialog
**/
public class ElviraBonusPoolsComponent : BaseBonusPoolsComponent
{
	[System.Serializable]
	public class CasketElements
	{
		public GameObject button;
		public GameObject casket;
		public Renderer renderer;
		public Transform casketHinge;
		public UISprite ghost;
		public UISprite multiplier;
		public UISprite wild;
		public UITexture symbol;
		public Transform sizer;
	}

	public CasketElements[] caskets;

	// override to handle reseting of the elements that the bonus pool you are doing is using
	protected override void resetBonusButtonElements()
	{
		// Hide the some graphics by default.
		for (int i = 0; i < 3; i++)
		{
			caskets[i].ghost.gameObject.SetActive(false);
			caskets[i].multiplier.gameObject.SetActive(false);
			caskets[i].wild.gameObject.SetActive(false);
			caskets[i].symbol.gameObject.SetActive(false);
			caskets[i].casketHinge.localEulerAngles = Vector3.zero;
			caskets[i].sizer.localScale = Vector3.one;
			caskets[i].ghost.color = Color.white;
			caskets[i].multiplier.color = Color.white;
			caskets[i].wild.color = Color.white;
			caskets[i].symbol.color = Color.white;
		}
	}

	// override to play sounds when the game is starting
	protected override void playBonusStartSounds()
	{
		Audio.playMusic("ElviraPickACoffinBG");
		Audio.play("ELPickCoffinVO");
	}
	
	/// Opens a single casket.
	protected override IEnumerator revealButtonObject(int index, BonusPoolItem poolItem, bool selected = true)
	{
		if (indicesRemaining.IndexOf(index) == -1)
		{
			Debug.LogError("Trying to open the same casket twice: " + index);
			yield break;
		}
		
		CasketElements casket = caskets[index];	// Shorthand.
		if (!selected)
		{
			casket.renderer.material.SetFloat("_Saturation", -1f);
		}
		
		indicesRemaining.Remove(index);
		
		if (poolItem != pick)
		{
			// This is a reveal, not the pick. So remove it from the reveals list.
			reveals.Remove(poolItem);
		}
		
		// Rotate the casket door open.
		float duration = 1f;
		iTween.RotateTo(casket.casketHinge.gameObject, iTween.Hash("y", 140, "islocal", true, "time", 1f, "easetype", iTween.EaseType.easeInOutQuad));
		
		yield return new WaitForSeconds(duration * .5f);
		
		if (poolItem == pick)
		{
			Audio.play("kill_fanfares");
			Audio.play("RevealWildElvira");
			if (poolItem.reevaluations != null)
			{
				Audio.play("ELWildVO", 1, 0, 1f);
			}
			else if (poolItem.multiplier > 1)
			{
				Audio.play("ELMultiplierVO", 1, 0, 1f);
			}
		}
		
		if (poolItem.reevaluations != null)
		{
			SymbolInfo info = SlotBaseGame.instance.findSymbolInfo(fromSymbol);
			if ((info == null) || (info.getTexture() == null))
			{
				Debug.LogError("Can't find image for elvira01 wild symbol: " + fromSymbol);
			}
			else
			{
				NGUIExt.applyUITexture(caskets[index].symbol, info.getTexture());
			}
			
			casket.symbol.gameObject.SetActive(true);
			casket.wild.gameObject.SetActive(true);
			if (!selected)
			{
				casket.wild.color = Color.gray;
				casket.symbol.color = Color.gray;
			}
		}
		else if (poolItem.multiplier > 1)
		{
			showMultiplier(index, string.Format("{0}x_m", poolItem.multiplier), selected);
		}
		else
		{
			Debug.LogWarning("poolItem doesn't have anything useful.", gameObject);
		}

		// Squeeze the sizer after possibly attaching the reel symbol to it, so the reel symbol squeezes too.
		CommonTransform.setWidth(casket.sizer, .01f);

		// Start revealing the contents of the casket by un-squeezing it.
		iTween.ScaleTo(casket.sizer.gameObject, iTween.Hash("x", 1f, "time", duration * .5f));
		
		yield return new WaitForSeconds(1f + duration * .5f);
	}
		
	/// Shows a multiplier-type pool item.
	protected override void showMultiplier(int index, string spriteName, bool selected = true)
	{
		caskets[index].ghost.gameObject.SetActive(true);
		caskets[index].multiplier.gameObject.SetActive(true);
		caskets[index].multiplier.spriteName = spriteName;
		caskets[index].multiplier.MakePixelPerfect();

		if (!selected)
		{
			caskets[index].ghost.color = Color.gray;
			caskets[index].multiplier.color = Color.gray;
		}
	}
}
