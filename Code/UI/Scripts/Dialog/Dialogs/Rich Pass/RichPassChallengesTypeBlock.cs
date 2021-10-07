using UnityEngine;
using TMPro;

public class RichPassChallengesTypeBlock : RobustChallengesTypeBlock
{
    [SerializeField] private ObjectSwapper typeBlockSwapper;
    [SerializeField] private ObjectSwapper materialSwapper;
    [SerializeField] private ObjectSwapper viewSourceSwapper;
    [SerializeField] private LabelWrapperComponent richPassPointsLabel;
    [SerializeField] private LabelWrapperComponent unlockTimeLabel;
    [SerializeField] private GameObject completeContent;
    public GameObject newBadge;
    public UITexture gameUITexture;
    
    public Objective displayObjective { get; private set; }

    private const string ACTIVE = "_active";
    private const string INACTIVE = "_inactive";

    private const string DATE_BUBBLE_ON = "message_bubble_on";
    private const string DATE_BUBBLE_OFF = "message_bubble_off";

    private const string UNLOCKS_ON_LOCALIZATION = "unlocks_on_{0}";

    public override void init(Objective objective, bool isFinalCompletedObjective)
    {
        displayObjective = objective;
        base.init(objective, isFinalCompletedObjective);
        typeBlockSwapper.setState(CampaignDirector.richPass.passType + ACTIVE);
        setBlockState(CampaignDirector.richPass.passType, true);
        if (objective.usesTwoPartLocalization())
        {
            objectiveHeaderLabel.text = objective.getChallengeTypeActionHeader();    
        }
        else
        {
            bool abbr = objective.type == XDoneYTimesObjective.WIN_X_COINS_Y_TIMES;
            objectiveHeaderLabel.text = objective.getDynamicChallengeDescription(abbr);
        }
        
        for (int i = 0; i < objective.rewards.Count; i++)
        {
            long rewardAmount = objective.getRewardAmount(ChallengeReward.RewardType.PASS_POINTS);
            richPassPointsLabel.text = CommonText.formatNumber(rewardAmount);
        }
        completeContent.SetActive(objective.isComplete);
    }

    public void setBlockState(string tier, bool isActive)
    {
        typeBlockSwapper.setState(tier +  (isActive ? ACTIVE : INACTIVE));
    }

    public void setMasks(bool isMasked)
    {
        materialSwapper.setState(isMasked ? "masked" : "default");
    }

    public void setViewState(string source)
    {
        viewSourceSwapper.setState(source);
    }

    public void initLocked(System.DateTime unlockTime, bool showDate)
    {
        setBlockState(CampaignDirector.richPass.passType, false);
        typeBlockSwapper.setState(showDate ? DATE_BUBBLE_ON : DATE_BUBBLE_OFF);
        if (showDate)
        {
            unlockTimeLabel.text = Localize.text(UNLOCKS_ON_LOCALIZATION, unlockTime.ToShortDateString());
        }
    }
    
    protected override void logClick()
    {
        //TODO: Log me
    }
}
