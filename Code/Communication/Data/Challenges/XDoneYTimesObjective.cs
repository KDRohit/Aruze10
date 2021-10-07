
using System.Collections.Generic;
using System.Text;

public class XDoneYTimesObjective : Objective
{
    public const string WIN_X_COINS_Y_TIMES = "win_x_coins_y_times";
    
    private const string WIN_COUNT = "win_count";
    
    public int winCount { get; protected set; }
    
    public XDoneYTimesObjective(JSON data) : base(data)
    {
    }

    protected override void parseKey(JSON data, string key)
    {
        switch (key)
        {
            case WIN_COUNT:
                winCount = data.getInt(key, 0);
                break;
            
            default:
                base.parseKey(data, key);
                break;
        }
        
    }

    public override long progressBarMax
    {
        get
        {
            return winCount;
        }
    }
    public override float progressPercent
    {
        get
        {
            return isComplete ? 1.0f : currentAmount / (float)winCount;
			
        }
    }
    
    public override bool isComplete
    {
        get
        {
            return currentAmount >= winCount && currentAmount > 0;
        }
    }
    
    protected override bool usePercentageForProgress()
    {
        return false;
    }
    
    public override string getProgressText()
    {
        long amountToDisplay = currentAmount >= winCount ? winCount : currentAmount;
        return string.Format("{0}/{1}", amountToDisplay, winCount);
    }
    
    public override string getCompletedProgressText()
    {
        return string.Format("{0}/{1}", winCount, winCount);
    }
    
    protected override string getLocString(string prefix, bool includeCredits, bool inProgress = false)
    { 
        List<object> locItems = new List<object>();
        StringBuilder sb = new StringBuilder();
        sb.Append(prefix + type + "_count_{0}");
        locItems.Add(amountNeeded);
            
        if (minWager > 0)
        {
            sb.Append(Localize.DELIMITER + "min_wager_{1}");
            locItems.Add(CreditsEconomy.convertCredits(minWager));
        }

        long remainingAmount = winCount - currentAmount;
        
        if (remainingAmount > 1)
        {
            sb.Append(Localize.DELIMITER + "plural");
        }
        
        locItems.Add(winCount);
        locItems.Add(game);

        return Localize.text(sb.ToString(), locItems.ToArray());
    }
    
    protected override string getChallengeTypeActionHeaderDynamic(string prefix, bool abbreviateNumber)
    {
        string shortLocString = prefix + type + "_count_{0}";
        if (winCount > 1)
        {
            shortLocString += Localize.DELIMITER + "plural";
        }
        return Localize.text(shortLocString, CreditsEconomy.multiplyAndFormatNumberAbbreviated(amountNeeded), winCount);
        
    }

}
