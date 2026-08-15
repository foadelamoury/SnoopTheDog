using UnityEngine;
using System;

namespace BarkAndDeliver.Delivery
{
    /// <summary>
    /// Abstraction for NPC handoff logic.
    /// </summary>
    public interface IHandoffHandler
    {
        bool IsHandoffInProgress { get; }

        /// <summary>
        /// Sequence where the NPC hands an item to the dog.
        /// </summary>
        /// <param name="dogHolder">The transform where the item should end up.</param>
        /// <param name="itemToHandOff">The item being handed over.</param>
        /// <param name="onComplete">Callback when animation finishes.</param>
        void StartGiveToDog(Transform dogHolder, Transform itemToHandOff, Action onComplete);

        /// <summary>
        /// Sequence where the NPC receives an item from the dog.
        /// </summary>
        /// <param name="droppedItem">The item the dog dropped.</param>
        /// <param name="onComplete">Callback when animation finishes.</param>
        void StartReceiveFromDog(Transform droppedItem, Action onComplete);
    }
}
