using Common.Enums;
using Common.Extensions;
using Microsoft.AspNetCore.Http;
using Models.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Models
{
    #region Interface

    public interface IBaseFilter
    {
        public string? Keyword { get; set; }

        public int Page { get; set; }

        public int PageSize { get; set; }
    }

    public interface ISoftDeleteFilter
    {
        public bool? IsDeleted { get; set; }
    }

    #endregion

    #region Base

    public class BaseFilterVM : IBaseFilter, ISoftDeleteFilter
    {
        public string? Keyword { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public bool? IsDeleted { get; set; }
    }

    public class BaseImageVM
    {
        [AllowedExtensions(UploadType.Image)]
        public IFormFile? File { get; set; }

        public string? Url { get; set; }
    }

    public class BaseStatusVM : ISoftDeleteFilter
    {
        /// <summary>
        /// 是否刪除
        /// </summary>
        [Required(ErrorMessage = "狀態必填")]
        public virtual bool? IsDeleted { get; set; } = false;

        /// <summary>
        /// 狀態
        /// </summary>
        public string Status => !IsDeleted.HasValue ? "異常" : IsDeleted.Value ? "停用" : "啟用";
    }

    #endregion

    public class SortCondition<TEnum> where TEnum : struct
    {
        /// <summary>
        /// 排序欄位
        /// </summary>
        public TEnum? SortColumn { get; set; }

        /// <summary>
        /// 排序類型
        /// </summary>
        public SortType? SortType { get; set; }

        /// <summary>
        /// 欄位名稱
        /// </summary>
        [JsonIgnore]
        public string ColumnName => !SortColumn.HasValue ? string.Empty : SortColumn.Value.GetAttributeContent(AttributeType.Description);

        [JsonIgnore]
        public bool HasValue => SortColumn.HasValue && SortType.HasValue;
    }
}
