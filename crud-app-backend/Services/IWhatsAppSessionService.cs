using crud_app_backend.DTOs;
using crud_app_backend.Models;

namespace crud_app_backend.Services
{
    public interface IWhatsAppSessionService
    {
        /// <summary>
        /// Returns the session for <paramref name="phone"/>.
        /// Never returns null — new phones receive a default INIT response
        /// with <see cref="SessionResponse.IsNew"/> = true.
        /// </summary>
        Task<SessionResponse> GetSessionAsync(
            string phone,
            CancellationToken ct = default);

        /// <summary>
        /// Insert or update the session row and write one history entry.
        /// Returns a success/fail envelope consumed by the controller.
        /// </summary>
        Task<ApiResponseDto<object>> UpsertSessionAsync(
            UpsertSessionRequestDto req,
            CancellationToken ct = default);

        /// <summary>
        /// Atomically appends one media messageId to complaint_images or
        /// complaint_voices in TempData using a DB-level row lock, preventing
        /// concurrent n8n executions from overwriting each other.
        ///
        /// Returns the full updated arrays so n8n can build updatedSession
        /// from fresh data.
        /// </summary>
        Task<AppendMediaResponseDto> AppendMediaAsync(
            AppendMediaRequestDto req,
            CancellationToken ct = default);

        /// <summary>
        /// Hard-delete a session and all its history rows (CASCADE).
        /// Returns Success = false when the phone number is not found.
        /// </summary>
        Task<ApiResponseDto<object>> DeleteSessionAsync(
            string phone,
            CancellationToken ct = default);

        /// <summary>
        /// Returns the last <paramref name="limit"/> state-transition rows
        /// for <paramref name="phone"/>, ordered newest-first.
        /// </summary>
        Task<List<WhatsAppSessionHistory>> GetHistoryAsync(
            string phone,
            int limit = 20,
            CancellationToken ct = default);
    }
}
