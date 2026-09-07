namespace gauntlet_framework_api.DTO {
    public record CreateRunRecordDto(
        string PlayerUniqueID,
        string PlayerName,
        float RecordTime,
        string MapName,
        string EventName
    );
}
