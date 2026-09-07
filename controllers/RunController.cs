using gauntlet_framework_api.authentication;
using gauntlet_framework_api.database;
using gauntlet_framework_api.DTO;
using gauntlet_framework_api.models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace gauntlet_framework_api.controllers {

    [ApiController]
    [Route("api/leaderboard")]
    public class RunController(AppDbContext db, IConfiguration config) : ControllerBase {

        [HttpGet]
        public async Task<IActionResult> GetLeaderboard([FromQuery] string mapName, [FromQuery] string eventName = "Main", [FromQuery] int limit = 50) {
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
                    r.EventName,
                    r.DateTime
                ))
                .ToListAsync();

            return Ok(records);
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = ApiKeyAuthenticationOptions.DefaultScheme)]
        public async Task<IActionResult> PostRecord([FromBody] CreateRunRecordDto dto) {
            var apiKeyClaim = User.FindFirst("ApiKeyId")?.Value;
            if (string.IsNullOrEmpty(apiKeyClaim) || !int.TryParse(apiKeyClaim, out int apiKeyId)) {
                return Unauthorized("Invalid API Key identity.");
            }

            var record = new RunRecord {
                PlayerUniqueID = dto.PlayerUniqueID,
                PlayerName = dto.PlayerName,
                RecordTime = dto.RecordTime,
                MapName = dto.MapName,
                EventName = dto.EventName,
                ApiKeyId = apiKeyId,
                DateTime = DateTime.UtcNow
            };

            db.RunRecords.Add(record);
            await db.SaveChangesAsync();

            var response = new RunRecordResponseDto(
                record.Id,
                record.PlayerUniqueID,
                record.PlayerName,
                record.RecordTime,
                record.MapName,
                record.EventName,
                record.DateTime
            );

            return CreatedAtAction(
                nameof(GetLeaderboard),
                new { mapName = record.MapName, eventName = record.EventName },
                response
            );
        }

        [HttpPost("keys/generate")]
        [AllowAnonymous]
        public async Task<IActionResult> GenerateApiKey([FromHeader(Name = "X-Admin-Secret")] string? adminSecret) {
            // Optional security check: Verify Master Admin Secret if defined in appsettings.json
            var configuredSecret = config["AdminSecret"];
            if (!string.IsNullOrEmpty(configuredSecret) && adminSecret != configuredSecret) {
                return Unauthorized("Invalid admin secret.");
            }

            // Generate a cryptographically secure raw string
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
                Note = "Save this key immediately. It is stored hashed and cannot be shown again.",
                createdAt = apiKeyEntity.CreatedAt
            });
        }
    }
}