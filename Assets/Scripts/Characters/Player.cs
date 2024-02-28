using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : AbstractUnit
{
    [SerializeField] private int intellect;
    [SerializeField] private int food;    

    void Start()
    {
        base.Start();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            AttemptMove<CommonInteractiveObject>(1f, 0f);
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            AttemptMove<CommonInteractiveObject>(-1f, 0f);            
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            AttemptMove<CommonInteractiveObject>(0f, 1f);
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            AttemptMove<CommonInteractiveObject>(0f, -1f);
        }

        if (Input.GetMouseButtonDown(0))
        {            
            Vector3 pz = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            pz.z = 0;

            Vector3Int cellPosition = GameLogic.grid.WorldToCell(pz);
            Debug.Log("new values");
            Debug.Log(pfCell.x + " " + pfCell.y);
            Debug.Log(cellPosition);

            PathFinder.PFCell targetCell = new PathFinder.PFCell(cellPosition.x, cellPosition.y);
            currentWay = PathFinder.GetWay(pfCell, targetCell, (max_turns - current_turns));

            if(currentWay != null)
            {
                StartCoroutine(GoThroughWay(currentWay));
            }
        }
    }    

    public override void AfterMovement()
    {
        
    }

    public override void BeforeMovement()
    {
        
    }

    protected override void OnCantMove<T>(T Component)
    {
        
    }

    public override void Hurt(Interactive another)
    {
        
    }

    public override void Hurted(CommonInteractiveObject hurting)
    {
        
    }

    public override void AttemptMove<T>(float xDir, float yDir)
    {        
        if (gameLogicInstance.playersTurn)
        {
            current_turns++;
            if (current_turns >= max_turns)
            {
                gameLogicInstance.playersTurn = false;
                current_turns = 0;
            }
            base.AttemptMove<T>(xDir, yDir);
        }
    }
}
