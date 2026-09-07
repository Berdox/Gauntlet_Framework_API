using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using gauntlet_framework_api.database;
using gauntlet_framework_api.models;
using gauntlet_framework_api.authentication;


namespace gauntlet_framework_api.controllers {

    [ApiController]
    [Route("api/leaderboard")]
    public class RunController(AppDbContext db) : ControllerBase {
        [HttpGet]
        public async Task<IActionResult> GetLeaderboard([FromQuery] string mapName, [FromQuery] string eventName = "Main", [FromQuery] int limit = 50) {
            var records = await db.RunRecords
                .Where(r => r.MapName.ToLower() == mapName.ToLower() && r.EventName.ToLower() == eventName.ToLower())
                .OrderBy(r => r.RecordTime)
                .Take(limit)
                .Select(r => new {
                    r.Id,
                    r.PlayerUniqueID,
                    r.PlayerName,
                    r.RecordTime,
                    r.MapName,
                    r.EventName,
                    r.DateTime
                })
                .ToListAsync();

            return Ok(records);
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = ApiKeyAuthenticationOptions.DefaultScheme)]
        public async Task<IActionResult> PostRecord([FromBody] RunRecord record) {
            // 1. Extract the authenticated ApiKey entity attached by your ApiKeyAuthenticationHandler
            var apiKeyClaim = User.FindFirst("ApiKeyId")?.Value;
            if (string.IsNullOrEmpty(apiKeyClaim) || !int.TryParse(apiKeyClaim, out int apiKeyId)) {
                return Unauthorized("Invalid API Key identity.");
            }

            // 2. Assign the Foreign Key ID and timestamp
            record.ApiKeyId = apiKeyId;
            record.DateTime = DateTime.UtcNow;

            db.RunRecords.Add(record);
            await db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLeaderboard), new { mapName = record.MapName, eventName = record.EventName }, record);
        }

        [HttpPost("keys/generate")]
        public async Task<IActionResult> GenerateKey() {
            var rawKey = ApiKeyHelper.GenerateApiKey();
            var hashedKey = ApiKeyHelper.HashApiKey(rawKey);

            var apiKey = new ApiKey {
                KeyHash = hashedKey,
                IsActive = true
            };

            db.ApiKeys.Add(apiKey);
            await db.SaveChangesAsync();

            return Ok(new {
                KeyId = apiKey.Id,
                ApiKey = rawKey,
                Note = "Save this key immediately. It is stored hashed and cannot be shown again."
            });
        }
    }
}
