using Com.Scheduler;
using System.Collections;
using UnityEngine;

public class OutOfCoinsReboundDialog : DialogBase, IResetGame
{
    private const string BUTTON_LOC_STRING = "free_coins";
    private const string STAT_COUNTER = "dialog";
    private const string STAT_KINGDOM = "ooc_rebound";
    private const string STAT_PHYLUM = "special_ooc";
    private const string STAT_PHYLUM_CTA = "special_ooc_cta";
    private const string DIALOG_KEY = "ooc_rebound";
    
    
    [SerializeField] private ButtonHandler ctaButton;
    [SerializeField] private UITexture bgTexture;
    [SerializeField] private LabelWrapperComponent buttonLabel;
    [SerializeField] private Transform coinStartPos;
    
    public static OutOfCoinsReboundDialog instance { get; private set; }

    private Material clonedMaterial = null;
    public override void init()
    {

        instance = this;
        ctaButton.registerEventDelegate(onClickCTA);
        
        if (downloadedTextures != null && downloadedTextures.Length > 0)
        {
            //if we replace teh texture it will clone the material and we need to clean that up
            if (bgTexture.material.mainTexture == null)
            {
                bgTexture.material.mainTexture = downloadedTextures[0];
            }
            else if (bgTexture.material.mainTexture != null && bgTexture.material.mainTexture != downloadedTextures[0])
            {
                //we have a new texture this will cause a material clone that we need to clean up
                bgTexture.material.mainTexture = downloadedTextures[0];
                clonedMaterial = bgTexture.material;
            }
        }

        if (buttonLabel != null)
        {
            buttonLabel.text = Localize.text(BUTTON_LOC_STRING);
        }
        
        StatsManager.Instance.LogCount(counterName: STAT_COUNTER,
            kingdom: STAT_KINGDOM,
            phylum: STAT_PHYLUM,
            genus: "view");
    }
    // Called by Dialog.close() - do not call directly.	
    public override void close()
    {
        //clean up downloaded texture
        if (clonedMaterial != null)
        {
            Destroy(clonedMaterial);
        }

        instance = null;

        StatsManager.Instance.LogCount(counterName: STAT_COUNTER,
            kingdom: STAT_KINGDOM,
            phylum: STAT_PHYLUM,
            genus: "close");
    }

    private void onClickCTA(Dict args = null)
    {
        string actionType = ExperimentWrapper.SpecialOutOfCoins.experimentData.cta == "inbox" ? "inbox" : "coin";
        
        StatsManager.Instance.LogCount(counterName: STAT_COUNTER,
            kingdom: STAT_KINGDOM,
            phylum: STAT_PHYLUM_CTA,
            genus: actionType);


        if (ExperimentWrapper.SpecialOutOfCoins.experimentData.cta == "dollar_reward")
        {
            //action is handled by close handler, don't need to do anything other than close the dialog
            StartCoroutine(coinFly());    
        }
        else
        {
            Dialog.close(this);
        }
        
    }


    public static void showDialog(JSON outcomeJSON, AnswerDelegate clickAction)
    {
        if (instance != null)
        {
            Debug.LogWarning("Can't show dialog because it's already active");
            return;
        }
        
        Dict args = Dict.create(
            D.CALLBACK, clickAction,
            D.PRIORITY, SchedulerPriority.PriorityType.IMMEDIATE
        );

        Scheduler.addDialog(DIALOG_KEY, args);

    }
    
    private IEnumerator coinFly()
    {
        if (Overlay.instance.topV2 == null)
        {
            Dialog.close(this);
            yield break;
        }

        //prevent clicks
        closeButtonHandler.enabled = false;
        ctaButton.enabled = false;
		
		
        // Create the coin as a child of "sizer", at the position of "coinIconSmall",
        // with a local offset of (0, 0, -100) so it's in front of everything else with room to spin in 3D.
        CoinScriptUpdated coin = CoinScriptUpdated.create(
            sizer,
            coinStartPos.position,
            new Vector3(0, 0, -100)
        );

        Audio.play("initialbet0");
		
        Vector3 overlayCoinPosition = Overlay.instance.topV2.coinAnchor.position;
        //note that this is from a different camera, so we have to convert to screen from world position back to screen position
        //Find the ngui camera
        int layerMask = 1 << Overlay.instance.topV2.gameObject.layer;
        Camera nguiCamera = CommonGameObject.getCameraByBitMask(layerMask);

        //calculate the screen position based on viewing camera and world position
        Vector2int destination = NGUIExt.screenPositionOfWorld(nguiCamera, overlayCoinPosition);
		
        //convert from ngui to unity vector 2
        Vector2 endDestination = new Vector2(destination.x, destination.y);
		
        //fly, you're free!
        yield return StartCoroutine(coin.flyTo(endDestination));
		
        //destroy coin 
        coin.destroy();
        
        //close this dialog (credit add is handled by feature handler on dialog close callback)
        Dialog.close(this);
    }

    public static void resetStaticClassData()
    {
        instance = null;
    }
}
