using IdleOnDemo.Core;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using IdleOnDemo.Gameplay.Player;
using IdleOnDemo.Gameplay.Quests;

namespace IdleOnDemo.Gameplay.Environment
{
    /// <summary>
    /// Handles player-triggered scene travel when the player clicks while standing inside a portal trigger.
    /// </summary>
    [RequireComponent(typeof(Collider2D), typeof(Animator))]
    public sealed class Portal : MonoBehaviour
    {
        [SerializeField] private string targetSceneName;
        [SerializeField] private string targetSpawnPointID;
        [SerializeField] private string requiredQuestID;

        private bool isPlayerInPortal;
        private Transform playerTransform;
        private Animator portalAnimator;
        private static readonly int IsLockedHash = Animator.StringToHash("IsLocked");
        private bool isLocked;

        /// <summary>
        /// Caches the Animator component reference.
        /// </summary>
        private void Awake()
        {
            portalAnimator = GetComponent<Animator>();
        }

        /// <summary>
        /// Subscribes to quest updates and syncs the current lock state.
        /// </summary>
        private void OnEnable()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestUpdated += HandleQuestUpdated;
            }

            RefreshLockState();
        }

        /// <summary>
        /// Unsubscribes from quest updates to avoid stale event handlers.
        /// </summary>
        private void OnDisable()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestUpdated -= HandleQuestUpdated;
            }
        }

        /// <summary>
        /// Refreshes the lock state when the relevant quest's state changes.
        /// </summary>
        /// <param name="state">The quest runtime state that was updated.</param>
        private void HandleQuestUpdated(QuestRuntimeState state)
        {
            if (state == null || !string.Equals(state.QuestID, requiredQuestID, StringComparison.Ordinal))
            {
                return;
            }

            RefreshLockState();
        }

        /// <summary>
        /// Recomputes whether the portal is locked and updates the Animator accordingly.
        /// </summary>
        private void RefreshLockState()
        {
            isLocked = !string.IsNullOrEmpty(requiredQuestID) &&
                       (QuestManager.Instance == null || !QuestManager.Instance.GetQuestState(requiredQuestID).IsTurnedIn);

            if (portalAnimator != null)
            {
                portalAnimator.SetBool(IsLockedHash, isLocked);
            }
        }

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

            if (isLocked)
            {
                Debug.LogWarning($"Portal '{name}' requires quest '{requiredQuestID}' to be turned in.");
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