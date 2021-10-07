using Com.HitItRich.Feature.VirtualPets;
using UnityEngine;

public class VirtualPetsTrickPanel : MonoBehaviour
{
    [SerializeField] private LabelWrapperComponent headerLabel;
    [SerializeField] private LabelWrapperComponent descriptionLabel;
    [SerializeField] private ObjectSwapper activeStateSwapper;
    [SerializeField] private GradientTintController panelTintController;
    [SerializeField] private AdjustObjectColorsByFactor lockedDimmer;
    [SerializeField] private GameObject lockedObject;

    public ClickHandler panelButton;

    private VirtualPetsDialogTabTricks.TRICK_TYPE treatType;

    private const string INACTIVE_STATE = "inactive";
    private const string ACTIVE_STATE = "active";

    private const string HEADER_LOC = "virtual_pet_trick_{0}_header";
    private const string LOCKED_DESCRIPTION_LOC = "virtual_pet_trick_{0}_desc_locked";
    private const string ACTIVE_DESCRIPTION_LOC = "virtual_pet_trick_{0}_desc_active";
    private const string COOLDOWN_DESCRIPTION_LOC = "virtual_pet_trick_{0}_desc_cooldown";


    public void init(VirtualPetsDialogTabTricks.TRICK_TYPE type)
    {
        treatType = type;
        switch (treatType)
        {
            case VirtualPetsDialogTabTricks.TRICK_TYPE.ROLLOVER_RESPIN:
                setupRespinPanel();
                break;
            case VirtualPetsDialogTabTricks.TRICK_TYPE.FETCH_DB:
                setupFreeBonusPanel();
                break;
        }
    }

    private void setupRespinPanel()
    {
        headerLabel.text = Localize.text(string.Format(HEADER_LOC, treatType));
        descriptionLabel.text = Localize.text(string.Format(ACTIVE_DESCRIPTION_LOC, treatType) + (VirtualPetsFeature.instance.isHyper ? "_hyper" : ""));
        
        activeStateSwapper.setState(ACTIVE_STATE);
        panelTintController.updateColor((float)VirtualPetsFeature.instance.currentEnergy/(float)VirtualPetsFeature.instance.maxEnergy, true);
    }

    private void setupFreeBonusPanel()
    {
        headerLabel.text = Localize.text(string.Format(HEADER_LOC, treatType));
        if (VirtualPetsFeature.instance.timerCollectsUsed >= VirtualPetsFeature.instance.hyperMaxTimerCollects)
        {
            //In cooldown
            descriptionLabel.text = Localize.text(string.Format(COOLDOWN_DESCRIPTION_LOC, treatType));
            activeStateSwapper.setState(INACTIVE_STATE);
        }
        else if (VirtualPetsFeature.instance.hyperReached)
        {
            //Unlocked and can fetch
            descriptionLabel.text = Localize.text(string.Format(ACTIVE_DESCRIPTION_LOC, treatType));
            activeStateSwapper.setState(ACTIVE_STATE);
            panelTintController.updateColor((float)VirtualPetsFeature.instance.currentEnergy/(float)VirtualPetsFeature.instance.maxEnergy, true);
        }
        else
        {
            //Locked
            descriptionLabel.text = Localize.text(string.Format(LOCKED_DESCRIPTION_LOC, treatType));
            activeStateSwapper.setState(ACTIVE_STATE);
            panelTintController.updateColor((float)VirtualPetsFeature.instance.currentEnergy/(float)VirtualPetsFeature.instance.maxEnergy, true);
            lockedDimmer.multiplyColors();
            lockedObject.SetActive(true);
        }
    }
}
