namespace gauntlet_framework_api.DTO {
    public record CreateRunRecordDto {
        public required string PlayerUniqueID { get; init; }
        public required string PlayerName { get; init; }
        public required float RecordTime { get; init; }
        public required string MapName { get; init; }

        private readonly string? _routeName;
        public string RouteName {
            get => string.IsNullOrWhiteSpace(_routeName) ? "Main" : _routeName;
            init => _routeName = value;
        }

        private readonly string? _eventName;
        public string EventName {
            get => string.IsNullOrWhiteSpace(_eventName) ? "None" : _eventName;
            init => _eventName = value;
        }
    }
}
