using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Models;
using Services.Interface;

namespace Services
{
    public class MailService : IMailService
    {
        private readonly MailSettings _mailSettings;
        private readonly IServiceScopeFactory _scopeFactory;

        public MailService(
            IOptions<MailSettings> mailSettings,
            IServiceScopeFactory scopeFactory)
        {
            _mailSettings = mailSettings.Value;
            _scopeFactory = scopeFactory;
        }

        #region Utilities

        #endregion

        /// <summary>
        /// 寄信
        /// </summary>
        /// <param name="mail">信件資料</param>
        public async Task SendMail(Mail mail)
        {
            var mailMessage = new MimeMessage { Subject = mail.Subject };

            mailMessage.From.Add(MailboxAddress.Parse(_mailSettings.From));
            mailMessage.To.AddRange(mail.Recipients.Select(x => MailboxAddress.Parse(x)));

            // 副本
            if (mail.CarbonCopies is not null)
                mailMessage.Cc.AddRange(mail.CarbonCopies.Select(x => MailboxAddress.Parse(x)));

            // 密件副本
            if (mail.BlindCarbonCopies is not null)
                mailMessage.Cc.AddRange(mail.BlindCarbonCopies.Select(x => MailboxAddress.Parse(x)));

            // 內容
            var bodyBuilder = new BodyBuilder();
            // 多換一行 (因避免威合 SMTP server 免責聲明直接接在內容後面)
            bodyBuilder.HtmlBody = $"{mail.Body}<br><br>**這是一封提醒郵件，請勿直接回覆，謝謝。<br>";

            // 附件
            if (mail.AttachmentPaths is not null)
                foreach (var path in mail.AttachmentPaths)
                    bodyBuilder.Attachments.Add(path);

            mailMessage.Body = bodyBuilder.ToMessageBody();

            // 發送
            using var smtpClient = new SmtpClient();
            await smtpClient.ConnectAsync(_mailSettings.Host, _mailSettings.Port, SecureSocketOptions.Auto);

            if (!string.IsNullOrWhiteSpace(_mailSettings.User) && !string.IsNullOrWhiteSpace(_mailSettings.Password))
                await smtpClient.AuthenticateAsync(_mailSettings.User, _mailSettings.Password);

            await smtpClient.SendAsync(mailMessage);
            await smtpClient.DisconnectAsync(true);
        }

        /// <summary>
        /// 背景寄信
        /// </summary>
        /// <param name="mail">信件資料</param>
        public void SendMailByBackground(Mail? mail)
        {
            if (mail is null)
                return;

            Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<MailService>>();

                try
                {
                    await SendMail(mail);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"{nameof(SendMailByBackground)}, subject: {mail.Subject} recipients: {string.Join(",", mail.Recipients)}");
                }
            });
        }
    }
}
