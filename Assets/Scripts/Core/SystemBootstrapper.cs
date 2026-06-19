using UnityEngine;

namespace IdleOnDemo.Core
{
    /// <summary>
    /// Creates persistent project systems before the first scene loads.
    /// </summary>
    public static class SystemBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void InstantiatePersistentSystems()
        {
            Object systemsPrefab = Resources.Load("Systems");
            if (systemsPrefab == null)
            {
                Debug.LogWarning("SystemBootstrapper could not find a Resources prefab named 'Systems'. Create one at Assets/Resources/Systems.prefab to initialize persistent systems automatically.");
                return;
            }

            Object systemsInstance = Object.Instantiate(systemsPrefab);
            Object.DontDestroyOnLoad(systemsInstance);
        }
    }
}
