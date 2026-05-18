// ============================================================
// Services/EmailTemplates.cs
// Templates HTML pour les emails MoneyMoney
// ============================================================

namespace MoneyTransferApp.Services
{
    public static class EmailTemplates
    {
        // ── Email envoyé quand un transfert est créé ──────────
        public static string TransferConfirmation(string fullName, string serialNumber,
            decimal amount, string fromCurrency, decimal convertedAmount, string toCurrency)
        {
            return $@"
<div style='font-family:Arial,sans-serif;max-width:520px;margin:0 auto;'>
  <div style='background:#1490dc;padding:24px;border-radius:10px 10px 0 0;text-align:center;'>
    <h2 style='color:white;margin:0;'>💸 Money Money</h2>
    <p style='color:rgba(255,255,255,0.85);margin:6px 0 0;'>Transfer Confirmation</p>
  </div>
  <div style='background:#f8fbff;padding:28px;border:1px solid #e2e8f0;border-top:none;border-radius:0 0 10px 10px;'>
    <p style='margin:0 0 16px;'>Hello <strong>{fullName}</strong>,</p>
    <p>Your transfer has been <strong style='color:#00b48c;'>confirmed</strong>.</p>
    <div style='background:white;border:1px solid #e2e8f0;border-radius:8px;padding:16px;margin:16px 0;'>
      <p style='margin:0 0 8px;font-size:0.85rem;color:#6b7a8d;'>SERIAL NUMBER</p>
      <p style='margin:0 0 16px;font-family:monospace;font-size:1.1rem;font-weight:700;color:#1490dc;'>{serialNumber}</p>
      <p style='margin:0 0 4px;font-size:0.85rem;color:#6b7a8d;'>AMOUNT SENT</p>
      <p style='margin:0 0 16px;font-size:1.2rem;font-weight:700;'>{amount:F2} {fromCurrency}</p>
      <p style='margin:0 0 4px;font-size:0.85rem;color:#6b7a8d;'>AMOUNT RECEIVED</p>
      <p style='margin:0;font-size:1.2rem;font-weight:700;color:#00b48c;'>{convertedAmount:F2} {toCurrency}</p>
    </div>
    <p style='font-size:0.82rem;color:#6b7a8d;'>Use the serial number above to track your transfer at any time.</p>
    <hr style='border:none;border-top:1px solid #e2e8f0;margin:20px 0;'/>
    <p style='font-size:0.78rem;color:#6b7a8d;margin:0;'>This is an automated message from Money Money. Do not reply to this email.</p>
  </div>
</div>";
        }

        // ── Email envoyé quand un cash-in est reçu ───────────
        public static string CashInConfirmation(string fullName, string serialNumber,
            decimal amount, string currency, decimal convertedUsd)
        {
            return $@"
<div style='font-family:Arial,sans-serif;max-width:520px;margin:0 auto;'>
  <div style='background:#00b48c;padding:24px;border-radius:10px 10px 0 0;text-align:center;'>
    <h2 style='color:white;margin:0;'>💰 Money Money</h2>
    <p style='color:rgba(255,255,255,0.85);margin:6px 0 0;'>Cash-In Received</p>
  </div>
  <div style='background:#f8fbff;padding:28px;border:1px solid #e2e8f0;border-top:none;border-radius:0 0 10px 10px;'>
    <p style='margin:0 0 16px;'>Hello <strong>{fullName}</strong>,</p>
    <p>You have received a <strong style='color:#00b48c;'>Cash-In</strong> on your account.</p>
    <div style='background:white;border:1px solid #e2e8f0;border-radius:8px;padding:16px;margin:16px 0;'>
      <p style='margin:0 0 4px;font-size:0.85rem;color:#6b7a8d;'>SERIAL NUMBER</p>
      <p style='margin:0 0 16px;font-family:monospace;font-weight:700;color:#1490dc;'>{serialNumber}</p>
      <p style='margin:0 0 4px;font-size:0.85rem;color:#6b7a8d;'>AMOUNT</p>
      <p style='margin:0 0 16px;font-size:1.2rem;font-weight:700;'>{amount:F2} {currency}</p>
      <p style='margin:0 0 4px;font-size:0.85rem;color:#6b7a8d;'>ADDED TO BALANCE</p>
      <p style='margin:0;font-size:1.2rem;font-weight:700;color:#00b48c;'>+{convertedUsd:F2} USD</p>
    </div>
    <p style='font-size:0.78rem;color:#6b7a8d;margin:0;'>This is an automated message from Money Money.</p>
  </div>
</div>";
        }

        // ── Email envoyé quand le statut d'agent change ───────
        public static string AgentStatusUpdate(string fullName, string storeName, bool approved)
        {
            var color = approved ? "#00b48c" : "#dc2626";
            var status = approved ? "Approved ✅" : "Not Approved";
            var msg = approved
                ? "Congratulations! Your agent application has been approved. You can now log in as an Agent and start processing cash operations."
                : "Unfortunately, your agent application was not approved at this time. Please contact support for more information.";

            return $@"
<div style='font-family:Arial,sans-serif;max-width:520px;margin:0 auto;'>
  <div style='background:{color};padding:24px;border-radius:10px 10px 0 0;text-align:center;'>
    <h2 style='color:white;margin:0;'>🏪 Money Money</h2>
    <p style='color:rgba(255,255,255,0.85);margin:6px 0 0;'>Agent Application Update</p>
  </div>
  <div style='background:#f8fbff;padding:28px;border:1px solid #e2e8f0;border-top:none;border-radius:0 0 10px 10px;'>
    <p>Hello <strong>{fullName}</strong>,</p>
    <p>Your agent application for <strong>{storeName}</strong> has been reviewed.</p>
    <p style='font-size:1.1rem;font-weight:700;color:{color};'>Status: {status}</p>
    <p>{msg}</p>
    <p style='font-size:0.78rem;color:#6b7a8d;'>Money Money Team</p>
  </div>
</div>";
        }
    }
}