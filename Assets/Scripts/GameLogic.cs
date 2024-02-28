using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLogic : MonoBehaviour
{
    public float levelStartDelay = 2f;

    public float turnDelay = 1f;

    public static GameLogic instance = null;

    public static Grid grid = null;

    public int maxEnemiesCount = 100;

    [HideInInspector] public bool playersTurn = true;

    private List<Enemy> enemies;

    private LevelGen generator;

    private Player player;

    private bool enemiesTurn;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);

        generator = GetComponent<LevelGen>();

        enemies = new List<Enemy>();

        player = GameObject.Find("player_test").GetComponent<Player>();

        enemiesTurn = false;

        InitLevel();
    }

    private void InitLevel()
    {
        enemies.Clear();
        generator.SetUpLevel();

        grid = generator.levelGrid;

        PathFinder.Init();

        player.transform.position = LevelGen.player_spawn;
        player.pfCell = LevelGen.player_PF_position;
    }

    void Start()
    {

    }

    void Update()
    {
        if (playersTurn || enemiesTurn)
            return;
        
        StartCoroutine(EnemiesTurn());        
    }    

    private IEnumerator EnemiesTurn()
    {
        player.AfterMovement();

        enemiesTurn = true;

        if(enemies.Count < maxEnemiesCount)
        {            
            Spawner spawner = LevelGen.all_spawners[Random.Range(0, LevelGen.all_spawners.Count)];
            GameObject newEnemy = Instantiate(spawner.availableEnemies[Random.Range(0, spawner.availableEnemies.Length)]);
            newEnemy.transform.position = spawner.spawn_position;
            newEnemy.GetComponent<Enemy>().pfCell = spawner.pfCell;
            enemies.Add(newEnemy.GetComponent<Enemy>());           
        }

        if(enemies.Count == 0)
            yield return new WaitForSeconds(turnDelay);

        for(int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            enemy.BeforeMovement();            
        }

        playersTurn = true;
        enemiesTurn = false;

        player.BeforeMovement();
    }
}
