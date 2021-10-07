using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PurchasePerksIconContainer : MonoBehaviour
{
    public Transform iconParent;
    public ObjectSwapper containerSwapper;

    public static class SwapperStates
    {
        public const string ATTACHED_WITHOUT_BACKING_STATE = "attached_without_backing";
        public const string ATTACHED_WITH_BACKING_STATE = "attached_with_backing";
        public const string DETACHED_WITHOUT_BACKING_STATE = "detached_without_backing";
        public const string DETACHED_WITH_BACKING_STATE = "detached_with_backing";
    }
}
