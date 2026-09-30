using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : Component
{
    private static T _instance;

    public static T Instance
    {
        get
        {
            // If the instance is null, try to find it in the scene.
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<T>();

                // If it's still null, something is wrong.
                if (_instance == null)
                {
                    Debug.LogError($"An instance of type {typeof(T)} is needed in the scene, but there is none.");
                }
            }
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        // If no instance exists yet...
        if (_instance == null)
        {
            // ...this becomes the singleton instance.
            _instance = this as T;

            // And we make it persistent across scene loads.
            DontDestroyOnLoad(this.gameObject);
        }
        // If an instance already exists and it's not this one...
        else if (_instance != this)
        {
            // ...then this is a duplicate and should be destroyed.
            Destroy(gameObject);
        }
    }
}