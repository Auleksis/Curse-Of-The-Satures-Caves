using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PairPurposeInt
{   
    public Purpose purpose;
    public int count;
    [HideInInspector] public List<RoomUnit> rooms = new List<RoomUnit>();
}
