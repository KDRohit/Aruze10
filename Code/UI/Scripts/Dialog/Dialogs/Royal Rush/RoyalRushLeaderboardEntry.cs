﻿using System;
using System.Collections;
using UnityEngine;
using TMPro;

public class RoyalRushLeaderboardEntry : MonoBehaviour
{
	public const string BASIC_BAR_ASSET_PATH = "";
	public const string PLAYER_RANK_ANIMATION = "Player Ranking Load";
	public const string RULER_ANIMATION = "Ruler Bar Load";

	public TextMeshPro score;
	public TextMeshPro rank;
	public FacebookFriendInfo user;
	public bool isUser = false;
	public Animator popInAnimator;

	private void Start()
	{

	}
}

