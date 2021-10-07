using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VirtualPetTreat
{
    public string keyName = "";
    public long hyperStateSeconds = 0;
    public string iconSpriteName = "";

    public VirtualPetTreat(string name, JSON data)
    {
        keyName = name;
        hyperStateSeconds = data.getLong("hyper_state_seconds", 0);
        iconSpriteName = data.getString("icon_path", "");
    }
}
