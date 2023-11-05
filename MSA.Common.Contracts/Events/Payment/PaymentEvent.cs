namespace MSA.Common.Contracts.Events.Payment
{
    public record PaymentProcessedSucceeded
    {
        public Guid OrderId { get; init; }
        public Guid PaymentId { get; init; }
    }

    public record PaymentProcessedFailed
    {
        public Guid OrderId { get; init; }
        public Guid PaymentId { get; init; }
        public string Reason { get; set; }
    }
}