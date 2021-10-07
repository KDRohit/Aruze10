using UnityEngine;
using System.Collections;
using TMPro;

/**
A place to store everything that is used in the DTWScoreBox.
**/

public class DtwScoreBox : TICoroutineMonoBehaviour
{
	public GameObject nameBox;
	public TextMeshPro nameLabel;

	public TextMeshPro score;

	void Start()
	{
		score.text = "";
	}
}
