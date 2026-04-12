using System.Net.Http.Headers;
using System.Text.Json;
using crud_app_backend.DTOs;
using crud_app_backend.Models;
using crud_app_backend.Repositories;

namespace crud_app_backend.Services
{
    public class WhatsAppComplaintService : IWhatsAppComplaintService
    {
        private readonly IWhatsAppComplaintRepository _complaintRepo;
        private readonly IWhatsAppMessageRepository _messageRepo;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpClientFactory _httpFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<WhatsAppComplaintService> _logger;

        public WhatsAppComplaintService(
            IWhatsAppComplaintRepository complaintRepo,
            IWhatsAppMessageRepository messageRepo,
            IWebHostEnvironment env,
            IHttpClientFactory httpFactory,
            IConfiguration config,
            ILogger<WhatsAppComplaintService> logger)
        {
            _complaintRepo = complaintRepo;
            _messageRepo = messageRepo;
            _env = env;
            _httpFactory = httpFactory;
            _config = config;
            _logger = logger;
        }

        public async Task<SubmitComplaintResponseDto> SubmitAsync(
            SubmitComplaintRequestDto req, CancellationToken ct)
        {
            _logger.LogInformation(
                "[Complaint] Submit — type={T} staff={S} phone={P} voices={V} images={I}",
                req.ComplaintType, req.StaffId, req.WhatsappPhone,
                req.VoiceMessageIds.Count, req.ImageMessageIds.Count);

            // ── STEP 1: Load voice files from disk ────────────────────────────
            // Done before CRM call so files are ready to send.
            var voiceFiles = new List<(string FileName, string MimeType, byte[] Data)>();
            foreach (var msgId in req.VoiceMessageIds)
            {
                if (string.IsNullOrWhiteSpace(msgId)) continue;
                var waMsg = await _messageRepo.GetByMessageIdAsync(msgId, ct);
                var file = ReadFileDirect(msgId, "audio", waMsg?.MimeType ?? "audio/ogg");
                if (file is not null)
                {
                    voiceFiles.Add(file.Value);
                    _logger.LogInformation("[Complaint] Voice loaded: {F} ({B} bytes)",
                        file.Value.FileName, file.Value.Data.Length);
                }
                else
                {
                    _logger.LogWarning("[Complaint] Voice file not found on disk: msgId={Id}", msgId);
                }
            }

            // ── STEP 2: Load image files from disk ────────────────────────────
            var imageFiles = new List<(string FileName, string MimeType, byte[] Data)>();
            foreach (var msgId in req.ImageMessageIds)
            {
                if (string.IsNullOrWhiteSpace(msgId)) continue;
                var waMsg = await _messageRepo.GetByMessageIdAsync(msgId, ct);
                var file = ReadFileDirect(msgId, "images", waMsg?.MimeType ?? "image/jpeg");
                if (file is not null)
                {
                    imageFiles.Add(file.Value);
                    _logger.LogInformation("[Complaint] Image loaded: {F} ({B} bytes)",
                        file.Value.FileName, file.Value.Data.Length);
                }
                else
                {
                    _logger.LogWarning("[Complaint] Image file not found on disk: msgId={Id}", msgId);
                }
            }

            // ── STEP 3: Send to CRM FIRST ─────────────────────────────────────
            // Only save to DB if CRM succeeds.
            // Uses independent 120s timeout so n8n request timeout won't cancel it.
            var (crmTicketId, crmError) = await SendToCrmAsync(req, voiceFiles, imageFiles);

            if (crmTicketId is null)
            {
                // CRM failed — do NOT save to DB, return error to n8n
                _logger.LogWarning("[Complaint] CRM failed — skipping DB save. Error: {E}", crmError);
                return new SubmitComplaintResponseDto
                {
                    Success = false,
                    ComplaintId = null,
                    ErrorMessage = crmError ?? "Could not reach support team. Please try again.",
                    Message = "CRM submission failed",
                };
            }

            // ── STEP 4: CRM succeeded — now save to YOUR DB ───────────────────
            var complaint = new WhatsAppComplaint
            {
                WhatsappPhone = req.WhatsappPhone,
                StaffId = req.StaffId,
                StaffName = req.Name,
                OfficialPhone = req.OfficialPhone,
                Designation = req.Designation,
                Dept = req.Dept,
                GroupName = req.GroupName,
                Company = req.Company,
                LocationName = req.LocationName,
                Email = req.Email,
                Description = req.Description,
                ComplaintType = req.ComplaintType,
                CrmTicketId = crmTicketId,          // already known — set at insert
                Status = "open",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            await _complaintRepo.InsertAsync(complaint, ct);

            var complaintNumber = $"PR{complaint.Id:D5}";
            await _complaintRepo.UpdateComplaintNumberAsync(complaint.Id, complaintNumber, ct);

            // ── STEP 5: Save media rows ───────────────────────────────────────
            foreach (var msgId in req.VoiceMessageIds)
            {
                if (string.IsNullOrWhiteSpace(msgId)) continue;
                var waMsg = await _messageRepo.GetByMessageIdAsync(msgId, ct);
                await _complaintRepo.InsertMediaAsync(new WhatsAppComplaintMedia
                {
                    ComplaintId = complaint.Id,
                    MessageId = msgId,
                    MediaType = "voice",
                    FileUrl = waMsg?.FileUrl,
                    MimeType = waMsg?.MimeType ?? "audio/ogg",
                }, ct);
            }

            foreach (var msgId in req.ImageMessageIds)
            {
                if (string.IsNullOrWhiteSpace(msgId)) continue;
                var waMsg = await _messageRepo.GetByMessageIdAsync(msgId, ct);
                await _complaintRepo.InsertMediaAsync(new WhatsAppComplaintMedia
                {
                    ComplaintId = complaint.Id,
                    MessageId = msgId,
                    MediaType = "image",
                    FileUrl = waMsg?.FileUrl,
                    MimeType = waMsg?.MimeType ?? "image/jpeg",
                    Caption = waMsg?.Caption,
                }, ct);
            }

            _logger.LogInformation(
                "[Complaint] Saved — complaintNumber={N} crmTicketId={C} voices={V} images={I}",
                complaintNumber, crmTicketId,
                req.VoiceMessageIds.Count, req.ImageMessageIds.Count);

            return new SubmitComplaintResponseDto
            {
                Success = true,
                ComplaintId = crmTicketId,
                Message = req.ComplaintType == "agent_connect"
                    ? "Support agent request submitted successfully"
                    : "Complaint submitted to support team",
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // Read file directly from wwwroot/wa-media/{subFolder}/{messageId}.ext
        // WhatsAppMessageService saves files here — we know the exact path.
        // ─────────────────────────────────────────────────────────────────────
        private (string FileName, string MimeType, byte[] Data)?
            ReadFileDirect(string messageId, string subFolder, string mimeType)
        {
            try
            {
                var ext = MimeToExt(mimeType);
                var folder = Path.Combine(_env.WebRootPath, "wa-media", subFolder);

                var exactPath = Path.Combine(folder, $"{messageId}{ext}");
                if (File.Exists(exactPath))
                {
                    var bytes = File.ReadAllBytes(exactPath);
                    return ($"{messageId}{ext}", mimeType, bytes);
                }

                // Fallback: glob for any file starting with the messageId
                var matches = Directory.Exists(folder)
                    ? Directory.GetFiles(folder, $"{messageId}*")
                    : Array.Empty<string>();

                if (matches.Length > 0)
                {
                    var bytes = File.ReadAllBytes(matches[0]);
                    var actualExt = Path.GetExtension(matches[0]);
                    return (Path.GetFileName(matches[0]), ExtToMime(actualExt, mimeType), bytes);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Complaint] ReadFileDirect failed — msgId={Id}", messageId);
                return null;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST multipart/form-data to CRM.
        // Returns (crmTicketId, null) on success.
        // Returns (null, errorMessage) on failure — errorMessage shown to user.
        // Independent 120s timeout — NOT the request ct.
        // ─────────────────────────────────────────────────────────────────────
        private async Task<(string? TicketId, string? Error)> SendToCrmAsync(
            SubmitComplaintRequestDto req,
            List<(string FileName, string MimeType, byte[] Data)> voiceFiles,
            List<(string FileName, string MimeType, byte[] Data)> imageFiles)
        {
            var crmUrl = _config["Crm:SubmitUrl"];
            if (string.IsNullOrWhiteSpace(crmUrl))
            {
                _logger.LogWarning("[Complaint] Crm:SubmitUrl not configured");
                return (null, "CRM is not configured. Please contact system admin.");
            }

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));

            try
            {
                using var form = new MultipartFormDataContent();

                // Always send all text fields — even empty string.
                // PHP foreach() crashes if field is missing (null) but handles "" fine.
                void Text(string key, string? val)
                    => form.Add(new StringContent(val ?? string.Empty), key);

                Text("chat_phone", req.WhatsappPhone);
                Text("staff_id", req.StaffId);
                Text("name", req.Name);
                Text("phone", req.OfficialPhone);   // CRM field = phone
                Text("designation", req.Designation);
                Text("department", req.Dept);            // CRM field = department
                Text("groupname", req.GroupName);
                Text("company", req.Company);
                Text("locationname", req.LocationName);
                Text("email", req.Email);
                Text("description", req.Description);

                // Voice files → voice_file[] (actual binary)
                foreach (var (fileName, mimeType, data) in voiceFiles)
                {
                    var content = new ByteArrayContent(data);
                    content.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
                    form.Add(content, "voice_file[]", fileName);
                }

                // Image files → images[] (actual binary)
                foreach (var (fileName, mimeType, data) in imageFiles)
                {
                    var content = new ByteArrayContent(data);
                    content.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
                    form.Add(content, "images[]", fileName);
                }

                var client = _httpFactory.CreateClient("CrmClient");
                var response = await client.PostAsync(crmUrl, form, cts.Token);
                var body = await response.Content.ReadAsStringAsync(cts.Token);

                _logger.LogInformation("[Complaint] CRM {Code}: {Body}",
                    (int)response.StatusCode,
                    body.Length > 800 ? body[..800] : body);

                if (!response.IsSuccessStatusCode)
                {
                    var httpError = $"Support system returned error {(int)response.StatusCode}. Please try again.";
                    _logger.LogWarning("[Complaint] CRM HTTP {Code}: {Body}",
                        (int)response.StatusCode, body);
                    return (null, httpError);
                }

                // Parse: { "status": "success", "data": { "id": 13 } }
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                // Check status field
                if (root.TryGetProperty("status", out var sv) &&
                    sv.GetString()?.ToLower() != "success")
                {
                    // Extract CRM's own message if available
                    var crmMsg = root.TryGetProperty("message", out var mv)
                        ? mv.GetString()
                        : null;
                    var errMsg = string.IsNullOrWhiteSpace(crmMsg)
                        ? "Support system could not process the request. Please try again."
                        : crmMsg;
                    _logger.LogWarning("[Complaint] CRM status={S} message={M}",
                        sv.GetString(), crmMsg);
                    return (null, errMsg);
                }

                // Extract ticket ID from data.id
                if (root.TryGetProperty("data", out var dataEl) &&
                    dataEl.TryGetProperty("id", out var idEl))
                {
                    var crmId = idEl.ValueKind == JsonValueKind.Number
                        ? idEl.GetInt32().ToString()
                        : idEl.GetString();

                    _logger.LogInformation("[Complaint] CRM ticket created — id={Id}", crmId);
                    return (crmId, null);
                }

                return (null, "Support system responded but did not return a ticket ID. Please try again.");
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("[Complaint] CRM call timed out after 120s");
                return (null, "Support system is taking too long to respond. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Complaint] CRM call threw exception");
                return (null, "Could not reach support system. Please try again.");
            }
        }

        private static string MimeToExt(string mime) => mime switch
        {
            "audio/ogg" => ".ogg",
            "audio/mpeg" => ".mp3",
            "audio/mp4" => ".m4a",
            "audio/wav" => ".wav",
            "audio/opus" => ".opus",
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            _ => ".bin"
        };

        private static string ExtToMime(string ext, string fallback) =>
            ext.ToLowerInvariant() switch
            {
                ".ogg" => "audio/ogg",
                ".mp3" => "audio/mpeg",
                ".m4a" => "audio/mp4",
                ".wav" => "audio/wav",
                ".opus" => "audio/opus",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".gif" => "image/gif",
                _ => fallback
            };
    }
}
