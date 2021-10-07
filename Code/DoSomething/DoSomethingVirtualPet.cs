
using Com.HitItRich.Feature.VirtualPets;

public class DoSomethingVirtualPet : DoSomethingAction
{
    public override void doAction(string parameter)
    {
        VirtualPetsFeatureDialog.showDialog();
    }
    
    public override bool getIsValidToSurface(string parameter)
    {
        bool canShow = false;
        if (VirtualPetsFeature.instance != null)
        {
            canShow = VirtualPetsFeature.instance.isEnabled && VirtualPetsFeature.instance.ftueSeen;
        }
        return canShow;
    }

}
