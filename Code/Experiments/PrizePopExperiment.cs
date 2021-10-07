using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PrizePop
{
	public class PrizePopExperiment : EosExperiment
	{
		public string theme { get; private set; }
		public int startPicks { get; private set; }
		public int startTime { get; private set; }
		public int endTime { get; private set; }
		public string packageKeys { get; private set; }
		public string videoUrl { get; private set; }
		public string videoSummaryPath { get; private set; }
		public int endingSoonTrigger { get; private set; }
		
		public PrizePopExperiment(string name) : base(name)
		{
		}

		protected override void init(JSON data)
		{
			theme = getEosVarWithDefault(data, "theme", "");
			startPicks = getEosVarWithDefault(data, "start_picks", 0);
			startTime = getEosVarWithDefault(data, "start_time", int.MaxValue);
			endTime = getEosVarWithDefault(data, "end_time", int.MaxValue);
			packageKeys = getEosVarWithDefault(data, "package_keys", "");
			videoUrl = getEosVarWithDefault(data, "video_url", ""); 
			videoSummaryPath = getEosVarWithDefault(data, "summary_image_path", "");
			endingSoonTrigger = getEosVarWithDefault(data, "ending_soon_trigger", 15);
		}

		public override void reset()
		{
			base.reset();
			theme = "";
			startPicks = 0;
			startTime = int.MaxValue;
			endTime = int.MaxValue;
			packageKeys = "";
		}
	}   
}
