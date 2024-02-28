using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AbstractAbility
{
    public abstract void Use(AbstractUnit unit, Vector3 position);
}
