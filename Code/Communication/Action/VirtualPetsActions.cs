using System.Collections.Generic;
using UnityEngine;

namespace Com.HitItRich.Feature.VirtualPets
{
    public class VirtualPetsActions : ServerAction
    {
        //Actions
        private const string SET_PET_NAME = "virtual_pet_set_name";
        private const string TAP_PET = "virtual_pet_get_petting_reward";
        private const string REFRSH_PET_STATUS = "virtual_pet_refresh";
        private const string VIRTUAL_PET_FTUE_SEEN = "virtual_pet_ftue_seen";

        //Dev Actions
        private const string DEV_INITIALIZE = "virtual_pet_initialize_dev";
        private const string DEV_SET_ENERGY = "virtual_pet_set_energy_dev";
        private const string DEV_SET_FTUE_SEEN = "virtual_pet_set_ftue_seen_dev";
        private const string DEV_RESET_TIMER_COLLECT_PERK = "virtual_pet_set_fetch_uses_dev";
        private const string DEV_SET_HYPER_END_TIME = "virtual_pet_set_hyper_end_ts_dev";
        private const string DEV_SET_FULLY_FED_TIME = "virtual_pet_set_streak_ts_dev";
        private const string DEV_SET_FED_STREAK_COUNT = "virtual_pet_set_streak_count_dev";
        private const string DEV_SET_PETTING_REWARD_TIME = "virtual_pet_set_petting_ts_dev";
        private const string DEV_SET_TASK_COMPLETE = "virtual_pet_complete_task";
        private const string DEV_RESET_TASKS = "virtual_pet_reset_task";
        private const string DEV_GRANT_SPECIAL_TREAT = "virtual_pet_feed_special_treat_dev";
        
        /** Constructor */
        private VirtualPetsActions(ActionPriority priority, string type) : base(priority, type) { }
        
        //Property Names
        private const string PET_NAME = "name";
        private const string TIME = "timestamp";
        private const string STATUS = "status";
        private const string AMOUNT = "count";
        private const string TASK_ID = "task_id";
        private const string EVENT_ID = "event";
        private const string ENERGY = "amount";

        //Action Variables
        private bool status = false;
        private int time = 0;
        private int amount = 0;
        private string petName = "";
        private string taskId = "";
        private string eventId = "";
        private int energy = 0;

        public static Dictionary<string, string[]> propertiesLookup
        {
            get
            {
                if (_propertiesLookup == null)
                {
                    _propertiesLookup = new Dictionary<string, string[]>();
                    _propertiesLookup.Add(SET_PET_NAME, new string[] {PET_NAME});
                    _propertiesLookup.Add(TAP_PET, null);
                    _propertiesLookup.Add(REFRSH_PET_STATUS, null);
                    _propertiesLookup.Add(VIRTUAL_PET_FTUE_SEEN, new string[] { EVENT_ID });

#if !ZYNGA_PRODUCTION
                    _propertiesLookup.Add(DEV_INITIALIZE, null);
                    _propertiesLookup.Add(DEV_SET_ENERGY, new string[] { ENERGY });
                    _propertiesLookup.Add(DEV_RESET_TIMER_COLLECT_PERK, new string[] { AMOUNT });
                    _propertiesLookup.Add(DEV_SET_FTUE_SEEN, new string[] { STATUS });
                    _propertiesLookup.Add(DEV_SET_HYPER_END_TIME, new string[] { TIME });
                    _propertiesLookup.Add(DEV_SET_FULLY_FED_TIME, new string[] { TIME });
                    _propertiesLookup.Add(DEV_SET_FED_STREAK_COUNT, new string[] { AMOUNT });
                    _propertiesLookup.Add(DEV_SET_PETTING_REWARD_TIME, new string[] { TIME });
                    _propertiesLookup.Add(DEV_SET_TASK_COMPLETE, new string[] {TASK_ID});
                    _propertiesLookup.Add(DEV_RESET_TASKS, null);
                    _propertiesLookup.Add(DEV_GRANT_SPECIAL_TREAT, null);
#endif
                }
                return _propertiesLookup;
            }
        }
		
        private static Dictionary<string, string[]> _propertiesLookup = null;

        private static void baseAction(string actionKey)
        {
            VirtualPetsActions action = new VirtualPetsActions(ActionPriority.IMMEDIATE, actionKey);
            processPendingActions();
        }
        
        /*
         * Action to set pet's name in server data
         */
        public static void setPetName(string newName)
        {
            VirtualPetsActions action = new VirtualPetsActions(ActionPriority.HIGH, SET_PET_NAME);
            action.petName = newName;
            processPendingActions();
        }

