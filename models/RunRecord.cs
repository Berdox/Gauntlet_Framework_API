using System.Diagnostics.Contracts;

namespace gauntlet_framework_api.models {
    public class RunRecord {
        public int Id { get; set; }
        public required string PlayerUniqueID { get; set; }
        public required string PlayerName { get; set; }
        public required float RecordTime { get; set; }
        public required string MapName { get; set; }
        public string RouteName { get; set; } = "Main";
        public string EventName { get; set; } = "None";
        public DateTime DateTime { get; set; } = DateTime.UtcNow;
        public int ApiKeyId { get; set; }
    }
}
