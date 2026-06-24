using UnityEngine;

public abstract class SimpleSingleton<T> : MonoBehaviour where T : Component
{
    public static T Instance { get; private set; }

    protected virtual void Awake() => Instance = this as T;
}

public abstract class Singleton<T> : MonoBehaviour where T : Component
{
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        //create singleton
        if (Instance) Destroy(gameObject);
        else
        {
            Instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
    }
}

public abstract class ComplexSingleton<T> : MonoBehaviour where T : Component
{
    private enum DestroyType { Destroy_New, Destroy_Old, Destroy_All }

    [Header("Singleton"), SerializeField]
    private DestroyType _destroyType = DestroyType.Destroy_New;

    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        //destroy existing
        if (_destroyType == DestroyType.Destroy_Old) Destroy(Instance.gameObject);
        else if (_destroyType == DestroyType.Destroy_All)
        {
            if (Instance) Destroy(Instance.gameObject);
            Destroy(gameObject);
        }
        
        //create singleton
        if (!Instance) { Instance = this as T; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }
}