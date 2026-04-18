using crud_app_backend.DTOs;
using crud_app_backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace crud_app_backend.Repositories
{
    public class WhatsAppSessionRepository : IWhatsAppSessionRepository
    {
        private readonly AppDbContext _db;
        private readonly ILogger<WhatsAppSessionRepository> _logger;

        public WhatsAppSessionRepository(
            AppDbContext db,
            ILogger<WhatsAppSessionRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        // ── GET ──────────────────────────────────────────────────────────

        public async Task<WhatsAppSession?> GetByPhoneAsync(
            string phone, CancellationToken ct = default)
        {
            return await _db.WhatsAppSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Phone == phone, ct);
        }

        // ── UPSERT ───────────────────────────────────────────────────────
        // When preserveComplaintMedia = true the repository reads the current
        // complaint_images / complaint_voices from the DB row and grafts them
        // onto the incoming TempData before saving, so a concurrent append is
        // never silently overwritten by a full-session state-machine write.

        public async Task UpsertAsync(
            WhatsAppSession session,
            string? rawMessage,
            bool preserveComplaintMedia = false,
            CancellationToken ct = default)
        {
            var strategy = _db.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database
                    .BeginTransactionAsync(ct);

                try
                {
                    // ── 1. Fetch existing row (tracked) ──────────────────
                    // When preserveComplaintMedia is true we use UPDLOCK so that
                    // concurrent upserts for the same phone are serialized at the
                    // DB level — the same guarantee AppendMediaAsync already provides.
                    //
                    // Without this, exec A could read existing.TempData = [imgA]
                    // (before B and C have appended) and then commit AFTER exec C
                    // has written [imgA,imgB,imgC], silently overwriting the full
                    // array with the stale one-image version.
                    //
                    // When preserveComplaintMedia is false (intentional reset, e.g.
                    // starting a new complaint) no lock is needed — the caller owns
                    // that write deliberately.
                    WhatsAppSession? existing;
                    if (preserveComplaintMedia)
                    {
                        existing = await _db.WhatsAppSessions
                            .FromSqlRaw(
                                "SELECT * FROM WhatsAppSessions WITH (UPDLOCK, ROWLOCK) WHERE Phone = {0}",
                                session.Phone)
                            .FirstOrDefaultAsync(ct);
                    }
                    else
                    {
                        existing = await _db.WhatsAppSessions
                            .FirstOrDefaultAsync(s => s.Phone == session.Phone, ct);
                    }

                    string fromStep;

                    if (existing == null)
                    {
                        // ── INSERT ────────────────────────────────────────
                        fromStep = "NEW";
                        session.CreatedAt = DateTime.UtcNow;
                        session.UpdatedAt = DateTime.UtcNow;
                        await _db.WhatsAppSessions.AddAsync(session, ct);

                        _logger.LogInformation(
                            "[WA-Session] INSERT phone={Phone} step={Step}",
                            session.Phone, session.CurrentStep);
                    }
                    else
                    {
                        // ── UPDATE ────────────────────────────────────────
                        fromStep = existing.CurrentStep;

                        // If the caller asked us to preserve the media arrays
                        // already in the DB, graft them onto the incoming TempData
                        // before we overwrite the row. This prevents a concurrent
                        // POST-Session-to-DB from clobbering atomically-appended
                        // complaint_images / complaint_voices.
                        var finalTempData = session.TempData;
                        if (preserveComplaintMedia)
                        {
                            finalTempData = GraftMediaArrays(
                                existingTempData: existing.TempData,
                                incomingTempData: session.TempData);
                        }

                        existing.CurrentStep = session.CurrentStep;
                        existing.PreviousStep = session.PreviousStep;
                        existing.TempData = finalTempData;
                        existing.PendingReport = session.PendingReport;
                        existing.PendingShopReg = session.PendingShopReg;
                        existing.UpdatedAt = DateTime.UtcNow;

                        _logger.LogInformation(
                            "[WA-Session] UPDATE phone={Phone} {From}→{To} preserveMedia={Preserve}",
                            session.Phone, fromStep, session.CurrentStep, preserveComplaintMedia);
                    }

                    // ── 2. Write history row ──────────────────────────────
                    var historyRow = new WhatsAppSessionHistory
                    {
                        Phone = session.Phone,
                        FromStep = fromStep,
                        ToStep = session.CurrentStep,
                        RawMessage = rawMessage,
                        TempDataSnapshot = session.TempData,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _db.WhatsAppSessionHistories.AddAsync(historyRow, ct);

                    // ── 3. Save both rows in one round-trip ───────────────
                    await _db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync(ct);
                    _logger.LogError(ex,
                        "[WA-Session] Upsert FAILED for phone={Phone}", session.Phone);
                    throw;
                }
            });
        }

        // ── APPEND MEDIA (atomic) ────────────────────────────────────────
        // Uses UPDLOCK + ROWLOCK so that concurrent n8n executions for the
        // same phone are serialized at the DB level. This guarantees that
        // when a user sends 3 images from the gallery simultaneously, all
        // three messageIds end up in the complaint_images array.

        public async Task<AppendMediaResponseDto> AppendMediaAsync(
            string phone,
            string mediaType,   // "image" | "voice"
            string messageId,
            CancellationToken ct = default)
        {
            if (mediaType != "image" && mediaType != "voice")
                throw new ArgumentException(
                    "mediaType must be 'image' or 'voice'", nameof(mediaType));

            var strategy = _db.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                // Use READ COMMITTED + UPDLOCK so only one caller at a time
                // can hold the row lock for this phone number.
                await using var tx = await _db.Database
                    .BeginTransactionAsync(
                        System.Data.IsolationLevel.ReadCommitted, ct);

                try
                {
                    // SELECT ... WITH (UPDLOCK, ROWLOCK) — EF Core raw SQL.
                    // This is tracked (no AsNoTracking) so SaveChanges works.
                    var session = await _db.WhatsAppSessions
                        .FromSqlRaw(
                            "SELECT * FROM WhatsAppSessions WITH (UPDLOCK, ROWLOCK) WHERE Phone = {0}",
                            phone)
                        .FirstOrDefaultAsync(ct);

                    if (session == null)
                    {
                        await tx.RollbackAsync(ct);
                        throw new KeyNotFoundException(
                            $"No session found for phone {phone}. " +
                            "Make sure the session is created before media is attached.");
                    }

                    // Parse TempData JSON
                    var node = JsonNode.Parse(session.TempData ?? "{}") as JsonObject
                               ?? new JsonObject();

                    var arrayKey = mediaType == "image" ? "complaint_images" : "complaint_voices";
                    var arr = (node[arrayKey]?.AsArray() ?? new JsonArray()).ToList();

                    // Deduplicate — 360dialog occasionally re-delivers the same event
                    if (!arr.Any(x => x?.GetValue<string>() == messageId))
                        arr.Add(JsonValue.Create(messageId)!);

                    // Write updated array back
                    var newArr = new JsonArray();
                    foreach (var item in arr)
                        newArr.Add(item?.GetValue<string>() is string s
                            ? JsonValue.Create(s) : null);

                    node[arrayKey] = newArr;
                    session.TempData = node.ToJsonString();
                    session.UpdatedAt = DateTime.UtcNow;

                    await _db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);

                    // Build return arrays from the now-saved node
                    List<string> Extract(string key) =>
                        (node[key]?.AsArray() ?? new JsonArray())
                        .Select(x => x?.GetValue<string>() ?? "")
                        .Where(x => x.Length > 0)
                        .ToList();

                    _logger.LogInformation(
                        "[WA-Session] AppendMedia phone={Phone} type={Type} msgId={MsgId} " +
                        "images={ImgCount} voices={VoiceCount}",
                        phone, mediaType, messageId,
                        Extract("complaint_images").Count,
                        Extract("complaint_voices").Count);

                    return new AppendMediaResponseDto
                    {
                        Phone = phone,
                        ComplaintImages = Extract("complaint_images"),
                        ComplaintVoices = Extract("complaint_voices")
                    };
                }
                catch (Exception ex) when (ex is not KeyNotFoundException)
                {
                    await tx.RollbackAsync(ct);
                    _logger.LogError(ex,
                        "[WA-Session] AppendMedia FAILED phone={Phone} type={Type}",
                        phone, mediaType);
                    throw;
                }
            });
        }

        // ── DELETE ───────────────────────────────────────────────────────

        public async Task<bool> DeleteAsync(
            string phone, CancellationToken ct = default)
        {
            var session = await _db.WhatsAppSessions
                .FirstOrDefaultAsync(s => s.Phone == phone, ct);

            if (session == null) return false;

            _db.WhatsAppSessions.Remove(session);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "[WA-Session] DELETED phone={Phone}", phone);
            return true;
        }

        // ── HISTORY ──────────────────────────────────────────────────────

        public async Task<List<WhatsAppSessionHistory>> GetHistoryAsync(
            string phone, int limit = 20, CancellationToken ct = default)
        {
            return await _db.WhatsAppSessionHistories
                .AsNoTracking()
                .Where(h => h.Phone == phone)
                .OrderByDescending(h => h.CreatedAt)
                .Take(limit)
                .ToListAsync(ct);
        }

        // ── Private helpers ───────────────────────────────────────────────

        /// <summary>
        /// Takes the complaint_images and complaint_voices arrays from
        /// <paramref name="existingTempData"/> and writes them into
        /// <paramref name="incomingTempData"/>, returning the merged JSON.
        /// All other keys come from the incoming payload.
        /// </summary>
        private static string GraftMediaArrays(
            string? existingTempData, string? incomingTempData)
        {
            var existing = JsonNode.Parse(existingTempData ?? "{}") as JsonObject
                           ?? new JsonObject();
            var incoming = JsonNode.Parse(incomingTempData ?? "{}") as JsonObject
                           ?? new JsonObject();

            foreach (var key in new[] { "complaint_images", "complaint_voices" })
            {
                var existingArr = existing[key]?.AsArray();
                if (existingArr is not null)
                    incoming[key] = existingArr.DeepClone();
                // If the key doesn't exist in DB yet, leave incoming value as-is
            }

            return incoming.ToJsonString();
        }
    }
}
