using Microsoft.Maui.ApplicationModel.Communication;

namespace TerranovaDemo;

public static class SmsService
{
    public static string PhoneNumber { get; set; } = string.Empty;

    public static async Task SendSMSAsync(string message)
    {
        if (string.IsNullOrEmpty(PhoneNumber))
            return;

        try
        {
            var smsMessage = new SmsMessage(message, PhoneNumber);
            await Sms.ComposeAsync(smsMessage);
        }
        catch { }
    }
}
