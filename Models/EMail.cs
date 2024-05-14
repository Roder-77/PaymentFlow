namespace Models
{
    public class Mail
    {
        public Mail(string subject, string body, string recipient)
        {
            Subject = subject;
            Body = body;
            Recipients = new List<string> { recipient };
        }

        public Mail(string subject, string body, IEnumerable<string> recipients)
        {
            Subject = subject;
            Body = body;
            Recipients = recipients;
        }

        /// <summary>
        /// 主旨
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        /// 內容
        /// </summary>
        public string Body { get; set; }

        public IEnumerable<string> Recipients { get; set; }

        public IEnumerable<string>? CarbonCopies { get; set; }

        public IEnumerable<string>? BlindCarbonCopies { get; set; }

        public IEnumerable<string>? AttachmentPaths { get; set; }
    }
}
