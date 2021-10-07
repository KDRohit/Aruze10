using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(PowerupCustomAttribute))]
public class PowerupSpawnerEditor : PropertyDrawer
{
	public static string[] POWER_UP_LIST = new string[]
	{
		PowerupBase.POWER_UP_BUY_PAGE_KEY,
		PowerupBase.POWER_UP_DAILY_BONUS_KEY,
		PowerupBase.POWER_UP_BIG_WINS_KEY,
		PowerupBase.POWER_UP_DOUBLE_MAX_VOLTAGE_KEY,
		PowerupBase.POWER_UP_DOUBLE_VIP_KEY,
		PowerupBase.POWER_UP_EVEN_LEVELS_KEY,
		PowerupBase.POWER_UP_ODD_LEVELS_KEY,
		PowerupBase.POWER_UP_FREE_SPINS_KEY,
		PowerupBase.POWER_UP_ROYAL_RUSH_KEY,
		PowerupBase.POWER_UP_TRIPLE_XP_KEY,
		PowerupBase.POWER_UP_VIP_BOOSTS_KEY,
		PowerupBase.POWER_UP_WEEKLY_RACE_KEY
	};
	
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		int index = Mathf.Max (0, Array.IndexOf(POWER_UP_LIST, property.stringValue));
		index = EditorGUI.Popup(position, property.displayName, index, POWER_UP_LIST);

		if (property.propertyType == SerializedPropertyType.String)
		{
			property.stringValue = POWER_UP_LIST[index];
		}
	}
}