using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LottoBlastProgressFX : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private AnimationListController.AnimationInformationList spinAnim;
    void Start()
    {
        if (XPUI.instance != null)
        {
            XPUI.instance.xpUpdated += playSpinFX;

        }
    }
    private void playSpinFX()
    {
        StartCoroutine(AnimationListController.playListOfAnimationInformation(spinAnim));
    }

    // Update is called once per frame
    void OnDestroy()
    {
        if (XPUI.instance != null)
        {
            XPUI.instance.xpUpdated -= playSpinFX;
        }
    }
}
