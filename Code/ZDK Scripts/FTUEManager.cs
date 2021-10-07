using UnityEngine;
using System;
using TMPro;

public class FTUEManager
{
    //FTUE prefab
    public const string FTUE_PREFAB = "";
    
    //Different steps of the ftue stored in the custome player data or the player pref
	public const int ACHIEVEMENT_FTUE_STEP_0 = 0;
    public const int ACHIEVEMENT_FTUE_STEP_1 = 1;
	public const int ACHIEVEMENT_FTUE_STEP_2 = 2;
	public const int ACHIEVEMENT_FTUE_STEP_3 = 3;
	public const int ACHIEVEMENT_FTUE_STEP_4 = 4;
  
    //Instance of the ftuemanager
    protected static FTUEManager _ftueManager = null;
    protected static GameObject _attachGameObject = null;
    
	public static GameObject _go = null;

	//Constant for timing
	public const float TIMING_CONSTANT = 3.0f;

    //Singleton for FTUEManager
    public static FTUEManager Instance
    {
        get
        {
            if (_ftueManager == null)
            {
                _ftueManager = new FTUEManager();
            }
            return _ftueManager;
        }
    }

	public GameObject Go
	{
		get
		{
			return _go;
		}
	}
		
	/// <summary>
    /// Load Ftue Prefab
    /// </summary>
    /// <param name="prefab"></param>
    public void LoadFtuePrefab(string prefab, GameObject attachGameObject=null)
    {
        _attachGameObject = attachGameObject;
        prefab = string.Format(prefab);
        AssetBundleManager.load(this, prefab, CustomFtueLoadSuccess, CustomFtueLoadFailure);
    }

    /// <summary>
    /// Call back from loading the ftue prefab
    /// </summary>
    /// <param name="assetPath"></param>
    /// <param name="obj"></param>
    /// <param name="data"></param>
    private static void CustomFtueLoadSuccess(string assetPath, UnityEngine.Object obj, Dict data = null)
    {
        GameObject prefab = obj as GameObject;//CommonGameObject.instantiate(obj) as GameObject;
		prefab.SetActive (true);
        _go = NGUITools.AddChild(_attachGameObject, prefab);
        if (_attachGameObject != null)
        {
			
			Vector3 pos = _attachGameObject.transform.localPosition;
			_go.transform.localPosition = new Vector3(pos.x, pos.y, -150f);
			Instance.positionFtue ();
		}
    }

    public virtual void positionFtue()
	{

	}

    /// <summary>
    /// Callback if ftue prefab fails
    /// </summary>
    /// <param name="assetPath"></param>
    /// <param name="data"></param>
    public static void CustomFtueLoadFailure(string assetPath, Dict data = null)
    {
        Debug.LogError("FTUE::CustomFtueLoadFailure - Failed to load asset at: " + assetPath);
    }

    /// <summary>
	/// Is the ftue enabled.
	/// </summary>
	/// <returns><c>true</c>, if ftue enabled, <c>false</c> otherwise.</returns>
	/// <param name="achievement">Achievement.</param>
	public static bool isFtueEnabled(string achievement)
	{
		if (CustomPlayerData.getInt(achievement, 0) != 1) 
		{
			return true;
		}
		return false;
	}
    
    /// <summary>
    /// Test function
    /// </summary>
    /// <param name="status"></param>
    /// <returns></returns>
    public bool StartFtue(int status)
    {
        int achievementFtueStep = status;
		NetworkProfileDialog networkProfileDialog = (NetworkProfileDialog)Dialog.instance.currentDialog;
		if (null == Dialog.instance.currentDialog || null == Dialog.instance.currentDialog.gameObject)
		{
			Debug.LogError("Dialog has already been closed");
			return false;
		}
		GameObject currentDialog = Dialog.instance.currentDialog.gameObject;

		if (networkProfileDialog.member.isUser &&  NetworkAchievements.isEnabled) {
			if (achievementFtueStep == ACHIEVEMENT_FTUE_STEP_0) {
				if (CustomPlayerData.getInt(CustomPlayerData.ACHIEVEMENTS_FTUE_0, 0) != 1) {
					ResetFtue ();
					LoadFtuePrefab ("Features/Achievements/Prefabs/FTUE Trophies Tab", currentDialog);
					CustomPlayerData.setValue(CustomPlayerData.ACHIEVEMENTS_FTUE_0, 1);
				}
			} else if (achievementFtueStep == ACHIEVEMENT_FTUE_STEP_1) {
				if (CustomPlayerData.getInt(CustomPlayerData.ACHIEVEMENTS_FTUE_1, 0) != 1) {
					ResetFtue ();
					LoadFtuePrefab ("Features/Achievements/Prefabs/FTUE Trophy Room", currentDialog);
					CustomPlayerData.setValue(CustomPlayerData.ACHIEVEMENTS_FTUE_1, 1);
				}
			} else if (achievementFtueStep == ACHIEVEMENT_FTUE_STEP_2) {
				if (CustomPlayerData.getInt(CustomPlayerData.ACHIEVEMENTS_FTUE_2, 0) != 1) {
					ResetFtue ();
					LoadFtuePrefab ("Features/Achievements/Prefabs/FTUE Profile Favorite", currentDialog);
					CustomPlayerData.setValue(CustomPlayerData.ACHIEVEMENTS_FTUE_2, 1);
				}
			} else if (achievementFtueStep == ACHIEVEMENT_FTUE_STEP_3) {
				if (CustomPlayerData.getInt(CustomPlayerData.ACHIEVEMENTS_FTUE_3, 0) != 1) {
					ResetFtue ();
					LoadFtuePrefab ("Features/Achievements/Prefabs/FTUE Favorite Trophy Selected", currentDialog);
					CustomPlayerData.setValue(CustomPlayerData.ACHIEVEMENTS_FTUE_3, 1);
				}
			} else if (achievementFtueStep == ACHIEVEMENT_FTUE_STEP_4) {
				if (CustomPlayerData.getInt(CustomPlayerData.ACHIEVEMENTS_FTUE_4, 0) != 1) {
					ResetFtue ();
					LoadFtuePrefab ("Features/Achievements/Prefabs/FTUE Ranks", currentDialog);
					CustomPlayerData.setValue(CustomPlayerData.ACHIEVEMENTS_FTUE_4, 1);
				}
			}
		}

        return true;
    }

	/// <summary>
	/// Resets the ftue static variables.
	/// </summary>
	public static void ResetFtue()
	{
		_ftueManager = null;
		_attachGameObject = null;
		_go = null;
	}
}
