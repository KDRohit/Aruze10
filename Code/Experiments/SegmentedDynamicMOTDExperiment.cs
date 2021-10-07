using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SegmentedDynamicMOTDExperiment : EosExperiment 
{

	public bool isValidForPlatform { get; private set; }
	public string keyName { get; private set; } 
	public int sortIndex { get; private set; }
	public string appearance { get; private set; }
	public string locTitle { get; private set; }
	public string locBodyText { get; private set; }
	public string imageBackground { get; private set; }
	public string locAction1 { get; private set; }
	public string commandAction1 { get; private set; }
	public string locAction2 { get; private set; }
	public string commandAction2 { get; private set; }
	public bool shouldShowAppEntry { get; private set; }
	public bool shouldShowVip { get; private set; }
	public bool shouldShowRTL { get; private set; }
	public int maxViews { get; private set; }
	public string statName { get; private set; }
	public int cooldown { get; private set; }

	public int uniqueId { get; private set;}

	public string audioPackKey { get; private set;}
	public string soundClose { get; private set;}
	public string soundOpen { get; private set;}
	public string soundOk { get; private set;}
	public string soundMusic { get; private set;}

	public SegmentedDynamicMOTDExperiment(string name) : base(name)
	{

	}

	public override bool isInExperiment
	{
		get
		{
			return (base.isInExperiment && isValidForPlatform);
		}
	}

	protected override void init(JSON data)
	{
		keyName = getEosVarWithDefault(data, "key_name", "");
		sortIndex = getEosVarWithDefault(data, "sort_index", 99999);
		appearance = getEosVarWithDefault(data, "appearance", "");
		locTitle = getEosVarWithDefault(data, "loc_key_title", "");
		locBodyText = getEosVarWithDefault(data, "loc_key_body_text", "");
		imageBackground = getEosVarWithDefault(data, "background_image", "");
		locAction1 = getEosVarWithDefault(data, "loc_key_action_1_text", "");
		commandAction1 = getEosVarWithDefault(data, "loc_key_action_1_string", "");
		locAction2 = getEosVarWithDefault(data, "loc_key_action_2_text", "");
		commandAction2 = getEosVarWithDefault(data, "loc_key_action_2_string", "");
		shouldShowAppEntry = getEosVarWithDefault(data, "show_location_entry", false);
		shouldShowVip = getEosVarWithDefault(data, "show_location_vip", false);
		shouldShowRTL = getEosVarWithDefault(data, "show_location_rtl", false);
		maxViews = getEosVarWithDefault(data, "max_views", 0);
		statName= getEosVarWithDefault(data, "stat_name", "dynamic_motd");
		cooldown = getEosVarWithDefault(data, "show_cooldown", 0) * Common.SECONDS_PER_HOUR;

		audioPackKey = getEosVarWithDefault(data, "audio_pack", "");
		soundClose = getEosVarWithDefault(data, "sound_close", "");
		soundOpen = getEosVarWithDefault(data, "sound_open", "");
		soundOk = getEosVarWithDefault(data, "sound_ok", "");
		soundMusic = getEosVarWithDefault(data, "sound_music", "");

		uniqueId = getHashCode();
#if UNITY_IPHONE
		isValidForPlatform = getEosVarWithDefault(data, "show_device_ios", false);
#elif ZYNGA_KINDLE
		isValidForPlatform = getEosVarWithDefault(data, "show_device_kindle", false);
#elif UNITY_ANDROID
		isValidForPlatform = getEosVarWithDefault(data, "show_device_android", false);
#elif UNITY_WEBGL
		isValidForPlatform = getEosVarWithDefault(data, "show_device_unityweb", false);
#elif UNITY_WSA_10_0 && NETFX_CORE //SMP this may need to be WSA specific
		isValidForPlatform = getEosVarWithDefault(data, "show_device_windows", false);
#else
		isValidForPlatform = false;
#endif
	}

	public override void reset()
	{
		base.reset();
		keyName = "";
		sortIndex = 99999;
		appearance = "";
		locTitle = "";
		locBodyText = "";
		imageBackground = "";
		locAction1 = "";
		commandAction1 = "";
		locAction2 = "";
		commandAction2 = "";
		shouldShowAppEntry = false;
		shouldShowVip = false;
		shouldShowRTL = false;
		maxViews = 0;
		statName = "dynamic_motd";
		cooldown =  0;
		uniqueId = -1;
		isValidForPlatform = false;

	}
}
