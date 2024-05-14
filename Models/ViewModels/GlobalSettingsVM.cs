using System.ComponentModel.DataAnnotations;

namespace Models.ViewModels
{
    public class GlobalSettingsVM
    {
        public int? Id { get; set; }

        /// <summary>
        /// 公司名稱
        /// </summary>
        [Required(ErrorMessage = "名稱必填")]
        public string? CompanyName { get; set; }

        /// <summary>
        /// 公司地址
        /// </summary>
        [Required(ErrorMessage = "地址必填")]
        public string? CompanyAddress { get; set; }

        [Url(ErrorMessage = "連結格式錯誤")]
        public string? FacebookUrl { get; set; }

        [Url(ErrorMessage = "連結格式錯誤")]
        public string? LineUrl { get; set; }

        [Url(ErrorMessage = "連結格式錯誤")]
        public string? YoutubeUrl { get; set; }

        [Url(ErrorMessage = "連結格式錯誤")]
        public string? InstagramUrl { get; set; }

        /// <summary>
        /// 更新時間
        /// </summary>
        public string? UpdateTime { get; set; }
    }
}
