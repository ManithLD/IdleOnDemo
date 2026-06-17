using System;
using System.Reflection;
using UnityEngine;

namespace IdleOnDemo.Mechanics
{
    /// <summary>
    /// Rebinds scene-local camera bounds after a persistent camera enters a newly loaded scene.
    /// </summary>
    public sealed class SceneCameraInitializer : MonoBehaviour
    {
        private const string CameraBoundsName = "CameraBounds";
        private const string CinemachineConfiner2DTypeName = "Cinemachine.CinemachineConfiner2D";

        /// <summary>
        /// Finds the local camera bounds object and assigns it to any active Cinemachine 2D confiner.
        /// </summary>
        private void Start()
        {
            GameObject boundsObject = GameObject.Find(CameraBoundsName);
            if (boundsObject == null)
            {
                Debug.LogWarning($"SceneCameraInitializer could not find '{CameraBoundsName}'.");
                return;
            }

            PolygonCollider2D boundsCollider = boundsObject.GetComponent<PolygonCollider2D>();
            if (boundsCollider == null)
            {
                Debug.LogWarning($"SceneCameraInitializer found '{CameraBoundsName}', but it has no PolygonCollider2D.");
                return;
            }

            Component confiner = FindCinemachineConfiner2D();
            if (confiner == null)
            {
                return;
            }

            AssignBoundingShape(confiner, boundsCollider);
            InvalidateConfinerCache(confiner);
        }

        /// <summary>
        /// Searches loaded behaviours for a CinemachineConfiner2D component without requiring a compile-time Cinemachine reference.
        /// </summary>
        /// <returns>The first active CinemachineConfiner2D component found, or <c>null</c> when none exists.</returns>
        private static Component FindCinemachineConfiner2D()
        {
            Component[] components = FindObjectsByType<Component>(FindObjectsInactive.Exclude);
            foreach (Component component in components)
            {
                if (component == null)
                {
                    continue;
                }

                Type componentType = component.GetType();
                if (componentType.FullName == CinemachineConfiner2DTypeName || componentType.Name == "CinemachineConfiner2D")
                {
                    return component;
                }
            }

            return null;
        }

        /// <summary>
        /// Assigns the scene bounds collider to the confiner's bounding shape field or property.
        /// </summary>
        /// <param name="confiner">The CinemachineConfiner2D component to configure.</param>
        /// <param name="boundsCollider">The scene-local polygon bounds collider.</param>
        private static void AssignBoundingShape(Component confiner, PolygonCollider2D boundsCollider)
        {
            Type confinerType = confiner.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            FieldInfo field = confinerType.GetField("m_BoundingShape2D", flags);
            if (field != null && typeof(Collider2D).IsAssignableFrom(field.FieldType))
            {
                field.SetValue(confiner, boundsCollider);
                return;
            }

            PropertyInfo property = confinerType.GetProperty("BoundingShape2D", flags);
            if (property != null && property.CanWrite && typeof(Collider2D).IsAssignableFrom(property.PropertyType))
            {
                property.SetValue(confiner, boundsCollider);
            }
        }

        /// <summary>
        /// Invalidates the confiner cache after rebinding bounds so Cinemachine recalculates the confinement shape.
        /// </summary>
        /// <param name="confiner">The CinemachineConfiner2D component to refresh.</param>
        private static void InvalidateConfinerCache(Component confiner)
        {
            MethodInfo method = confiner.GetType().GetMethod(
                "InvalidateCache",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            method?.Invoke(confiner, null);
        }
    }
}
