using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class PaytableGlyph : TICoroutineMonoBehaviour
{
	public TextMeshPro[] keys;
	public TextMeshPro[] labels;
	public UITexture symbolSlot;
	[SerializeField] private GameObject containerToCenter = null;	// Parent game object of all the keys and labels, only set if you want this container to be centered

	public void init(PayTable table, string symbol)
	{
		// Hide everything (they will get shown again):
		for (int i = 0; i < this.keys.Length; i++)
		{
			this.keys[i].gameObject.SetActive(false);
			this.labels[i].gameObject.SetActive(false);
		}

		// Dynamically set values for the paytable display.
		// First collect them for this symbol:
		List<KeyValuePair<int, long>> sort = new List<KeyValuePair<int, long>>();

		long multiplier = GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers;
		
		// Make sure we turn the name into a server name, in case it is a variant name.  Since
		// those names are not used as entries in the paytable data.
		symbol = SlotSymbol.getServerNameFromName(symbol);

		foreach (PayTable.LineWin line in table.lineWins.Values)
		{
			if (line.symbol == symbol)
			{
				sort.Add(new KeyValuePair<int, long>(line.symbolMatchCount, line.credits * multiplier));
			}
		}
		sort.Sort(sortByKeyReverse);

		// ...now pick out the top values and add to the dialog:
		int walk = 0;

		foreach (KeyValuePair<int, long> kvp in sort)
		{
			this.labels[walk].text = CreditsEconomy.convertCredits(kvp.Value);
			this.labels[walk].gameObject.SetActive(true);			
			this.keys[walk].text = string.Format("{0}=", CommonText.formatNumber(kvp.Key));
			this.keys[walk].gameObject.SetActive(true);

			walk++;
			if (walk >= this.keys.Length)
			{
				break;
			}
		}

		if (containerToCenter != null)
		{
			// Adjust the labels to center them under the image
			Bounds containerBounds = NGUIMath.CalculateRelativeWidgetBounds(containerToCenter.transform);
			Vector3 currentPos = containerToCenter.transform.localPosition;
			containerToCenter.transform.localPosition = new Vector3(-containerBounds.center.x, currentPos.y, currentPos.z);
		}
	}

	private static int sortByKeyReverse(KeyValuePair<int, long> a, KeyValuePair<int, long> b)
	{
		return b.Key.CompareTo(a.Key);
	}

	public void hide()
	{
		this.gameObject.SetActive(false);
	}
}
