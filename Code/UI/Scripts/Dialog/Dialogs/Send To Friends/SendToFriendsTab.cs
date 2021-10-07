using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/**
Basic script to define labels and data for each tab.
*/

public class SendToFriendsTab : MonoBehaviour
{
	public UIImageButton button;
	public TextMeshPro nameLabel;
	[HideInInspector] public List<SocialMember> members;
}
