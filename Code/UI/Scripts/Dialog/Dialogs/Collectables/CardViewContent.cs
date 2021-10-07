using UnityEngine;
using System.Collections;
using TMPro;

public class CardViewContent : MonoBehaviour
{
	public CollectableCardHighQuality cardOnDisplay;
	public GameObject earnedStateObjects;
	public GameObject unearnedStateObjects;
	public GameObject cardParent; // The static card that sits on this screen. Maybe have another class that holds all that info?
	public TextMeshProMasker masker;
}
