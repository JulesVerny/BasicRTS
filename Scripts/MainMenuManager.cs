using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    //
    // Static Wide Player Response Variables - To Hold Between Scenes  
    static public int NumberPlayerWins =0;
    static public int NumberAIWins =0;
    static public string LastGameResponse = "Have Fun";

    [SerializeField]
    public TMP_Text LastResponseTB;
    [SerializeField]
    public TMP_Text NumPlayerWinsTB;
    [SerializeField]
    public TMP_Text NumAIWinsTB;

    [SerializeField]
    Toggle EasyToggle;
    [SerializeField]
    Toggle MediumToggle;
    [SerializeField]
    Toggle HardToggle;

    // Sound Effects 
    [Header("Sound Effects")]
    [SerializeField] private AudioSource TheAudioPlayer;
    [SerializeField] private AudioClip MissionSuccessClip;
    [SerializeField] private AudioClip MissionFailureClip;
    // =============================================================================

    // ==============================================================================
    void Start()
    {
        // Don't do Anything Here - As will be called when Reloaded

        // However Check the Player Prefs for Last Winner
        if(PlayerPrefs.GetInt("Winner") == 1) TheAudioPlayer.PlayOneShot(MissionFailureClip);
        if (PlayerPrefs.GetInt("Winner") == 2) TheAudioPlayer.PlayOneShot(MissionSuccessClip);
    }
    // ===============================================================================
    // Update is called once per frame
    void Update()
    {
        LastResponseTB.text = LastGameResponse;
        NumPlayerWinsTB.text = NumberPlayerWins.ToString();
        NumAIWinsTB.text = NumberAIWins.ToString();
    }
    // =========================================================================

    // ===============================================================================
    public void PlayTheGame()
    {
        // Check the Selected Game Difficulty
        GameManager.TheGameDifficulty = GameManager.GameDifficultyType.Easy;

        if (EasyToggle.isOn) GameManager.TheGameDifficulty = GameManager.GameDifficultyType.Easy;
        if (MediumToggle.isOn) GameManager.TheGameDifficulty = GameManager.GameDifficultyType.Medium;
        if (HardToggle.isOn) GameManager.TheGameDifficulty = GameManager.GameDifficultyType.Hard;

        Debug.Log("[INFO]: The Player Has Selected to Play the Game at : " + GameManager.TheGameDifficulty.ToString() + " difficluty"); 
        // Load the Main Play Scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(1);

    } // PlayTheGame


    // ===============================================================================
    public void ExitButton()
    {
        PlayerPrefs.SetInt("Winner", 0);
        // Load the Main Play Scene
        Application.Quit();

    } // PlayTheGame

    // ===============================================================================
}