        /*
         * Action sent when player taps on the pet in the feature dialog and is eligible for the petting reward
         * Rewardable event should be sent back with coin or energy grant
         */
        public static void tapPet()
        {
            baseAction(TAP_PET);
        }
        
        /*
         * Request for up-to-date pets data
         */
        public static void refreshPetStatus()
        {
            baseAction(REFRSH_PET_STATUS);
        }
        
        /*
         * Some parts of the feature are locked to the player until they've triggered the respin feature once and seen that FTUE flow
         * Sending this action after the FTUE is seen to unlock the rest of the feature
         */
        public static void markFtueSeen(string eventId)
        {
            VirtualPetsActions action = new VirtualPetsActions(ActionPriority.IMMEDIATE, VIRTUAL_PET_FTUE_SEEN);
            action.eventId = eventId;
            processPendingActions();
        }
        
#if !ZYNGA_PRODUCTION
        //Dev Event Handlers
        public static void devInitialize()
        {
            baseAction(DEV_INITIALIZE);
        }
        
        public static void devSetEnergy(int newEnergy)
        {
            VirtualPetsActions action = new VirtualPetsActions(ActionPriority.IMMEDIATE, DEV_SET_ENERGY);
            action.energy = newEnergy;
            processPendingActions();
        }

        public static void devSetFtueSeen(bool seen)
        {
            VirtualPetsActions action = new VirtualPetsActions(ActionPriority.IMMEDIATE, DEV_SET_FTUE_SEEN);
            action.status = seen;
            processPendingActions();
        }

        public static void devResetTimerCollectPerk()
        {
            VirtualPetsActions action = new VirtualPetsActions(ActionPriority.IMMEDIATE, DEV_RESET_TIMER_COLLECT_PERK);
            action.amount = 0;
            processPendingActions();
        }

        public static void devSetTaskComplete(string taskId)
        {
            VirtualPetsActions action = new VirtualPetsActions(ActionPriority.IMMEDIATE, DEV_SET_TASK_COMPLETE);
            action.taskId = taskId;
            processPendingActions();
        }

        public static void devResetTasks()
        {
            VirtualPetsActions action = new VirtualPetsActions(ActionPriority.IMMEDIATE, DEV_RESET_TASKS);
            processPendingActions();
        }

        public static void devSetHyperEndTime(int timestamp)
        {
            VirtualPetsActions action = new VirtualPetsActions(ActionPriority.IMMEDIATE, DEV_SET_HYPER_END_TIME);
            action.time = timestamp;
            processPendingActions();
        }

        public static void devSetFullyFedTimestamp(int timestamp)
        {
            VirtualPetsActions action = new VirtualPetsActions(ActionPriority.IMMEDIATE, DEV_SET_FULLY_FED_TIME);
            action.time = timestamp;
            processPendingActions();
        }

        public static void devSetFullyFedStreak(int streakValue)
        {
            VirtualPetsActions action = new VirtualPetsActions(ActionPriority.IMMEDIATE, DEV_SET_FED_STREAK_COUNT);
            action.amount = streakValue;
            processPendingActions();
        }
        
        public static void devSetPettingRewardTime(int timestamp)
        {
            VirtualPetsActions action = new VirtualPetsActions(ActionPriority.IMMEDIATE, DEV_SET_PETTING_REWARD_TIME);
            action.time = timestamp;
            processPendingActions();
        }
        
        public static void devAddSpecialTreat()
        {
            baseAction(DEV_GRANT_SPECIAL_TREAT);
        }
#endif



        new public static void resetStaticClassData()
        {
            _propertiesLookup = null;
        }

        public override void appendSpecificJSON(System.Text.StringBuilder builder)
        {
            if (!propertiesLookup.ContainsKey(type))
            {
                Debug.LogError("No properties defined for action: " + type);
                return;
            }

            //skip if we have no arguments
            if (propertiesLookup[type] == null)
            {
                return;
            }

            foreach (string property in propertiesLookup[type])
            {
                
                switch (property)
                {
                    case PET_NAME:
                        appendPropertyJSON(builder, property, petName);
                        break;
                    case TIME:
                        appendPropertyJSON(builder, property, time);
                        break;
                    case AMOUNT:
                        appendPropertyJSON(builder, property, amount);
                        break;
                    case TASK_ID:
                        appendPropertyJSON(builder, property, taskId);
                        break;
                    case EVENT_ID:
                        appendPropertyJSON(builder, property, eventId);
                        break;
                    case STATUS:
                        appendPropertyJSON(builder, property, status);
                        break;
                    case ENERGY:
                        appendPropertyJSON(builder, property, energy);
                        break;
                    default:
                        Debug.LogWarning($"Unknown property for action={type} {property}");
                        break;
                }
            }
        }

    }
}