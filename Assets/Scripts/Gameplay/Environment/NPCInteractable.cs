using System.Collections.Generic;
using IdleOnDemo.Gameplay.Player;
using IdleOnDemo.Gameplay.Quests;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IdleOnDemo.Gameplay.Environment
{
    /// <summary>
    /// Handles click-based NPC quest acceptance and turn-ins while the player is inside trigger range.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class NPCInteractable : MonoBehaviour
    {
        [SerializeField] private QuestData targetQuest;
        [SerializeField] private List<string> objectiveIDs = new List<string>();

        private Collider2D npcCollider;
        private bool isPlayerInRange;
        private int lastInteractionFrame = -1;

        private void Awake()
        {
            npcCollider = GetComponent<Collider2D>();
        }

        private void Update()
        {
            if (WasClickedByInputFallback())
            {
                TryInteract();
            }
        }

        private void OnMouseDown()
        {
            TryInteract();
        }

        private void TryInteract()
        {
            if (lastInteractionFrame == Time.frameCount || !isPlayerInRange)
            {
                return;
            }

            lastInteractionFrame = Time.frameCount;
            if (objectiveIDs != null && objectiveIDs.Count > 0)
            {
                ReportObjectiveProgress();
                return;
            }

            QuestManager questManager = QuestManager.Instance;
            if (questManager == null || targetQuest == null)
            {
                return;
            }

            QuestRuntimeState state = questManager.GetQuestState(targetQuest.QuestID);
            if (state == null)
            {
                return;
            }

            if (!state.IsAccepted)
            {
                questManager.AcceptQuest(targetQuest);
                return;
            }

            if (state.IsAccepted && !state.IsCompleted)
            {
                Debug.Log($"Quest in progress: {state.CurrentProgress}/{targetQuest.RequiredAmount}");
                return;
            }

            if (state.IsCompleted)
            {
                bool success = questManager.TurnInQuest(targetQuest.QuestID);
                if (success)
                {
                    Debug.Log($"Quest complete! Received {targetQuest.CoinReward} coins.");
                }
                else
                {
                    Debug.Log("Reward already claimed.");
                }
            }
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

        private bool WasClickedByInputFallback()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return false;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera == null || npcCollider == null)
            {
                return false;
            }

            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
            Vector3 screenPoint = new Vector3(
                mouseScreenPosition.x,
                mouseScreenPosition.y,
                Mathf.Abs(mainCamera.transform.position.z - transform.position.z));
            Vector2 mouseWorldPosition = mainCamera.ScreenToWorldPoint(screenPoint);
            return npcCollider.OverlapPoint(mouseWorldPosition);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsPlayerCollider(other))
            {
                isPlayerInRange = true;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (IsPlayerCollider(other))
            {
                isPlayerInRange = false;
            }
        }

        private bool IsPlayerCollider(Collider2D other)
        {
            return other.CompareTag("Player") || other.GetComponentInParent<PlayerController>() != null;
        }

        private void OnValidate()
        {
            Collider2D attachedCollider = GetComponent<Collider2D>();
            if (attachedCollider != null)
            {
                attachedCollider.isTrigger = true;
            }
        }
    }
}
