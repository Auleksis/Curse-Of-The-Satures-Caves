using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CommonInteractiveObject : MonoBehaviour, Interactive
{
    public abstract void Hurt(Interactive another);
    public abstract void Hurted(CommonInteractiveObject hurting);
}
