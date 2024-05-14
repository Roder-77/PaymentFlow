using Models;

namespace Services.Interface
{
    public interface IMailService
    {
        public delegate IMailService MailServiceResolver(MailServiceType type);

        public enum MailServiceType { Normal, Aws };

        Task SendMail(Mail mail);

        void SendMailByBackground(Mail? mail);
    }
}
