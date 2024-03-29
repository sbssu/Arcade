using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        Instance = this as T;
    }
}

public static class Custom
{
    public static T GetRandom<T>(this IEnumerable<T> target)
    {
        int count = -1;
        int index = Random.Range(0, target.Count() - 1);
        foreach(T value in target)
        {
            count += 1;
            if (count == index)
                return value;
        }
        return default(T);
    }
}

