using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AbstractUnit : CommonInteractiveObject
{
    [SerializeField] protected int health;
    [SerializeField] protected int strength;
    [SerializeField] protected float speed;
    [SerializeField] protected LayerMask blockingLayer;

    protected Rigidbody2D rb;
    protected BoxCollider2D boxCollider;
    protected GameLogic gameLogicInstance;


    protected AbstractAbility ability;

    protected List<AbstractIllness> illnesses;

    private float inverseMoveTime;
    private float moveTime = 0.05f;

    private static float xMoveDistance;
    private static float yMoveDistance;

    private bool isGoing;

    protected int max_turns;
    protected int current_turns;

    protected bool toRight;
    public PathFinder.PFCell pfCell;

    protected Stack<PathFinder.PFCell> currentWay;

    protected virtual void Start()
    {
        xMoveDistance = GameObject.Find("Grid").GetComponent<Grid>().cellSize.x;
        yMoveDistance = GameObject.Find("Grid").GetComponent<Grid>().cellSize.y;
        isGoing = false;
        toRight = true;

        gameLogicInstance = GameObject.Find("GameLogic").GetComponent<GameLogic>();
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();

        illnesses = new List<AbstractIllness>();
        ability = null;

        currentWay = null;

        max_turns = Mathf.CeilToInt(Mathf.Lerp(0, 4, (speed / 100f)));
        current_turns = 0;

        inverseMoveTime = 1f / moveTime;
    }

    protected IEnumerator SmoothMovement(Vector3 end)
    {
        float sqrRemainingDistance = (end - transform.position).sqrMagnitude;

        while(sqrRemainingDistance > float.Epsilon)
        {
            Vector3 newPosition = Vector3.MoveTowards(rb.position, end, inverseMoveTime * Time.deltaTime);
            rb.MovePosition(newPosition);
            sqrRemainingDistance = (end - transform.position).sqrMagnitude;

            yield return null;
        }
    }

    public abstract void BeforeMovement();

    public IEnumerator GoThroughWay(Stack<PathFinder.PFCell> way)
    {
        if (!isGoing)
        {
            isGoing = true;
            while (way.Count > 0)
            {
                PathFinder.PFCell cell = currentWay.Pop();
                AttemptMove<CommonInteractiveObject>(cell.x - pfCell.x, cell.y - pfCell.y);

                yield return new WaitForSeconds(moveTime * 5f);
            }
            isGoing = false;
        }
    }

    public virtual void AttemptMove<T>(float xDir, float yDir) where T: Component
    {
        if (xDir < 0 && toRight || xDir > 0 && !toRight)
            Flip();

        RaycastHit2D hit;

        bool canMove = Move(xDir, yDir, out hit);

        if (hit.transform == null)
        {
            pfCell.x += (int)xDir;
            pfCell.y += (int)yDir;
            return;
        }

        T hitComponent = hit.transform.GetComponent<T>();

        if(!canMove && hitComponent != null)
        {
            OnCantMove(hitComponent);
        }
    }

    public virtual bool Move(float xDir, float yDir, out RaycastHit2D hit)
    {
        Vector2 start = transform.position;

        Vector2 end = start + new Vector2(xDir * xMoveDistance, yDir * yMoveDistance);

        boxCollider.enabled = false;
        hit = Physics2D.Linecast(start, end, blockingLayer);
        boxCollider.enabled = true;

        if(hit.transform == null)
        {
            StartCoroutine(SmoothMovement(end));
            return true;
        }

        return false;
    }

    protected abstract void OnCantMove<T>(T Component) where T : Component;

    public abstract void AfterMovement();

    public abstract override void Hurt(Interactive another);
    public abstract override void Hurted(CommonInteractiveObject hurting);

    protected virtual void Flip()
    {
        toRight = !toRight;
        Vector3 currentScale = gameObject.transform.localScale;
        currentScale.x *= -1;
        gameObject.transform.localScale = currentScale;
    }
}
