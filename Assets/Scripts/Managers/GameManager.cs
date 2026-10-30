using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System;
using SimplePlatformer.Player;

/// <summary>
/// TODO: Remove static behaviour for Network purposes (netcode)
/// Manages alot of things...
/// </summary>
public class GameManager : MonoBehaviour
{
    // If it's multiplier this props should not be shared
    protected int score = 0;
    public bool isPaused = false;
    public bool PlayerIsDead = false; 
    public bool hasBow;
    private RespawnManager respawnManager;
    private LevelManager levelManager;
    private PlayerController player;
    private Coroutine showDeathMenuCo;

    /// <summary>
    /// Type of key and quantity
    /// </summary>
    protected Dictionary<KeyColor,int> keys;

    //Instance
    static GameManager instance;

    public static GameManager GetInstance()
    {
        return instance;
    }

    public Dictionary<KeyColor,int> GetKeys()
    {
        return keys;
    }

    public void Start()
    {
        instance = this;

        this.levelManager = GameObject.FindObjectOfType<LevelManager>();
        this.respawnManager = GameObject.FindObjectOfType<RespawnManager>();

        //Loading Data
        if (GlobalControl.Instance.IsSceneBeingLoaded)
        {
            score = GlobalControl.Instance.LocalCopyOfData.score;
            Vector3 position;
            position.x = GlobalControl.Instance.LocalCopyOfData.position[0];
            position.y = GlobalControl.Instance.LocalCopyOfData.position[1];
            position.z = GlobalControl.Instance.LocalCopyOfData.position[2];

            //player.transform.position = position;
            //player.playerCombatBehaviour.hasBow = GlobalControl.Instance.LocalCopyOfData.hasBow;
            //player.healthSystem.SetHealth(GlobalControl.Instance.LocalCopyOfData.health);
            levelManager.SetCelldoors();
            levelManager.SetLevelKeys();
            levelManager.SetChests();
        }

        player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();

        //Loads the spawns or start from the first spawn (if it is a new game)
        respawnManager.LoadSpawns(GlobalControl.Instance.IsSceneBeingLoaded);

        //Loads the keys 
        InitializeKeys();

        //End Loading
        GlobalControl.Instance.IsSceneBeingLoaded = false;

    }

    //------
    //Save
    //------
    public void SaveGame()
    {
        List<CampFire> campFires = respawnManager.respawns;
        List<CellDoor> cellDoors = levelManager.celldoors;
        List<GameObject> levelKeys = levelManager.levelKeys;
        List<Chest> chests = levelManager.chests;
        SaveSystem.SaveGame(player, score, campFires,cellDoors,GetKeys(),levelKeys,chests);
    }

    public void TogglePauseState()
    {
        isPaused = !isPaused;
        //player.DisablePlayerState(isPaused);
        ToggleTimeScale();
        SwitchFocusedPlayerControlScheme();
        UpdateUIMenu();

    }

    void SwitchFocusedPlayerControlScheme()
    {
        switch (isPaused)
        {
            case true:
                //player.EnablePauseMenuControls();
                break;

            case false:
                //player.EnableGameplayControls();
                break;
        }
    }

    private void UpdateUIMenu()
    {
        UIManager.GetInstance().UpdateUIMenuState(isPaused);
    }

    private void UpdateUIDeath()
    {
        UIManager.GetInstance().UpdateUIDeathState(PlayerIsDead);
    }

    public void TogglePlayerDeath(bool death)
    {
        PlayerIsDead = death;
        if (showDeathMenuCo != null) StopCoroutine(showDeathMenuCo);

        if (death)
        {
            // Let the death animation play before showing the menu
            showDeathMenuCo = StartCoroutine(DieCo());
        }
        else
        {
            showDeathMenuCo = null;
            UpdateUIDeath();
        }
    }

    private IEnumerator DieCo()
    {
        yield return new WaitForSeconds(2f);
        UpdateUIDeath();
    }


    /// <summary>
    /// Initialize the keys Dictionary.
    /// Adds every KeyColor into the list and set it to zero (amount of keys)
    /// </summary>
    private void InitializeKeys()
    {
        if (keys == null)
        {
            this.keys = new Dictionary<KeyColor, int>();
        }

        //Everything to 0
        foreach (KeyColor kc in (KeyColor[])Enum.GetValues(typeof(KeyColor)))
        {
            this.keys.Add(kc, 0);
        }

        if (GlobalControl.Instance.IsSceneBeingLoaded)
        {
            //Loading
            int i = 0;
            List<int> keys = GlobalControl.Instance.LocalCopyOfData.keys;
            //Foreach through the Enum of Key Colors and store it into a List<int> with the amount of keys of each color
            foreach (KeyColor kc in (KeyColor[])Enum.GetValues(typeof(KeyColor)))
            {
                this.keys[kc] = keys[i];
                i++;
            }
            GameEvents.UpdateKeysHandler();
        }
        
    }

    /// <summary>
    /// Increments the amount of keys in the color passed
    /// </summary>
    /// <param name="kc"></param>
    public void AddKey(KeyColor kc)
    {
        if (keys == null)
        {
            keys = new Dictionary<KeyColor, int>();
        }

        if (keys.ContainsKey(kc))
        {
            keys[kc] += 1;
        }
        GameEvents.UpdateKeysHandler();
        
    }

    /// <summary>
    /// Decreases the amount of keys of that Color by 1.
    /// </summary>
    /// <param name="kc"></param>
    /// <returns bool ></returns> Returns true if not zero
    public bool DeleteKey(KeyColor kc)
    {
        if (keys.ContainsKey(kc) && keys[kc] > 0)
        {
            keys[kc] -= 1;
            GameEvents.UpdateKeysHandler();
            return true;
        }
        else
        {
            return false;
        }
        
    }

    void OnDestroy()
    {
        //Debug.Log("GameStatus was destroyed.");

        // Before we get destroyed, let's save our data to our save file.
        // This is "Implementation #1".
        // This will happen whenever this object is destroyed, which
        // includes scene changes as well as simply exiting the program.
        //PlayerPrefs.SetInt("score", score);
        //PlayerPrefs.SetInt("lives", numLives);
    }

    public void AddScore(int s)
    {
        score += s;
        // We could take this opportunity to save the score in a file/UserPrefs
        // But depending on implentation, this could be slow/inneficient.
        // On the other hand, if we are constantly saving the data on 
        // the fly, it means that we pretty never lose anything to a crash.
    }

    public int GetScore()
    {
        return score;
    }

    //Pause Utilities ----

    void ToggleTimeScale()
    {
        float newTimeScale = 0f;

        switch (isPaused)
        {
            case true:
                newTimeScale = 0f;
                break;

            case false:
                newTimeScale = 1f;
                break;
        }

        Time.timeScale = newTimeScale;
    }


}
