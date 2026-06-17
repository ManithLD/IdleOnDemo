using System.Collections.Generic;
using IdleOnDemo.Gameplay.Enemies;
using UnityEngine;

namespace IdleOnDemo.Gameplay.Environment
{
    /// <summary>
    /// Spawns and tracks enemies inside a rectangular roaming zone.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class EnemySpawnerZone : MonoBehaviour
    {
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private float spawnCooldown = 5f;
        [SerializeField] private int maxEnemies = 3;

        private readonly List<GameObject> activeEnemies = new();
        private BoxCollider2D zoneCollider;
        private float spawnTimer;

        /// <summary>
        /// Caches the zone collider and primes the first spawn.
        /// </summary>
        private void Awake()
        {
            zoneCollider = GetComponent<BoxCollider2D>();
            zoneCollider.isTrigger = true;
            spawnTimer = spawnCooldown;
        }

        /// <summary>
        /// Advances the spawn timer and creates enemies until the configured cap is reached.
        /// </summary>
        private void Update()
        {
            RemoveDestroyedEnemies();

            if (enemyPrefab == null || activeEnemies.Count >= maxEnemies)
            {
                return;
            }

            spawnTimer += Time.deltaTime;
            if (spawnTimer < spawnCooldown)
            {
                return;
            }

            SpawnEnemy();
            spawnTimer = 0f;
        }

        /// <summary>
        /// Gets a random world-space point inside this zone, using the collider bottom edge for grounded enemies.
        /// </summary>
        /// <returns>A random point along the bottom edge of the zone collider.</returns>
        public Vector2 GetRandomPointInZone()
        {
            zoneCollider ??= GetComponent<BoxCollider2D>();

            Bounds bounds = zoneCollider.bounds;
            float randomX = Random.Range(bounds.min.x, bounds.max.x);
            return new Vector2(randomX, bounds.min.y);
        }

        /// <summary>
        /// Instantiates an enemy and initializes its home roaming zone.
        /// </summary>
        private void SpawnEnemy()
        {
            GameObject enemy = Instantiate(enemyPrefab, GetRandomPointInZone(), Quaternion.identity);
            enemy.transform.SetParent(transform.parent, true);

            EnemyController enemyController = enemy.GetComponent<EnemyController>();
            if (enemyController != null)
            {
                enemyController.InitializeSpawnerBounds(this);
            }

            activeEnemies.Add(enemy);
        }

        /// <summary>
        /// Removes destroyed enemy references before enforcing the active enemy cap.
        /// </summary>
        private void RemoveDestroyedEnemies()
        {
            activeEnemies.RemoveAll(enemy => enemy == null);
        }

        /// <summary>
        /// Keeps the zone collider and serialized tuning values valid when edited in the Inspector.
        /// </summary>
        private void OnValidate()
        {
            BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
            if (boxCollider != null)
            {
                boxCollider.isTrigger = true;
            }

            spawnCooldown = Mathf.Max(0.1f, spawnCooldown);
            maxEnemies = Mathf.Max(1, maxEnemies);
        }

        /// <summary>
        /// Draws the spawning zone bounds in the Scene view.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
            if (boxCollider == null)
            {
                return;
            }

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(boxCollider.bounds.center, boxCollider.bounds.size);
        }
    }
}
