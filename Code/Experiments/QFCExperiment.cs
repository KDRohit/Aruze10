namespace QuestForTheChest
{
	public class QFCExperiment : EosExperiment
	{
		public string economyBucket { get; private set; }
		public string theme { get; private set; }
		
		public string videoUrl { get; private set; }
		public string videoSummaryPath { get; private set; }
		public string toasterTimeTrigger { get; private set; }
		public string toasterKeyTrigger { get; private set; }

		public QFCExperiment(string name) : base(name)
		{
		}

		protected override void init(JSON data)
		{
			theme = getEosVarWithDefault(data, "event_theme", "");
			economyBucket = getEosVarWithDefault(data, "event_economy_bucket", "");
			videoUrl = getEosVarWithDefault(data, "video_url", "");
			videoSummaryPath = getEosVarWithDefault(data, "video_summary_path", "");
			toasterTimeTrigger = getEosVarWithDefault(data, "toaster_time_triggers_hours", "");
			toasterKeyTrigger = getEosVarWithDefault(data, "toaster_keys_triggers", "");
		}

		public override void reset()
		{
			base.reset();
			theme = "";
			economyBucket = "";
			videoUrl = "";
			videoSummaryPath = "";
			toasterTimeTrigger = "";
			toasterKeyTrigger = "";
		}
	}
}
