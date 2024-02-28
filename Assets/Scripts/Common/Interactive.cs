using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface Interactive
{
    public void Hurt(Interactive another);
    public void Hurted(CommonInteractiveObject hurting);
}
