using UnityEngine;

namespace IdleOnDemo.Gameplay.Environment
{
    /// <summary>
    /// Marks a scene position that can receive a persistent player during scene transitions.
    /// </summary>
    public sealed class SpawnPoint : MonoBehaviour
    {
        [SerializeField] private string spawnPointID;

        /// <summary>
        /// Gets the unique identifier used by scene transitions to locate this spawn point.
        /// </summary>
        public string SpawnPointID => spawnPointID;

        /// <summary>
        /// Draws an editor-only marker to make spawn points easy to locate in the Scene view.
        /// </summary>
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.25f);
        }
    }
}
