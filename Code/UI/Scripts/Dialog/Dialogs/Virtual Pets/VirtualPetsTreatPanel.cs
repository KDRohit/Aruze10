using UnityEngine;
using Com.HitItRich.Feature.VirtualPets;

public class VirtualPetsTreatPanel : TICoroutineMonoBehaviour
{
    [SerializeField] private LabelWrapperComponent headerLabel;
    [SerializeField] private LabelWrapperComponent descriptionLabel;
    [SerializeField] private ObjectSwapper completionStateSwapper;
    [SerializeField] private AnimationListController.AnimationInformationList panelClickedAnimationList;
    [SerializeField] private ClickHandler panelButton;

    private VirtualPet playerPet;
    
    private const string COLLECTED_TREAT_TEXT = "Next Available at {0}";

    private const string INPROGRESS_STATE = "inprogress";
    private const string COMPLETE_STATE = "complete";

    public void init(CampaignDirector.FeatureTask task, VirtualPet pet)
    {
        playerPet = pet;
        headerLabel.text = Localize.text(task.type + "_pet_treat_header");
        System.DateTime taskEndingTime = Common.convertTimestampToDatetime(task.expirationTime);
        descriptionLabel.text = task.isComplete ? string.Format(COLLECTED_TREAT_TEXT, CommonText.formatTime(taskEndingTime.ToLocalTime())) : Localize.text(task.type + "_pet_treat_desc");
        completionStateSwapper.setState(task.isComplete ? COMPLETE_STATE : INPROGRESS_STATE);
        panelButton.registerEventDelegate(treatPanelClicked, Dict.create(D.TYPE, task.type));
    }
    
    private void treatPanelClicked(Dict args = null)
    {
        if (!playerPet.isPlayingReaction)
        {
            string treatType = (string) args.getWithDefault(D.TYPE, "");
            StatsManager.Instance.LogCount("dialog", "pet", "treats", treatType, VirtualPetsFeature.instance.getNumCompletedTasks().ToString(), "click", VirtualPetsFeature.instance.currentEnergy);
            StartCoroutine(AnimationListController.playListOfAnimationInformation(panelClickedAnimationList));
            StartCoroutine(playerPet.playTreatPanelClickedAnimation());
        }
    }
}
