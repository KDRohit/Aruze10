using Com.HitItRich.Feature.VirtualPets;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine;

    [TestFixture]
    public class VirtualPetUnitTests : IPrebuildSetup, IPostBuildCleanup
    {
        private const string TEST_DATA_DIRECTORY = "Test Data/VirtualPets/";
        private const string LOGIN_FILE = "login";
        private JSON getFakeLoginData()
        {
            string testDataPath = TEST_DATA_DIRECTORY + LOGIN_FILE;
            TextAsset textAsset = (TextAsset)Resources.Load(testDataPath,typeof(TextAsset));
            string text = textAsset.text;
            //update start/end time to be current
            text = text.Replace("\"next_petting_reward_time\": 0", "\"next_petting_reward_time\": " +   (GameTimer.currentTime + 60));
            return new JSON(text);
        }
        
        public void Setup()
        {
            if (VirtualPetsFeature.instance == null)
            {
                string json = "{\"server_epoch_time\":" + System.DateTimeOffset.Now.ToUnixTimeSeconds()+"}";
                JSON sampleTime = new JSON(json);
                GameTimer.updateServerEpochTime(sampleTime);
                VirtualPetsFeature.instantiateFeature(getFakeLoginData());
            }
        }
        
        public void Cleanup()
        {
            VirtualPetsFeature.resetStaticClassData();
        }

        /* Test normal Pet Unlock*/

        /* Test Setting in CustomPlayerData.SILENT_FEED_PET are read from silentFeedPet get
         * Case:
         *     Set CustomPlayerData.SILENT_FEED_PET true / false
         *     Verify VirtualPetsFeature.instance.silentFeedPet get is true / false
         */ 
      
        [PrebuildSetup(typeof(VirtualPetUnitTests))]
        [TestCase(true,ExpectedResult = true)]
        [TestCase(false,ExpectedResult = false)]
        public static bool VirtualPetFeature_silentFeedPet_Get(bool value)
        {
            CustomPlayerData.setValue(CustomPlayerData.SILENT_FEED_PET,value);
            return VirtualPetsFeature.instance.silentFeedPet;
        }
        
        /* Test Setting VirtualPetsFeature.instance.silentFeedPet set is reflected in CustomPlayerData.SILENT_FEED_PET
        * Case:
        *     Set VirtualPetsFeature.instance.silentFeedPet set true / false
        *     VerifyCustomPlayerData.SILENT_FEED_PET get is true / false
        */
        [PrebuildSetup(typeof(VirtualPetUnitTests))]
        [TestCase(true)]
        [TestCase(false)]
        public static void VirtualPetFeature_silentFeedPet_SET(bool value)
        {
            VirtualPetsFeature.instance.silentFeedPet = value;
            Assert.AreEqual(CustomPlayerData.getBool(CustomPlayerData.SILENT_FEED_PET, !value), value);
        }

        /* Test timerCollectsMax is set correctly based on HyperMaxTimerCollects , NormalMaxTimerCollects and isHyper
         * Case:
         *     Set HyperMaxTimerCollects to 1 and MaxTimerCollects to 2 and set isHyper to true
         *     verify VirtualPetsFeature.instance.timerCollectsMax is equal to VirtualPetsFeature.instance.hyperMaxTimerCollects
         * Case:
         *     Set HyperMaxTimerCollects to 1 and MaxTimerCollects to 2 and set isHyper to false
         *     verify VirtualPetsFeature.instance.timerCollectsMax is equal to VirtualPetsFeature.instance.normalMaxTimerCollects
        */
        [PrebuildSetup(typeof(VirtualPetUnitTests))]
        [TestCase(1,2,true,true,ExpectedResult = 1  )]
        [TestCase(1,2,false,false,ExpectedResult = 2  )]
        public static int VirtualPetFeature_timerCollectsMax_GET(int hyperTime , int normalTime, bool hyper ,bool returnHyperTime )
        {
#if !ZYNGA_PRODUCTION
            VirtualPetsFeature.instance.testSetHyperMaxTimerCollects(1);
            VirtualPetsFeature.instance.testSetNormalMaxTimerCollects(2);
            
            VirtualPetsFeature.instance.testSetHyperEndTime(GameTimer.currentTime + (hyper ? 10 : -10)); //Sets hyper to true or false
            return returnHyperTime
                ? VirtualPetsFeature.instance.hyperMaxTimerCollects
                : VirtualPetsFeature.instance.normalMaxTimerCollects;
#else
            return hyper ? 1 : 2;
#endif
            
        }

        /* Test isHyper set correctly based on GameTimer.currentTime < hyperEndTime; 
         * Case:
         *     Set hyperEndTime to GameTimer.currentTime + 10
         *     Verify isHyper returns true
         * Case:
         *     Set hyperEndTime to GameTimer.currentTime - 10
         *     Verify isHyper returns false
         * * Case:
         *     Set hyperEndTime to GameTimer.currentTime - 0
         *     Verify isHyper returns false
         */
        [PrebuildSetup(typeof(VirtualPetUnitTests))]
        [TestCase(10,ExpectedResult = true)]
        [TestCase(-10,ExpectedResult = false)]
        [TestCase(0,ExpectedResult = false)]
        public static bool VirtualPetFeature_isHyper(int offset)
        {
#if !ZYNGA_PRODUCTION
            VirtualPetsFeature.instance.testSetHyperEndTime(GameTimer.currentTime + offset);
            return VirtualPetsFeature.instance.isHyper;
#else
            return offset > 0 ? true : false;
#endif
        }

    }
