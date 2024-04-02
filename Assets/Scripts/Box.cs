using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Box : MonoBehaviour
{
    public class Item
    {
        public string name;
        public int grade;
    }

    Item[] items;

    public Item GetRandomBox()
    {
        return items.GetRandom();
    }
}
