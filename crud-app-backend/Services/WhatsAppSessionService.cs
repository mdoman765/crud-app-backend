using crud_app_backend.DTOs;
using crud_app_backend.Models;
using crud_app_backend.Repositories;

namespace crud_app_backend.Services
{
    public class WhatsAppSessionService : IWhatsAppSessionService
    {
        private readonly IWhatsAppSessionRepository _repo;
        private readonly ILogger<WhatsAppSessionService> _logger;

        public WhatsAppSessionService(
            IWhatsAppSessionRepository repo,
            ILogger<WhatsAppSessionService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────
        // GET SESSION
        // ─────────────────────────────────────────────────────────────────
        public async Task<SessionResponse> GetSessionAsync(
            string phone,
            CancellationToken ct = default)
        {
            var session = await _repo.GetByPhoneAsync(phone, ct);

            if (session == null)
            {
                _logger.LogInformation(
                    "[WA-Service] New user — phone={Phone}", phone);

                return new SessionResponse
                {
                    Phone = phone,
                    CurrentStep = "INIT",
                    PreviousStep = "INIT",
                    TempData = "{}",
                    PendingReport = false,
                    PendingShopReg = false,
                    UpdatedAt = DateTime.UtcNow,
                    IsNew = true
                };
            }

            _logger.LogDebug(
                "[WA-Service] Session loaded — phone={Phone} step={Step}",
                session.Phone, session.CurrentStep);

            return MapEntityToResponse(session, isNew: false);
        }

        // ─────────────────────────────────────────────────────────────────
        // UPSERT SESSION
        // Passes PreserveComplaintMedia through to the repository so the
        // full-session write from "Prepare Send" never clobbers atomically-
        // appended complaint_images / complaint_voices.
        // ─────────────────────────────────────────────────────────────────
        public async Task<ApiResponseDto<object>> UpsertSessionAsync(
            UpsertSessionRequestDto req,
            CancellationToken ct = default)
        {
            var entity = new WhatsAppSession
            {
                Phone = req.Phone.Trim(),
                CurrentStep = req.CurrentStep.Trim(),
                PreviousStep = req.PreviousStep.Trim(),
                TempData = string.IsNullOrWhiteSpace(req.TempData) ? "{}" : req.TempData,
                PendingReport = req.PendingReport,
                PendingShopReg = req.PendingShopReg,
            };

            await _repo.UpsertAsync(
                entity,
                req.RawMessage,
                preserveComplaintMedia: req.PreserveComplaintMedia,
                ct);

            _logger.LogInformation(
                "[WA-Service] Session upserted — phone={Phone} step={Step} preserveMedia={Preserve}",
                entity.Phone, entity.CurrentStep, req.PreserveComplaintMedia);

            return ApiResponseDto<object>.Ok(new
            {
                phone = entity.Phone,
                step = entity.CurrentStep
            });
        }

        // ─────────────────────────────────────────────────────────────────
        // APPEND MEDIA  (new)
        // ─────────────────────────────────────────────────────────────────
        public async Task<AppendMediaResponseDto> AppendMediaAsync(
            AppendMediaRequestDto req,
            CancellationToken ct = default)
        {
            var result = await _repo.AppendMediaAsync(
                req.Phone.Trim(),
                req.MediaType.ToLowerInvariant(),
                req.MessageId.Trim(),
                ct);

            _logger.LogInformation(
                "[WA-Service] Media appended — phone={Phone} type={Type} " +
                "images={ImgCount} voices={VoiceCount}",
                req.Phone, req.MediaType,
                result.ComplaintImages.Count,
                result.ComplaintVoices.Count);

            return result;
        }

        // ─────────────────────────────────────────────────────────────────
        // DELETE SESSION
        // ─────────────────────────────────────────────────────────────────
        public async Task<ApiResponseDto<object>> DeleteSessionAsync(
            string phone,
            CancellationToken ct = default)
        {
            var deleted = await _repo.DeleteAsync(phone.Trim(), ct);

            if (!deleted)
            {
                _logger.LogWarning(
                    "[WA-Service] Delete requested but no session found — phone={Phone}",
                    phone);

                return ApiResponseDto<object>.Fail(
                    $"No session found for phone: {phone}");
            }

            _logger.LogInformation(
                "[WA-Service] Session deleted — phone={Phone}", phone);

            return ApiResponseDto<object>.Ok(
                new { phone },
                "Session deleted successfully");
        }

        // ─────────────────────────────────────────────────────────────────
        // GET HISTORY
        // ─────────────────────────────────────────────────────────────────
        public async Task<List<WhatsAppSessionHistory>> GetHistoryAsync(
            string phone,
            int limit = 20,
            CancellationToken ct = default)
        {
            if (limit < 1 || limit > 200)
                limit = 20;

            var rows = await _repo.GetHistoryAsync(phone.Trim(), limit, ct);

            _logger.LogDebug(
                "[WA-Service] History loaded — phone={Phone} rows={Count}",
                phone, rows.Count);

            return rows.Select(h => new WhatsAppSessionHistory
            {
                Id = h.Id,
                Phone = h.Phone,
                FromStep = h.FromStep,
                ToStep = h.ToStep,
                RawMessage = h.RawMessage,
                CreatedAt = h.CreatedAt
            }).ToList();
        }

        // ─────────────────────────────────────────────────────────────────
        private static SessionResponse MapEntityToResponse(
            WhatsAppSession s, bool isNew) => new()
            {
                Phone = s.Phone,
                CurrentStep = s.CurrentStep,
                PreviousStep = s.PreviousStep,
                TempData = s.TempData,
                PendingReport = s.PendingReport,
                PendingShopReg = s.PendingShopReg,
                UpdatedAt = s.UpdatedAt,
                IsNew = isNew
            };
    }
}
