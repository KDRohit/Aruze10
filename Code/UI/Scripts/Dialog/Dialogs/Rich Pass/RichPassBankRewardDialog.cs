using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using UnityEngine;

public class RichPassBankRewardDialog : DialogBase
{
    [SerializeField] private RichPassPiggyBankInfoDialog piggyBank;
    public override void init()
    {
        long amount = (long)dialogArgs.getWithDefault(D.AMOUNT, 0);
        string eventId = (string)dialogArgs.getWithDefault(D.EVENT_ID, "");

        piggyBank.init(amount, eventId);
    }
    
    
    public override void close()
    {
        
    }

    public static void showDialog(long amount, string eventId)
    {
        Scheduler.addDialog("rich_pass_bank_reward_dialog", Dict.create(D.AMOUNT, amount, D.EVENT_ID, eventId));
    }
}
