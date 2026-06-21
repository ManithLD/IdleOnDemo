using System.Collections;
using TMPro;
using UnityEngine;

namespace IdleOnDemo.UI
{
    /// <summary>
    /// Displays one loot notification line, then fades and destroys itself.
    /// </summary>
    public sealed class LootNotification : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI notificationText;
        [SerializeField] private float visibleDuration = 2f;
        [SerializeField] private float fadeDuration = 0.5f;

        private Coroutine lifecycleRoutine;

        /// <summary>
        /// Sets the notification text and starts its timed fade-out lifecycle.
        /// </summary>
        /// <param name="message">The already-formatted notification message to display.</param>
        public void Initialize(string message)
        {
            notificationText ??= GetComponent<TextMeshProUGUI>();
            if (notificationText != null)
            {
                notificationText.text = message;
                SetTextAlpha(1f);
            }

            if (lifecycleRoutine != null)
            {
                StopCoroutine(lifecycleRoutine);
            }

            lifecycleRoutine = StartCoroutine(LifecycleRoutine());
        }

        /// <summary>
        /// Caches the text reference before initialization when the prefab reference is not assigned.
        /// </summary>
        private void Awake()
        {
            notificationText ??= GetComponent<TextMeshProUGUI>();
        }

        /// <summary>
        /// Keeps the notification text reference cached while editing the prefab.
        /// </summary>
        private void OnValidate()
        {
            notificationText ??= GetComponent<TextMeshProUGUI>();
            visibleDuration = Mathf.Max(0f, visibleDuration);
            fadeDuration = Mathf.Max(0f, fadeDuration);
        }

        /// <summary>
        /// Waits while visible, fades text alpha to zero, then destroys this notification object.
        /// </summary>
        /// <returns>An IEnumerator used by Unity's coroutine scheduler.</returns>
        private IEnumerator LifecycleRoutine()
        {
            if (visibleDuration > 0f)
            {
                yield return new WaitForSeconds(visibleDuration);
            }

            if (notificationText != null && fadeDuration > 0f)
            {
                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    SetTextAlpha(Mathf.Lerp(1f, 0f, Mathf.Clamp01(elapsed / fadeDuration)));
                    yield return null;
                }
            }

            SetTextAlpha(0f);
            Destroy(gameObject);
        }

        /// <summary>
        /// Applies an alpha value to the configured TextMeshProUGUI text color.
        /// </summary>
        /// <param name="alpha">The target alpha value in the range 0 to 1.</param>
        private void SetTextAlpha(float alpha)
        {
            if (notificationText == null)
            {
                return;
            }

            Color color = notificationText.color;
            color.a = alpha;
            notificationText.color = color;
        }
    }
}
