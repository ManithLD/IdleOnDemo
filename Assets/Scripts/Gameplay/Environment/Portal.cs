using IdleOnDemo.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IdleOnDemo.Gameplay.Environment
{
    /// <summary>
    /// Handles player-triggered scene travel when the player clicks while standing inside a portal trigger.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class Portal : MonoBehaviour
    {
        [SerializeField] private string targetSceneName;
        [SerializeField] private string targetSpawnPointID;

        private bool isPlayerInPortal;
        private Transform playerTransform;

        /// <summary>
        /// Checks for player click input while the player is inside the portal trigger.
        /// </summary>
        private void Update()
        {
            if (!isPlayerInPortal ||
                Mouse.current == null ||
                !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            SceneTransitionManager transitionManager = SceneTransitionManager.Instance;
            if (transitionManager == null)
            {
                Debug.LogWarning($"Portal '{name}' could not transition because no SceneTransitionManager is active.");
                return;
            }

            transitionManager.TransitionToScene(targetSceneName, targetSpawnPointID, playerTransform);
        }

        /// <summary>
        /// Tracks when the player enters this portal's trigger area.
        /// </summary>
        /// <param name="other">The collider entering the portal trigger.</param>
        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController playerController = other.GetComponentInParent<PlayerController>();
            if (playerController == null)
            {
                return;
            }

            playerTransform = playerController.transform;
            isPlayerInPortal = true;
        }

        /// <summary>
        /// Clears tracked player state when the player leaves this portal's trigger area.
        /// </summary>
        /// <param name="other">The collider exiting the portal trigger.</param>
        private void OnTriggerExit2D(Collider2D other)
        {
            PlayerController playerController = other.GetComponentInParent<PlayerController>();
            if (playerTransform == null || playerController == null || playerController.transform != playerTransform)
            {
                return;
            }

            playerTransform = null;
            isPlayerInPortal = false;
        }

        /// <summary>
        /// Ensures the portal collider is configured as a trigger when edited in the Inspector.
        /// </summary>
        private void OnValidate()
        {
            Collider2D portalCollider = GetComponent<Collider2D>();
            if (portalCollider != null)
            {
                portalCollider.isTrigger = true;
            }
        }
    }
}
