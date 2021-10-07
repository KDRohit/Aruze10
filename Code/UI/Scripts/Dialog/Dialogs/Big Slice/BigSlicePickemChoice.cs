using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Controls a pickem choice object in the big slice game.
*/

public class BigSlicePickemChoice : MonoBehaviour
{
	public Animator animator;
	// Picked versions
	public GameObject bigSlice;
	public TextMeshPro plus1X;
	public GameObject spin;
	public GameObject doubleBet;

	// Revealed versions
	public GameObject bigSliceRevealed;
	public TextMeshPro plus1XRevealed;
	public GameObject spinRevealed;
	public GameObject doubleBetRevealed;
	public GameObject doubleAllRevealed;
	public GameObject coin;

	public GameObject doubleAll;
	public GameObject plus1Odd;
	public GameObject plus1Even;

	public IEnumerator pick(JSON data)
	{
		// Pick data has "modifiers" as a JSON array instead of a string array.
		JSON[] modifiers = data.getJsonArray("modifiers");
		
		if (modifiers == null || modifiers.Length == 0)
		{
			if (data.getLong("multiplier", 0L) == 2L)
			{
				doubleBet.SetActive(true);
			}
			else
			{
				spin.SetActive(true);
			}
		}
		else
		{
			string text = "";
			switch (modifiers[0].getString("key_name", ""))
			{
				case "big_slice":
				case "big_slice_new_one":
					bigSlice.SetActive(true);
					break;
				case "plus_1_all":
					text = Localize.textUpper("plus_{0}_X", 1);
					break;
				case "double_up":
					text = Localize.textUpper("Double\nAll", 1);
					doubleAll.SetActive(true);
					break;		
				case "plus_1_even_new_one":
					text = Localize.textUpper("plus_{0}_X", 1);
					plus1Even.SetActive(true);
					break;
				case "plus_1_odd_new_one":
					text = Localize.textUpper("plus_{0}_X", 1);
					plus1Odd.SetActive(true);
					break;	
			}			
			plus1X.text = text;
			plus1X.gameObject.SetActive(true);		

		}
				
		animator.Play("Reveal");
		
		yield return null;	// Wait a frame for the animation to start playing before we can get the current animator state info.
		yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
	}
	
	public void reveal(JSON data)
	{
		// Reveal data has "modifiers" as a string array instead of a JSON array.
		string[] modifiers = data.getStringArray("modifiers");
		
		if (modifiers == null || modifiers.Length == 0)
		{
			if (data.getLong("multiplier", 0L) == 2L)
			{
				doubleBetRevealed.SetActive(true);
			}
			else
			{
				spinRevealed.SetActive(true);
			}
		}
		else
		{
			string text = "";
			switch (modifiers[0])
			{
				case "big_slice":
				case "big_slice_new_one":
					bigSliceRevealed.SetActive(true);
					break;
				case "plus_1_all":
					text = Localize.textUpper("plus_{0}_X", 1);
					break;
				case "double_up":
					text = Localize.textUpper("Double All", 1);
					doubleAllRevealed.SetActive(true);
					break;		
				case "plus_1_even_new_one":
					text = Localize.textUpper("plus_{0}_X", 1);
					break;
				case "plus_1_odd_new_one":
					text = Localize.textUpper("plus_{0}_X", 1);
					break;								
			}
			
			plus1XRevealed.text = text;
			plus1XRevealed.gameObject.SetActive(true);

		}

		animator.Play("Revealed");
	}
}
