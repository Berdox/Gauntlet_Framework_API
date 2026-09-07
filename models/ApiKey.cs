namespace gauntlet_framework_api.models {
    public class ApiKey {
        public int Id { get; set; }
        public required string KeyHash { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<RunRecord> RunRecords { get; set; } = new List<RunRecord>();
    }
}
