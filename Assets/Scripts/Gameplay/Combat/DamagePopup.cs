using System.Collections;
using TMPro;
using UnityEngine;

namespace IdleOnDemo.Gameplay.Combat
{
    /// <summary>
    /// Displays a single floating combat damage value, then fades itself out.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class DamagePopup : MonoBehaviour
    {
        private const float Lifetime = 2f;
        private const float RiseDistance = 48f;

        [SerializeField] private TextMeshProUGUI damageText;

        /// <summary>
        /// Assigns the displayed damage value and matching severity color.
        /// </summary>
        /// <param name="damageValue">The damage amount to display.</param>
        public void Initialize(int damageValue)
        {
            damageText ??= GetComponent<TextMeshProUGUI>();
            if (damageText == null)
            {
                return;
            }

            damageText.text = damageValue.ToString();
            damageText.color = GetColorForDamage(damageValue);
        }

        /// <summary>
        /// Caches the TMP reference before external initialization occurs.
        /// </summary>
        private void Awake()
        {
            damageText ??= GetComponent<TextMeshProUGUI>();
        }

        /// <summary>
        /// Starts the upward drift and fade animation.
        /// </summary>
        private IEnumerator Start()
        {
            if (damageText == null)
            {
                Destroy(gameObject);
                yield break;
            }

            Vector3 startPosition = transform.position;
            Vector3 endPosition = startPosition + Vector3.up * RiseDistance;
            Color startColor = damageText.color;
            float elapsed = 0f;

            while (elapsed < Lifetime)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / Lifetime);

                transform.position = Vector3.Lerp(startPosition, endPosition, normalizedTime);
                Color fadedColor = startColor;
                fadedColor.a = Mathf.Lerp(startColor.a, 0f, normalizedTime);
                damageText.color = fadedColor;

                yield return null;
            }

            Destroy(gameObject);
        }

        /// <summary>
        /// Restores cached references while editing the prefab.
        /// </summary>
        private void OnValidate()
        {
            damageText ??= GetComponent<TextMeshProUGUI>();
        }

        /// <summary>
        /// Returns the popup color for the configured damage bands.
        /// </summary>
        /// <param name="damageValue">The damage amount being displayed.</param>
        /// <returns>The color assigned to the damage popup text.</returns>
        private Color GetColorForDamage(int damageValue)
        {
            if (damageValue <= 10)
            {
                return Color.green;
            }

            if (damageValue <= 22)
            {
                return Color.yellow;
            }

            if (damageValue <= 35)
            {
                return new Color(1f, 0.5f, 0f);
            }

            return Color.red;
        }
    }
}
