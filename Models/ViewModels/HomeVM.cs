using Common.Enums;
using System.ComponentModel.DataAnnotations;

#nullable disable warnings

namespace Models.ViewModels
{
    public class HomeBannerFilterVM : BaseFilterVM
    {
        public long? StartTime { get; set; }

        public long? EndTime { get; set; }

        public Status? Status { get; set; }
    }


    public class HomeBannerVM : BaseStatusVM
    {
        public int? Id { get; set; }

        public string? Title { get; set; }

        public string? Subtitle { get; set; }

        [Url(ErrorMessage = "連結格式有誤")]
        [Required(ErrorMessage = "連結必填")]
        public string? Url { get; set; }

        [Required(ErrorMessage = "上架時間必填")]
        public DateTime? StartTime { get; set; }

        [Required(ErrorMessage = "下架時間必填")]
        public DateTime? EndTime { get; set; }

        public string? CreateTime { get; set; }

        public string? UpdateTime { get; set; }

        public BaseImageVM Image { get; set; } = new();
    }

    public class HomeBrandConceptVM
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "標題必填")]
        public string? Title { get; set; }

        [Required(ErrorMessage = "副標題必填")]
        public string? Subtitle { get; set; }

        [Required(ErrorMessage = "說明文字必填")]
        public string? Description { get; set; }

        public string? UpdateTime { get; set; }

        public List<AreaVM> Areas { get; set; }

        public class AreaVM
        {
            public int? Id { get; set; }

            [Required(ErrorMessage = "標題必填")]
            public string? Title { get; set; }

            [Required(ErrorMessage = "副標題必填")]
            public string? Subtitle { get; set; }

            [Required(ErrorMessage = "說明文字必填")]
            public string? Description { get; set; }

            [Url(ErrorMessage = "連結格式有誤")]
            [Required(ErrorMessage = "連結必填")]
            public string? Url { get; set; }

            public BaseImageVM Image1 { get; set; } = new();

            public BaseImageVM Image2 { get; set; } = new();
        }
    }

    public class HomeBrandFeatureVM
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "標題必填")]
        public string? Title { get; set; }

        [Required(ErrorMessage = "副標題必填")]
        public string? Subtitle { get; set; }

        [Required(ErrorMessage = "說明文字必填")]
        public string? Description { get; set; }

        public BaseImageVM Image { get; set; } = new();

        public string? UpdateTime { get; set; }

        public List<AreaVM> Areas { get; set; }

        public class AreaVM
        {
            public int? Id { get; set; }

            [Required(ErrorMessage = "標題必填")]
            public string? Title { get; set; }

            [Required(ErrorMessage = "說明文字必填")]
            public string? Description { get; set; }

            public BaseImageVM Image { get; set; } = new();
        }
    }

    public class HomeProductOverviewVM
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "標題必填")]
        public string? Title { get; set; }

        [Required(ErrorMessage = "副標題必填")]
        public string? Subtitle { get; set; }

        [Required(ErrorMessage = "說明文字必填")]
        public string? Description { get; set; }

        public string? UpdateTime { get; set; }

        public List<AreaVM> Areas { get; set; }

        public class AreaVM
        {
            public int? Id { get; set; }

            [Required(ErrorMessage = "標題必填")]
            public string? Title { get; set; }

            [Required(ErrorMessage = "說明文字必填")]
            public string? Description { get; set; }
        }
    }
}
