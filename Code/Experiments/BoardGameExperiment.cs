using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardGameExperiment : OrchestratorFeatureExperiment
{
	public new const string experimentName = "boardgame";
	
	[Eos("theme", "casino")]
	public string theme { get; private set; }

	[Eos("sale_dialog_bg", "")]
	public string saleDialogBg { get; private set; }
	
	[Eos("enabled_start_time", -1)]
	public int startTime { get; private set; }
	
	[Eos("enabled_end_time", -1)]
	public int endTime { get; private set; }
	
	[Eos("video_url", "")]
	public string videoUrl { get; private set; }
	
	[Eos("video_summary_path", "")]
	public string videoSummeryPath { get; private set; }
	
	[Eos("package_1_collectible_pack", "")]
	public string packagePack1 { get; private set; }
	
	[Eos("package_2_collectible_pack", "")]
	public string packagePack2 { get; private set; }
	
	[Eos("package_3_collectible_pack", "")]
	public string packagePack3 { get; private set; }
	
	public BoardGameExperiment(string name) : base(name)
	{
	}
}
