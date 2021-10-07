using UnityEngine;
using System.Collections;
using TMPro;

/*
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
*/

public class DbsScoreBox : TICoroutineMonoBehaviour
{
	public TextMeshPro scoreLabel;
	public Animation goAnim;
	
	public GameObject effectAnchor;
	public GameObject effectPrefab;
	
	public Collider buttonCollider;

	public void setScore(string score)
	{
		scoreLabel.text = score;
		
		VisualEffectComponent.Create(effectPrefab, effectAnchor);
	}
}
