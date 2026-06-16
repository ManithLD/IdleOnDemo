using UnityEngine;

namespace IdleOnDemo.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for objects that can be interacted with by another transform.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Gets a value indicating whether the object is currently available for interaction.
        /// </summary>
        /// <value><c>true</c> when an interactor may call <see cref="Interact"/>.</value>
        bool CanInteract { get; }

        /// <summary>
        /// Performs the object's interaction behavior for the specified interactor.
        /// </summary>
        /// <param name="interactor">The transform initiating the interaction.</param>
        void Interact(Transform interactor);
    }
}
