using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/**
Used to help setup data for the specific QFC themes that just need to be loaded once and won't change
**/
namespace QuestForTheChest
{
	public class QFCThemedStaticData : MonoBehaviour
	{
		public Transform[] nodeLocations;
		public Color32 rewardShroudColor;
	}
}
