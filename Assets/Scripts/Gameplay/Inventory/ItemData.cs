using UnityEngine;

namespace IdleOnDemo.Gameplay.Inventory
{
    /// <summary>
    /// Defines immutable item metadata used by drops and inventory entries.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemData", menuName = "IdleOnDemo/Inventory/Item Data")]
    public sealed class ItemData : ScriptableObject
    {
        [SerializeField] private string itemID;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField] private ItemPickup dropPrefab;
        [SerializeField] private bool isStackable = true;

        public string ItemID => itemID;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public ItemPickup DropPrefab => dropPrefab;
        public bool IsStackable => isStackable;
    }
}
