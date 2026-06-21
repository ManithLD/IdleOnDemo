using IdleOnDemo.Core;
using System;
using System.Collections.Generic;
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
        [SerializeField] private List<string> objectiveIDs = new List<string>();

        private bool isPlayerInPortal;
        private Transform playerTransform;
        private Animator portalAnimator;
        private static readonly int IsLockedHash = Animator.StringToHash("IsLocked");
        private static readonly int PortalLockedStateHash = Animator.StringToHash("PortalLockedState");
        private static readonly int PortalUnlockedStateHash = Animator.StringToHash("PortalUnlockedState");
        private bool isLocked;

        /// <summary>
        /// Caches the Animator component reference.
        /// </summary>
        private void Awake()
        {
            portalAnimator = GetComponent<Animator>();
        }

        /// <summary>
        /// Subscribes to quest updates and snaps to the correct lock state with no transition delay.
        /// Handles re-enable cycles (e.g. pooling, toggled portals) after the initial scene load.
        /// </summary>
        private void OnEnable()
        {
            SubscribeToQuestUpdates();
            RefreshLockState(snapImmediately: true);
        }

        /// <summary>
        /// Re-syncs the lock state once every object's Awake/OnEnable in the scene has run,
        /// guarding against the case where QuestManager.Instance wasn't set yet during OnEnable.
        /// </summary>
        private void Start()
        {
            SubscribeToQuestUpdates();
            RefreshLockState(snapImmediately: true);
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
        /// Subscribes to QuestManager's update event, idempotently, so it's safe to call
        /// from both OnEnable and Start without risking a duplicate subscription.
        /// </summary>
        private void SubscribeToQuestUpdates()
        {
            if (QuestManager.Instance == null)
            {
                return;
            }

            QuestManager.Instance.OnQuestUpdated -= HandleQuestUpdated;
            QuestManager.Instance.OnQuestUpdated += HandleQuestUpdated;
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

            RefreshLockState(snapImmediately: false);
        }

        /// <summary>
        /// Recomputes whether the portal is locked and updates the Animator accordingly.
        /// </summary>
        /// <param name="snapImmediately">
        /// If true, jumps directly into the target state instead of transitioning, but only
        /// when the Animator isn't already in that state, to avoid restarting the loop unnecessarily.
        /// </param>
        private void RefreshLockState(bool snapImmediately)
        {
            isLocked = !string.IsNullOrEmpty(requiredQuestID) &&
                       (QuestManager.Instance == null || !QuestManager.Instance.GetQuestState(requiredQuestID).IsTurnedIn);

            if (portalAnimator == null)
            {
                return;
            }

            portalAnimator.SetBool(IsLockedHash, isLocked);

            if (!snapImmediately)
            {
                return;
            }

            int targetStateHash = isLocked ? PortalLockedStateHash : PortalUnlockedStateHash;
            AnimatorStateInfo currentState = portalAnimator.GetCurrentAnimatorStateInfo(0);
            if (currentState.shortNameHash != targetStateHash)
            {
                portalAnimator.Play(targetStateHash, 0, 0f);
                portalAnimator.Update(0f);
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

            // Convert mouse screen position to game world coordinates
            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
            Vector2 mouseWorldPosition = Camera.main.ScreenToWorldPoint(mouseScreenPosition);

            // Only proceed if the mouse is actually hovering over this portal's collider
            Collider2D portalCollider = GetComponent<Collider2D>();
            if (portalCollider != null && !portalCollider.OverlapPoint(mouseWorldPosition))
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
            ReportObjectiveProgress();
        }

        private void ReportObjectiveProgress()
        {
            if (QuestManager.Instance == null || objectiveIDs == null)
            {
                return;
            }

            foreach (string id in objectiveIDs)
            {
                if (!string.IsNullOrWhiteSpace(id))
                {
                    QuestManager.Instance.RegisterObjectiveProgress(id);
                }
            }
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
