using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using UnityEngine;

public class MountPoint : MonoBehaviour
{
    // ==========================================================================================================

    public bool HasTurret;

    [SerializeField]
    GameObject TheSelectorObject;
    public bool CurrentlySelected;
    public int MountIndex;
    public GameManager.Faction TheFaction; 
    // ==========================================================================================================
    private void Awake()
    {
        HasTurret = false;
        TheFaction = GameManager.Faction.None; 
    }
    // ==========================================================================================================
    public void SetSelection(bool NewSelectionMode)
    {
        if (TheSelectorObject != null) TheSelectorObject.SetActive(NewSelectionMode);
        CurrentlySelected = NewSelectionMode;
    } // SetSelection
    // =========================================================================================================
    void Start()
    {
        
    }
    // ==========================================================================================================

    // Update is called once per frame
    void Update()
    {
        
    }
    // =====================================================
    public void SetAffinity(GameManager.Faction TheAssignedAffinity)
    {
        // Set Affinity After being Created. 
        TheFaction = TheAssignedAffinity;
    } 

    // ========================================
    public void SetMounted()
    {
        HasTurret = true;
    }
    // =================================
    public void ClearMount()
    {
        HasTurret = false;
    }
    // =================================================================
    
}
