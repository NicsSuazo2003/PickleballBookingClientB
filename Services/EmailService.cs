using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace PickleballBookingSystem.Services;

public class EmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    private const string OUTER_BG = "#0D1A0D";  // forest-950
    private const string BODY_BG = "#1A2E1A";  // forest-900
    private const string CARD_BG = "#243024";  // forest-800
    private const string BANNER_BG = "#2A3A2A";  // forest-700
    private const string DIVIDER = "#324232";  // forest-600
    private const string BORDER_SOFT = "#3A4A3A";  // forest-500

    private const string ACCENT = "#D4AF37";  // gold-400
    private const string ACCENT_TEXT = "#0D1A0D";  // forest-950 (dark text on gold)
    private const string ACCENT_SOFT = "#EAD79E";  // gold-200
    private const string GOLD_300 = "#DEC36E";  // gold-300

    private const string CYAN = "#22D3EE";  // cyan-400 (Open Play — keep)
    private const string TEXT_CREAM = "#F5F0E8";  // cream
    private const string TEXT_CREAM_MUTED = "#B8B0A0";  // cream-muted
    private const string TEXT_CREAM_SOFT = "#E8E0D2";  // cream-dark

    private const string DANGER = "#E74C3C";  // error
    private const string WARNING = "#F39C12";  // warning
    private const string SUCCESS = "#2ECC71";  // success
    private const string PURPLE = "#A78BFA";  // refund (no tailwind equivalent)


    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    // ═════════════════════════════════════════════════════════════
    // ⏰ Time formatting — always output 12-hour AM/PM
    // ═════════════════════════════════════════════════════════════
    private static string FormatTime(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;

        input = input.Trim();

        if (Regex.IsMatch(input, @"\b(AM|PM)\b", RegexOptions.IgnoreCase))
            return input;

        var match = Regex.Match(input, @"^(\d{1,2}):(\d{2})");
        if (!match.Success) return input;

        if (!int.TryParse(match.Groups[1].Value, out var hour)) return input;
        var minute = match.Groups[2].Value;

        var period = hour >= 12 ? "PM" : "AM";
        var hour12 = hour % 12;
        if (hour12 == 0) hour12 = 12;

        return $"{hour12}:{minute} {period}";
    }

    private static string FormatTimeRange(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;

        var parts = Regex.Split(input.Trim(), @"\s*[-–—]\s*");
        if (parts.Length == 2)
        {
            return $"{FormatTime(parts[0])} – {FormatTime(parts[1])}";
        }

        return FormatTime(input);
    }

    private static string FormatDate(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;

        if (DateTime.TryParse(input, out var dt))
        {
            return dt.ToString("MMMM d, yyyy");
        }

        return input;
    }

    // ═════════════════════════════════════════════════════════════
    // 🎨 Shared layout — Forest Green theme
    // ═════════════════════════════════════════════════════════════
    private static string WrapLayout(string bannerTitle, string contentHtml)
    {
        return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='UTF-8'>
<meta name='viewport' content='width=device-width,initial-scale=1'>
<meta name='color-scheme' content='dark light'>
<title>Center Court</title>
</head>
<body style='margin:0;padding:0;background-color:{OUTER_BG};font-family:-apple-system,BlinkMacSystemFont,""Segoe UI"",Roboto,Helvetica,Arial,sans-serif;'>
  <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='background-color:{OUTER_BG};padding:24px 12px;'>
    <tr>
      <td align='center'>

        <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='max-width:560px;background-color:{BODY_BG};border-radius:16px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.5);'>

          <!-- ░░ Forest Green banner header ░░ -->
          <tr>
            <td style='background-color:{BANNER_BG};border-bottom:1px solid {DIVIDER};padding:26px 32px 22px;text-align:left;'>
              <div style='font-size:11px;font-weight:800;letter-spacing:2.5px;color:{ACCENT};text-transform:uppercase;margin-bottom:6px;font-family:-apple-system,BlinkMacSystemFont,""Segoe UI"",Roboto,Helvetica,Arial,sans-serif;'>
                CENTER COURT
              </div>
              <div style='font-size:24px;font-weight:800;color:{TEXT_CREAM};line-height:1.2;margin:0;letter-spacing:-0.3px;'>
                {bannerTitle}
              </div>
            </td>
          </tr>

          <!-- ░░ Body content ░░ -->
          <tr>
            <td style='padding:32px;'>
              {contentHtml}
            </td>
          </tr>

          <!-- ░░ Footer ░░ -->
          <tr>
            <td style='padding:20px 32px 26px;border-top:1px solid {DIVIDER};'>
              <div style='font-size:11px;color:{TEXT_CREAM_MUTED};text-align:center;line-height:1.6;'>
                Book Your Court. Play Your Game.<br>
                Automated message — please do not reply directly.
              </div>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
    }

    // 🧱 Key-value row — cream label + white value
    private static string KvRow(string label, string value, bool isLast = false)
    {
        var border = isLast ? "" : $"border-bottom:1px solid {DIVIDER};";
        return $@"
              <tr>
                <td style='padding:14px 0;{border}'>
                  <div style='font-size:10px;font-weight:800;letter-spacing:1.8px;color:{TEXT_CREAM_MUTED};text-transform:uppercase;margin-bottom:5px;'>
                    {label}
                  </div>
                  <div style='font-size:16px;font-weight:600;color:{TEXT_CREAM};line-height:1.35;'>
                    {value}
                  </div>
                </td>
              </tr>";
    }

    // 🎯 Gold CTA button — kept gold (primary action color)
    private static string CtaButton(string url, string text)
    {
        return $@"
              <table role='presentation' cellpadding='0' cellspacing='0' border='0' style='margin-top:28px;'>
                <tr>
                  <td align='center' style='border-radius:10px;background-color:{ACCENT};'>
                    <a href='{url}' target='_blank' style='display:inline-block;padding:13px 30px;font-size:14px;font-weight:700;color:{ACCENT_TEXT};text-decoration:none;border-radius:10px;'>
                      {text}
                    </a>
                  </td>
                </tr>
              </table>";
    }

    // 🏷 Status chip — forest-friendly with border + soft fill
    private static string StatusChip(string text, string color)
    {
        return $@"
              <div style='display:inline-block;padding:6px 14px;border-radius:999px;background-color:{color}22;border:1px solid {color}55;margin-bottom:24px;'>
                <span style='font-size:11px;font-weight:800;letter-spacing:1.5px;color:{color};text-transform:uppercase;'>— {text}</span>
              </div>";
    }

    // ═════════════════════════════════════════════════════════════
    // 1. ADMIN — New Booking
    // ═════════════════════════════════════════════════════════════
    public async Task NotifyAdminNewBookingAsync(string customerName, string referenceCode, string date, string time, string amount)
    {
        try
        {
            var apiKey = _config["Brevo:ApiKey"];
            var senderEmail = _config["Brevo:SenderEmail"];
            var senderName = _config["Brevo:SenderName"];
            var adminEmail = _config["Brevo:AdminEmail"];
            var frontendUrl = _config["App:FrontendUrl"];

            var prettyDate = FormatDate(date);
            var prettyTime = FormatTimeRange(time);

            var content = $@"
              <p style='margin:0 0 22px;font-size:15px;line-height:1.65;color:{TEXT_CREAM_SOFT};'>
                A new booking has come in. Review the details below and confirm once payment is verified.
              </p>

              <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0'>
                {KvRow("Customer", customerName)}
                {KvRow("Reference", referenceCode)}
                {KvRow("Schedule", $"{prettyDate} · {prettyTime}")}
                {KvRow("Amount", amount, isLast: true)}
              </table>

              {CtaButton($"{frontendUrl}/admin/bookings", "Review in Admin Panel")}
            ";

            var html = WrapLayout("New Booking", content);
            await SendAsync(apiKey, senderEmail, senderName, adminEmail, "Admin",
                $"🔔 New Booking: {referenceCode} — {customerName}", html);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email notification failed");
        }
    }

    // ═════════════════════════════════════════════════════════════
    // 2. CUSTOMER — Booking Confirmed
    // ═════════════════════════════════════════════════════════════
    public async Task NotifyCustomerBookingConfirmedAsync(
        string customerEmail,
        string customerName,
        string referenceCode,
        string date,
        string time,
        string? amount = null)
    {
        try
        {
            var apiKey = _config["Brevo:ApiKey"];
            var senderEmail = _config["Brevo:SenderEmail"];
            var senderName = _config["Brevo:SenderName"];
            var frontendUrl = _config["App:FrontendUrl"];

            var prettyDate = FormatDate(date);
            var prettyTime = FormatTimeRange(time);

            var amountRow = string.IsNullOrWhiteSpace(amount)
                ? ""
                : KvRow("Amount Paid", amount);

            var content = $@"
              <p style='margin:0 0 8px;font-size:15px;color:{TEXT_CREAM_SOFT};'>
                Hi {customerName},
              </p>
              <p style='margin:0 0 24px;font-size:15px;line-height:1.65;color:{TEXT_CREAM_SOFT};'>
                Great news — your booking has been <strong style='color:{CYAN};font-weight:700;'>confirmed</strong>. See you on the court!
              </p>

              {StatusChip("Paid", CYAN)}

              <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0'>
                {KvRow("Reference", referenceCode)}
                {KvRow("Schedule", $"{prettyDate} · {prettyTime}")}
                {amountRow}
                {KvRow("Status", "Confirmed", isLast: true)}
              </table>

              {CtaButton($"{frontendUrl}/track", "Track Your Booking")}
            ";

            var html = WrapLayout("Booking Confirmed", content);
            await SendAsync(apiKey, senderEmail, senderName, customerEmail, customerName,
                $"✅ Booking Confirmed: {referenceCode}", html);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Customer email notification failed");
        }
    }

    // ═════════════════════════════════════════════════════════════
    // 3. CUSTOMER — Booking Rejected
    // ═════════════════════════════════════════════════════════════
    public async Task NotifyCustomerBookingRejectedAsync(
        string customerEmail,
        string customerName,
        string referenceCode,
        string date,
        string time,
        string? reason = null,
        string? amount = null)
    {
        try
        {
            var apiKey = _config["Brevo:ApiKey"];
            var senderEmail = _config["Brevo:SenderEmail"];
            var senderName = _config["Brevo:SenderName"];
            var frontendUrl = _config["App:FrontendUrl"];

            var prettyDate = FormatDate(date);
            var prettyTime = FormatTimeRange(time);

            var amountRow = string.IsNullOrWhiteSpace(amount)
                ? ""
                : KvRow("Amount Paid", amount);

            var reasonBlock = string.IsNullOrWhiteSpace(reason)
                ? ""
                : $@"
              <div style='margin-top:20px;padding:14px 16px;border-left:3px solid {DANGER};background-color:{DANGER}15;border-radius:6px;'>
                <div style='font-size:10px;font-weight:800;letter-spacing:1.8px;color:{TEXT_CREAM_MUTED};text-transform:uppercase;margin-bottom:5px;'>Reason</div>
                <div style='font-size:14px;color:{TEXT_CREAM};line-height:1.55;'>{reason}</div>
              </div>";

            var content = $@"
              <p style='margin:0 0 8px;font-size:15px;color:{TEXT_CREAM_SOFT};'>
                Hi {customerName},
              </p>
              <p style='margin:0 0 24px;font-size:15px;line-height:1.65;color:{TEXT_CREAM_SOFT};'>
                Unfortunately, your booking could not be approved at this time.
              </p>

              {StatusChip("Not Approved", DANGER)}

              <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0'>
                {KvRow("Reference", referenceCode)}
                {KvRow("Schedule", $"{prettyDate} · {prettyTime}")}
                {amountRow}
                {KvRow("Status", "Not Approved", isLast: true)}
              </table>

              {reasonBlock}

              <p style='margin:24px 0 0;font-size:14px;line-height:1.65;color:{TEXT_CREAM_MUTED};'>
                If you believe this is a mistake, or would like to rebook, please get in touch — we're happy to help.
              </p>

              {CtaButton($"{frontendUrl}/track", "Track Your Booking")}
            ";

            var html = WrapLayout("Booking Not Approved", content);
            await SendAsync(apiKey, senderEmail, senderName, customerEmail, customerName,
                $"❌ Booking Not Approved: {referenceCode}", html);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rejection email notification failed");
        }
    }

    // ═════════════════════════════════════════════════════════════
    // 4. CUSTOMER — Booking Cancelled
    // ═════════════════════════════════════════════════════════════
    public async Task NotifyCustomerBookingCancelledAsync(
        string customerEmail,
        string customerName,
        string referenceCode,
        string date,
        string time,
        string? reason = null,
        string? amount = null)
    {
        try
        {
            var apiKey = _config["Brevo:ApiKey"];
            var senderEmail = _config["Brevo:SenderEmail"];
            var senderName = _config["Brevo:SenderName"];
            var frontendUrl = _config["App:FrontendUrl"];

            var prettyDate = FormatDate(date);
            var prettyTime = FormatTimeRange(time);

            var amountRow = string.IsNullOrWhiteSpace(amount)
                ? ""
                : KvRow("Amount Paid", amount);

            var reasonBlock = string.IsNullOrWhiteSpace(reason)
                ? ""
                : $@"
              <div style='margin-top:20px;padding:14px 16px;border-left:3px solid {WARNING};background-color:{WARNING}15;border-radius:6px;'>
                <div style='font-size:10px;font-weight:800;letter-spacing:1.8px;color:{TEXT_CREAM_MUTED};text-transform:uppercase;margin-bottom:5px;'>Reason</div>
                <div style='font-size:14px;color:{TEXT_CREAM};line-height:1.55;'>{reason}</div>
              </div>";

            var content = $@"
              <p style='margin:0 0 8px;font-size:15px;color:{TEXT_CREAM_SOFT};'>
                Hi {customerName},
              </p>
              <p style='margin:0 0 24px;font-size:15px;line-height:1.65;color:{TEXT_CREAM_SOFT};'>
                Your booking has been <strong style='color:{WARNING};font-weight:700;'>cancelled</strong>. If this was unexpected, please reach out and we'll help sort it out.
              </p>

              {StatusChip("Cancelled", WARNING)}

              <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0'>
                {KvRow("Reference", referenceCode)}
                {KvRow("Schedule", $"{prettyDate} · {prettyTime}")}
                {amountRow}
                {KvRow("Status", "Cancelled", isLast: true)}
              </table>

              {reasonBlock}

              <p style='margin:24px 0 0;font-size:14px;line-height:1.65;color:{TEXT_CREAM_MUTED};'>
                Want to play again? You can start a fresh booking anytime.
              </p>

              {CtaButton($"{frontendUrl}/track", "Track Your Booking")}
            ";

            var html = WrapLayout("Booking Cancelled", content);
            await SendAsync(apiKey, senderEmail, senderName, customerEmail, customerName,
                $"⚠️ Booking Cancelled: {referenceCode}", html);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cancellation email notification failed");
        }
    }

    // ═════════════════════════════════════════════════════════════
    // 5. CUSTOMER — Booking Refunded
    // ═════════════════════════════════════════════════════════════
    public async Task NotifyCustomerBookingRefundedAsync(
        string customerEmail,
        string customerName,
        string referenceCode,
        string date,
        string time,
        string? amount = null,
        string? reason = null)
    {
        try
        {
            var apiKey = _config["Brevo:ApiKey"];
            var senderEmail = _config["Brevo:SenderEmail"];
            var senderName = _config["Brevo:SenderName"];
            var frontendUrl = _config["App:FrontendUrl"];

            var prettyDate = FormatDate(date);
            var prettyTime = FormatTimeRange(time);

            var amountRow = string.IsNullOrWhiteSpace(amount)
                ? ""
                : KvRow("Amount Refunded", amount);

            var reasonBlock = string.IsNullOrWhiteSpace(reason)
                ? ""
                : $@"
              <div style='margin-top:20px;padding:14px 16px;border-left:3px solid {PURPLE};background-color:{PURPLE}15;border-radius:6px;'>
                <div style='font-size:10px;font-weight:800;letter-spacing:1.8px;color:{TEXT_CREAM_MUTED};text-transform:uppercase;margin-bottom:5px;'>Reason</div>
                <div style='font-size:14px;color:{TEXT_CREAM};line-height:1.55;'>{reason}</div>
              </div>";

            var content = $@"
              <p style='margin:0 0 8px;font-size:15px;color:{TEXT_CREAM_SOFT};'>
                Hi {customerName},
              </p>
              <p style='margin:0 0 24px;font-size:15px;line-height:1.65;color:{TEXT_CREAM_SOFT};'>
                Your booking has been <strong style='color:{PURPLE};font-weight:700;'>refunded</strong>. The amount will be returned to your original payment method.
              </p>

              {StatusChip("Refunded", PURPLE)}

              <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0'>
                {KvRow("Reference", referenceCode)}
                {KvRow("Schedule", $"{prettyDate} · {prettyTime}")}
                {amountRow}
                {KvRow("Status", "Refunded", isLast: true)}
              </table>

              {reasonBlock}

              <p style='margin:24px 0 0;font-size:14px;line-height:1.65;color:{TEXT_CREAM_MUTED};'>
                Refunds are typically processed within 3–5 business days, depending on your bank or e-wallet. If you haven't received it by then, please reach out and we'll check on it.
              </p>

              <p style='margin:16px 0 0;font-size:14px;line-height:1.65;color:{TEXT_CREAM_MUTED};'>
                We'd love to see you back on the court sometime soon!
              </p>

              {CtaButton($"{frontendUrl}/", "Book Again")}
            ";

            var html = WrapLayout("Booking Refunded", content);
            await SendAsync(apiKey, senderEmail, senderName, customerEmail, customerName,
                $"💜 Booking Refunded: {referenceCode}", html);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refund email notification failed");
        }
    }

    // ═════════════════════════════════════════════════════════════
    // 🔧 Shared sender
    // ═════════════════════════════════════════════════════════════
    private async Task SendAsync(
        string? apiKey,
        string? senderEmail,
        string? senderName,
        string? toEmail,
        string toName,
        string subject,
        string html)
    {
        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(toEmail))
        {
            _logger.LogWarning("Brevo config incomplete — skipping email {Subject}", subject);
            return;
        }

        using var http = new HttpClient();

        var payload = new
        {
            sender = new { email = senderEmail, name = senderName ?? "Center Court" },
            to = new[] { new { email = toEmail, name = toName } },
            subject,
            htmlContent = html
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("api-key", apiKey);

        var response = await http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError("Brevo failed: {Status} {Body}", response.StatusCode, body);
        }
        else
        {
            _logger.LogInformation("Email sent → {To} ({Subject})", toEmail, subject);
        }
    }
}