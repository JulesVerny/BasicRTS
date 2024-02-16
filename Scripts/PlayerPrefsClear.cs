using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPrefsClear : MonoBehaviour
{
    /// <summary>
    // Set The Game Object to Remain. (i.e. Not ToReload
    // Then Clear Upon Awake, ie. Only On Game Load First
    /// </summary>
    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
        PlayerPrefs.DeleteAll();    // Clear The Player prefs, to avoid Mission Success Failye, Script from Running
    }
}
