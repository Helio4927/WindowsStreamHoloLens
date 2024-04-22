using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MonoBehaviourInstance<T> : MonoBehaviour where T : MonoBehaviourInstance<T>
{
    private static T instance;

    protected virtual void Awake()
    {
        if (instance && instance != this)
        {
            Destroy(this);
        }
        else
        {
            instance = (T)this;
            OnInstance();
        }
    }

    protected virtual void OnInstance()
    {
    
    }

    public static T GetInstance()
    {
        return instance;
    }
}
