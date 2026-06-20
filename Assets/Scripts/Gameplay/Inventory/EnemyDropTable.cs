using System;
using System.Collections.Generic;
using IdleOnDemo.Gameplay.Enemies;
using UnityEngine;

namespace IdleOnDemo.Gameplay.Inventory
{
    /// <summary>
    /// Spawns configured item pickups when an enemy dies.
    /// </summary>
    [RequireComponent(typeof(EnemyController))]
    public sealed class EnemyDropTable : MonoBehaviour
    {
        private const float DropVerticalOffset = 0.5f;

        [Serializable]
        private sealed class DropEntry
        {
            public ItemData item;
            public int minAmount = 1;
            public int maxAmount = 1;
            [Range(0f, 1f)] public float dropChance = 1f;
        }

        [SerializeField] private List<DropEntry> drops = new List<DropEntry>();
        [SerializeField] private float spawnScatterRadius = 1f;

        private EnemyController enemyController;
        private bool hasDropped;

        /// <summary>
        /// Caches the required enemy controller reference.
        /// </summary>
        private void Awake()
        {
            enemyController = GetComponent<EnemyController>();
        }

        /// <summary>
        /// Subscribes to the enemy death event so drops stay outside the enemy lifecycle logic.
        /// </summary>
        private void OnEnable()
        {
            hasDropped = false;
            enemyController ??= GetComponent<EnemyController>();
            if (enemyController != null)
            {
                enemyController.OnDeath += HandleEnemyDeath;
            }
        }

        /// <summary>
        /// Unsubscribes from enemy death events when this drop table is disabled.
        /// </summary>
        private void OnDisable()
        {
            if (enemyController != null)
            {
                enemyController.OnDeath -= HandleEnemyDeath;
            }
        }

        /// <summary>
        /// Rolls each configured drop entry once and spawns successful item pickups.
        /// </summary>
        private void HandleEnemyDeath()
        {
            if (hasDropped)
            {
                return;
            }

            hasDropped = true;
            if (drops == null)
            {
                return;
            }

            foreach (DropEntry drop in drops)
            {
                ClampDrop(drop);
                if (drop.item == null || UnityEngine.Random.value > drop.dropChance)
                {
                    continue;
                }

                if (drop.item.DropPrefab == null)
                {
                    Debug.LogWarning($"EnemyDropTable could not spawn drop for {drop.item.name} because its ItemData has no drop prefab assigned.");
                    continue;
                }

                int quantity = UnityEngine.Random.Range(drop.minAmount, drop.maxAmount + 1);
                if (quantity <= 0)
                {
                    continue;
                }

                float spawnX = transform.position.x + GetScatterOffset().x;
                float spawnY = transform.position.y;
                if (enemyController.HomeZone != null)
                {
                    BoxCollider2D zoneCollider = enemyController.HomeZone.GetComponent<BoxCollider2D>();
                    if (zoneCollider != null)
                    {
                        spawnY = zoneCollider.bounds.min.y;
                    }
                }

                Vector3 spawnPosition = new Vector3(spawnX, spawnY + DropVerticalOffset, 0f);
                ItemPickup pickup = Instantiate(drop.item.DropPrefab, spawnPosition, Quaternion.identity);
                pickup.Initialize(drop.item, quantity);
            }
        }

        /// <summary>
        /// Calculates a small random horizontal world-space offset so multiple pickups do not overlap exactly.
        /// </summary>
        /// <returns>A scatter offset around this enemy's position.</returns>
        private Vector3 GetScatterOffset()
        {
            if (spawnScatterRadius <= 0f)
            {
                return Vector3.zero;
            }

            float xOffset = UnityEngine.Random.Range(-spawnScatterRadius, spawnScatterRadius);
            return new Vector3(xOffset, 0f, 0f);
        }

        /// <summary>
        /// Keeps serialized drop table values valid in the Inspector.
        /// </summary>
        private void OnValidate()
        {
            spawnScatterRadius = Mathf.Max(0f, spawnScatterRadius);
            if (drops == null)
            {
                return;
            }

            for (int i = 0; i < drops.Count; i++)
            {
                ClampDrop(drops[i]);
            }
        }

        /// <summary>
        /// Clamps a drop entry to safe roll and quantity ranges.
        /// </summary>
        /// <param name="drop">The drop entry to clamp.</param>
        private void ClampDrop(DropEntry drop)
        {
            if (drop == null)
            {
                return;
            }

            drop.minAmount = Mathf.Max(1, drop.minAmount);
            drop.maxAmount = Mathf.Max(drop.minAmount, drop.maxAmount);
            drop.dropChance = Mathf.Clamp01(drop.dropChance);
        }
    }
}
