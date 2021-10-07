//Child class of EosExperiment other experiments should inherit from if they're using video related variables
//Expecting the variables in EOS to be named:
//    video_url - URL for the streaming video
//    video_summary_path - path for the image from S3 that appears at the end of the video

public class EosVideoExperiment : EosExperiment
{
    public string videoUrl { get; private set; }
    public string videoSummaryPath { get; private set; }
    
    public EosVideoExperiment(string name) : base(name)
    {
    }
    
    protected override void init(JSON data)
    {
        base.init(data);
        if (data != null)
        {
            videoUrl = getEosVarWithDefault(data, "video_url", "");
            videoSummaryPath = getEosVarWithDefault(data, "video_summary_path", "");    
        }
    }
}
