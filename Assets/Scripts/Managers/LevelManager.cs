using Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using SimplePlatformer.Enemy;
using UnityEngine.Audio;
using SimplePlatformer.Player;

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;
    public Transform currentRespawnPoint;
    public GameObject playerPrefab;
    public GameObject enemyContainer;
    public GameObject bossContainer;
    public GameObject celldoorsContainer;
    public GameObject keysContainer;
    public GameObject chestsContainer;
    //Enemies positions and prefabs
    private List<RespawnEntityData> enemies;
    private List<RespawnEntityData> bosses;
    internal List<GameObject> levelKeys;
    internal List<Chest> chests;
    public List<CellDoor> celldoors;
    public Room currentRoom;
    public CanvasGroup bossHealthbar;
    public AudioMixer audioMixer;
    private GameObject player;

    public CinemachineVirtualCameraBase virtualCamera;

    private void OnEnable()
    {
        GameEvents.NewSpawnHandler += UpdateNewSpawn;
        GameEvents.OnPlayerDeath += FadeBossHealthBar;
    }

    private void OnDisable()
    {

        GameEvents.NewSpawnHandler -= UpdateNewSpawn;
        GameEvents.OnPlayerDeath -= FadeBossHealthBar;
    }

    public void UpdateCurrentRoom(GameObject room)
    {
        currentRoom = room.GetComponent<Room>();
    }

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player");

        //Gets enemies
        enemies = new List<RespawnEntityData>();
        foreach (Transform child in enemyContainer.transform)
        {
            enemies.Add(new RespawnEntityData(child.GetComponent<Enemy>(), child.position));
        }
        //Gets bosses
        bosses = new List<RespawnEntityData>();
        foreach (Transform child in bossContainer.transform)
        {
            bosses.Add(new RespawnEntityData(child.GetComponent<BossBehaviour>(), child.position));
        }
        celldoors = new List<CellDoor>();
        foreach (Transform child in celldoorsContainer.transform)
        {
            celldoors.Add(child.gameObject.GetComponent<CellDoor>());
        }
        levelKeys = new List<GameObject>();
        foreach (Transform child in keysContainer.transform)
        {
            levelKeys.Add(child.gameObject);
        }
        chests = new List<Chest>();
        foreach (Transform child in chestsContainer.transform)
        {
            chests.Add(child.gameObject.GetComponent<Chest>());
        }

        //Instance
        instance = this;

    }

    private void Start()
    {
        //Current spawnpoint
        currentRespawnPoint = RespawnManager.currentRespawn.transform;
    }


    public void SetCelldoors()
    {
        int i = 0;
        List<bool> cellDoorsBool = GlobalControl.Instance.LocalCopyOfData.cellDoors;
        foreach (CellDoor c in celldoors)
        {
            c.gameObject.SetActive(cellDoorsBool[i]);
            i++;
        }
    }

    public void SetLevelKeys()
    {
        int i = 0;
        List<bool> levelKeysBool = GlobalControl.Instance.LocalCopyOfData.levelKeys;
        foreach (GameObject c in this.levelKeys)
        {
            c.gameObject.SetActive(levelKeysBool[i]);
            i++;
        }
    }

    public void SetChests()
    {
        int i = 0;
        List<bool> chestsBool = GlobalControl.Instance.LocalCopyOfData.chests;
        foreach (Chest c in chests)
        {
            if (chestsBool[i])
            {
                c.Interacted();
            }
            i++;
        }
    }

    public void Respawn()
    {
        //TODO: If player is not dead
        //Set an event to respawn all enemies because every enemy handles his own destruction
        GameEvents.RespawnEnemiesHandler();
        foreach (RespawnEntityData ed in enemies)
        {
            GameObject instance = Instantiate(ed.enemy._enemyData.prefab, ed.position, Quaternion.identity, enemyContainer.transform);
            instance.GetComponent<Enemy>().dropItem = ed.enemy.dropItem;
            instance.GetComponent<Enemy>().dropChance = ed.enemy.dropChance;
            instance.GetComponent<Enemy>().patrollingEnabled = ed.enemy.patrollingEnabled;
        }
        foreach (RespawnEntityData bs in bosses)
        {
            GameObject instance = Instantiate(bs.boss._bossData.prefab, bs.position, Quaternion.identity, bossContainer.transform);
        }

        //player = Instantiate(playerPrefab, currentRespawnPoint.position, Quaternion.identity);

        //Set the follow to the actual virtual camera (don't know why I did this)
        //virtualCamera.Follow = player.transform;
        //Set the follow to all virtual cameras with the respawn Event
        GameEvents.RespawnHandler?.Invoke();
        //GameManager.GetInstance().TogglePlayerDeath(false);
        audioMixer.FindSnapshot("Level").TransitionTo(2f);

        player.GetComponent<PlayerController>().Respawn();

        GameManager.GetInstance().TogglePlayerDeath(false);
    }

    public void FadeBossHealthBar()
    {
        float duration = 1f;
        LeanTween.alphaCanvas(bossHealthbar, 0f, duration);
    }

    private void UpdateNewSpawn()
    {
        currentRespawnPoint = RespawnManager.currentRespawn.transform;
    }

}
