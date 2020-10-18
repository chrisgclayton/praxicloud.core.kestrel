namespace praxicloud.core.kestrel.tests
{
    #region Using Clauses
    using praxicloud.core.containers;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;
    #endregion

    /// <summary>
    /// A type used for basic probe tests
    /// </summary>
    [ExcludeFromCodeCoverage]
    public sealed class ProbeInvocationCounter : IHealthCheck, IAvailabilityCheck
    {
        #region Variables
        /// <summary>
        /// The number of times the availability handler has been invoked
        /// </summary>
        private long _availabilityCount;

        /// <summary>
        /// The number of times the health handler has been invoked
        /// </summary>
        private long _healthCount;
        #endregion
        #region Properties
        /// <summary>
        /// True if the availability results should return success
        /// </summary>
        public bool IsAvailable { get; set; } = true;

        /// <summary>
        /// True if the health results should return success
        /// </summary>
        public bool IsHealthy { get; set; } = true;

        /// <summary>
        /// The number of times the availability handler has been invoked
        /// </summary>
        public long AvailabiltyCount => _availabilityCount;

        /// <summary>
        /// The number of times the health handler has been invoked
        /// </summary>
        public long HealthCount => _healthCount;
        #endregion
        #region Methods
        /// <inheritdoc />
        public Task<bool> IsAvailableAsync()
        {
            Interlocked.Increment(ref _availabilityCount);

            return Task.FromResult(IsAvailable);
        }

        /// <inheritdoc />
        public Task<bool> IsHealthyAsync()
        {
            Interlocked.Increment(ref _healthCount);

            return Task.FromResult(IsHealthy);
        }
        #endregion
    }
}
