namespace BizFlow.Application.Common.Models
{
    /// <summary>
    /// Server-side snapshot of the Firestore gate doc used by security rules (client cannot read it).
    /// </summary>
    public class FirestoreGateDebugStatus
    {
        public string ProjectId { get; set; } = string.Empty;

        /// <summary>system_config/firestore_gate exists.</summary>
        public bool GateDocExists { get; set; }

        public bool? AllowClientRead { get; set; }
        public bool? AllowClientReadNoAuth { get; set; }

        /// <summary>Mirrors rule helper isClientReadEnabled().</summary>
        public bool IsClientReadEnabled { get; set; }

        public string? Reason { get; set; }
    }
}
