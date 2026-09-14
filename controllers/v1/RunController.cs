using gauntlet_framework_api.authentication;
using gauntlet_framework_api.database;
using gauntlet_framework_api.DTO;
using gauntlet_framework_api.models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Asp.Versioning;

namespace gauntlet_framework_api.controllers.v1 {

    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/leaderboard")]
    public class RunController(AppDbContext db) : ControllerBase {
        [HttpGet]
        public async Task<IActionResult> GetLeaderboard([FromQuery] string mapName, [FromQuery] string eventName = "None", [FromQuery] int limit = 50) {
            var records = await db.RunRecords
                .Where(r => r.MapName.ToLower() == mapName.ToLower() && r.EventName.ToLower() == eventName.ToLower())
                .OrderBy(r => r.RecordTime)
                .Take(limit)
                .Select(r => new RunRecordResponseDto(
                    r.Id,
                    r.PlayerUniqueID,
                    r.PlayerName,
                    r.RecordTime,
                    r.MapName,
                    r.RouteName,
                    r.EventName,
                    r.DateTime
                ))
                .ToListAsync();

            return Ok(records);
        }

        [HttpGet("routes")]
        public async Task<IActionResult> GetMapRoutes([FromQuery] string mapName) {
            if (string.IsNullOrWhiteSpace(mapName))
                return BadRequest("mapName is required.");

            var routes = await db.RunRecords
                .AsNoTracking()
                .Where(r => EF.Functions.Like(r.MapName, mapName))
                .Select(r => r.RouteName)
                .Distinct()
                .ToListAsync();

            return Ok(routes);
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = ApiKeyAuthenticationOptions.DefaultScheme)]
        public async Task<IActionResult> PostRecord([FromBody] CreateRunRecordDto dto) {
            var apiKeyClaim = User.FindFirst("ApiKeyId")?.Value;
            if (string.IsNullOrEmpty(apiKeyClaim) || !int.TryParse(apiKeyClaim, out int apiKeyId)) {
                return Unauthorized("Invalid API Key identity.");
            }

            var existingRecord = await db.RunRecords
                .FirstOrDefaultAsync(r =>
                    r.PlayerUniqueID == dto.PlayerUniqueID &&
                    r.MapName.ToLower() == dto.MapName.ToLower() &&
                    r.RouteName.ToLower() == dto.RouteName.ToLower() &&
                    r.EventName.ToLower() == dto.EventName.ToLower());

            RunRecord targetRecord;

            if (existingRecord != null) {
                if (dto.RecordTime >= existingRecord.RecordTime) {
                    return Ok(new RunRecordResponseDto(
                        existingRecord.Id,
                        existingRecord.PlayerUniqueID,
                        existingRecord.PlayerName,
                        existingRecord.RecordTime,
                        existingRecord.MapName,
                        existingRecord.RouteName,
                        existingRecord.EventName,
                        existingRecord.DateTime
                    ));
                }

                existingRecord.RecordTime = dto.RecordTime;
                existingRecord.PlayerName = dto.PlayerName;
                existingRecord.ApiKeyId = apiKeyId;
                existingRecord.DateTime = DateTime.UtcNow;

                targetRecord = existingRecord;
            }
            else {
                targetRecord = new RunRecord {
                    PlayerUniqueID = dto.PlayerUniqueID,
                    PlayerName = dto.PlayerName,
                    RecordTime = dto.RecordTime,
                    MapName = dto.MapName,
                    RouteName = dto.RouteName,
                    EventName = dto.EventName,
                    ApiKeyId = apiKeyId,
                    DateTime = DateTime.UtcNow
                };

                db.RunRecords.Add(targetRecord);
            }

            await db.SaveChangesAsync();

            var response = new RunRecordResponseDto(
                targetRecord.Id,
                targetRecord.PlayerUniqueID,
                targetRecord.PlayerName,
                targetRecord.RecordTime,
                targetRecord.MapName,
                targetRecord.RouteName,
                targetRecord.EventName,
                targetRecord.DateTime
            );

            return CreatedAtAction(
                nameof(GetLeaderboard),
                new { version = "1", mapName = targetRecord.MapName, routeName = targetRecord.RouteName, eventName = targetRecord.EventName },
                response
            );
        }

        [HttpPost("keys/generate")]
        [AllowAnonymous]
        public async Task<IActionResult> GenerateApiKey() {
            string rawApiKey = ApiKeyHelper.GenerateApiKey();
            string keyHash = ApiKeyHelper.HashApiKey(rawApiKey);

            var apiKeyEntity = new ApiKey {
                KeyHash = keyHash,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            db.ApiKeys.Add(apiKeyEntity);
            await db.SaveChangesAsync();

            return Ok(new {
                id = apiKeyEntity.Id,
                apiKey = rawApiKey,
                createdAt = apiKeyEntity.CreatedAt
            });
        }
    }
}