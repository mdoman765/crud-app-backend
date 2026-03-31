using crud_app_backend.DTOs;

namespace crud_app_backend.Services
{
    public interface IWhatsAppMessageService
    {
        /// <summary>
        /// Save an incoming plain-text WhatsApp message.
        /// Idempotent — returns the existing row if MessageId already processed.
        /// </summary>
        Task<WhatsAppMessageReceivedDto> HandleTextAsync(
            IncomingTextMessageDto dto,
            CancellationToken ct = default);

        /// <summary>
        /// Save an incoming voice note.
        /// Writes the audio file to wwwroot/wa-media/audio/, then stores the URL.
        /// Idempotent — returns the existing row if MessageId already processed.
        /// </summary>
        Task<WhatsAppMessageReceivedDto> HandleVoiceAsync(
            IncomingVoiceMessageDto dto,
            CancellationToken ct = default);

        /// <summary>
        /// Save an incoming image.
        /// Writes the image to wwwroot/wa-media/images/, then stores the URL.
        /// Idempotent — returns the existing row if MessageId already processed.
        /// </summary>
        Task<WhatsAppMessageReceivedDto> HandleImageAsync(
            IncomingImageMessageDto dto,
            CancellationToken ct = default);

        /// <summary>
        /// Returns the last <paramref name="limit"/> messages for a phone number, newest-first.
        /// </summary>
        Task<List<WhatsAppMessageListDto>> GetByPhoneAsync(
            string phone, int limit = 20, CancellationToken ct = default);

        /// <summary>
        /// Returns the most recent <paramref name="limit"/> messages across all senders.
        /// </summary>
        Task<List<WhatsAppMessageListDto>> GetRecentAsync(
            int limit = 20, CancellationToken ct = default);
    }
}
