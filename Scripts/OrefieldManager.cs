using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrefieldManager : MonoBehaviour
{
    // =========================================================================================
    
    public int CurrentFill;  

    // =========================================================================================
    void Start()
    {
        CurrentFill = 10;
        
    }
    // =========================================================================================
    // Update is called once per frame
    void Update()
    {
        // Debug the Material Update
        if (Input.GetKeyUp(KeyCode.T))
        {
            DecrementOreField();
        }
        if (Input.GetKeyUp(KeyCode.U))
        {
            IncrementOreField();
        }

    } // Update
    // =========================================================================================
    public void SetOreFieldFill(int Fill)
    {
        CurrentFill = Fill;
        UpdateMaterial();
    }
    public void DecrementOreField()
    {
        if(CurrentFill>0) CurrentFill = CurrentFill - 1;
        UpdateMaterial();
    }

    public void IncrementOreField()
    {
        if (CurrentFill < 11) CurrentFill = CurrentFill + 1;
        UpdateMaterial();
    }
    // ==========================================================
    private void UpdateMaterial()
    {
        float DisolveFactor = 11.0f - (float)CurrentFill;
        var material = GetComponent<Renderer>().material;
       
        //Call SetColor using the shader property name "_Color" and "_Color2" and setting their color to red
        material.SetFloat("_DisolveFactor", DisolveFactor);
    } // UpdateMaterial

    // =========================================================================================
}
