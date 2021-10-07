

using System.Collections.Generic;
using UnityEngine;

public class RichPassExperiment : EosExperiment
{
    public string packageKey
    {
        get;
        private set;
    }
    
    public string videoUrl { get; private set; }
    public string videoSummaryPath { get; private set; }
    public int passValueAmount { get; private set; }
    public string dialogBgPath { get; private set; }

    private HashSet<string> hideInGamePanelKeys;

    public RichPassExperiment(string name) : base(name)
    {
    }

    protected override void init(JSON data)
    {
        packageKey = getEosVarWithDefault(data, "package", "");
        videoUrl = getEosVarWithDefault(data, "video_url", ""); 
        videoSummaryPath = getEosVarWithDefault(data, "summary_image_path", "");
        passValueAmount = getEosVarWithDefault(data, "pass_value_amount", 0);
        dialogBgPath = getEosVarWithDefault(data, "dialog_bg_path", "");

        hideInGamePanelKeys = new HashSet<string>();
        string items = getEosVarWithDefault(data, "hide_panel_in_games", "");
        if (!string.IsNullOrEmpty(items))
        {
            string[] allGames = items.Split(',');
            for (int i = 0; i < allGames.Length; i++)
            {
                if (!string.IsNullOrEmpty(allGames[i]))
                {
                    hideInGamePanelKeys.Add(allGames[i]);
                }
            }
                
        }
    }

    public bool shouldDisplayCounterinGame(string gameKey)
    {
        return hideInGamePanelKeys == null || !hideInGamePanelKeys.Contains(gameKey);
    }

    public override void reset()
    {
        packageKey = "";
        videoUrl = "";
        videoSummaryPath = "";
        passValueAmount = 0;
        hideInGamePanelKeys = null;
        dialogBgPath = "";
    }
}
