namespace BarkAndDeliver.Delivery
{
    /// <summary>
    /// Represents the lifecycle of a single delivery quest.
    /// </summary>
    public enum DeliveryState
    {
        /// <summary>Quest exists but has not started yet.</summary>
        Idle,

        /// <summary>Dog has picked up the item; timer is running.</summary>
        PickedUp,

        /// <summary>Dog is carrying the item toward the destination.</summary>
        InTransit,

        /// <summary>Handoff animation is playing at the destination NPC.</summary>
        Delivering,

        /// <summary>Delivery was completed successfully.</summary>
        Completed,

        /// <summary>Timer expired or item was lost.</summary>
        Failed
    }
}
