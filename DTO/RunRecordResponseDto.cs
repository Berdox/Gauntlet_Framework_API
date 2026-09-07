namespace gauntlet_framework_api.DTO {
    public record RunRecordResponseDto(
        int Id,
        string PlayerUniqueID,
        string PlayerName,
        float RecordTime,
        string MapName,
        string EventName,
        DateTime DateTime
    );
}
