using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlashSaleDigitAnimator : MonoBehaviour
{
    public Animator animator;

    public void startAnimation()
    {
        animator.SetTrigger("StartAnimation");
    }
}
