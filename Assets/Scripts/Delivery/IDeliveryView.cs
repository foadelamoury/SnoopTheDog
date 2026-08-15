namespace BarkAndDeliver.Delivery
{
    /// <summary>
    /// Abstraction for any view (MiniMap, Timer, UI) that responds to delivery events.
    /// Follows Dependency Inversion Principle.
    /// </summary>
    public interface IDeliveryView
    {
        void OnDeliveryStarted(DeliveryModel model);
        void OnDeliveryCompleted(DeliveryModel model);
        void OnDeliveryFailed(DeliveryModel model);
        
        /// <summary>
        /// Called every frame while the delivery is active.
        /// </summary>
        /// <param name="remainingTime">Time left in seconds.</param>
        /// <param name="normalized">0.0 (start) to 1.0 (expired).</param>
        void OnTimerTick(float remainingTime, float normalized);
    }
}
