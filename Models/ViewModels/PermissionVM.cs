using Common.Enums;
using System.ComponentModel.DataAnnotations;

namespace Models.ViewModels
{
    public class PermissionVM
    {
        /// <summary>
        /// 角色代碼
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// 角色名稱
        /// </summary>
        [Required(ErrorMessage = "名稱必填")]
        public string? Name { get; set; }

        /// <summary>
        /// 角色簡介
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 是否為最高權限帳號
        /// </summary>
        public bool IsAdmin { get; set; }

        /// <summary>
        /// 建立時間
        /// </summary>
        public string? CreateTime { get; set; }

        /// <summary>
        /// 建立時間
        /// </summary>
        public string? UpdateTime { get; set; }

        public IEnumerable<PermissionSideMenuRelationVM>? Relations { get; set; } = new HashSet<PermissionSideMenuRelationVM>();
    }

    public class PermissionSideMenuRelationVM
    {
        /// <summary>
        /// 選單代碼
        /// </summary>
        public int? SideMenuId { get; set; }

        /// <summary>
        /// 權限狀態
        /// </summary>
        public PermissionStatus? Status { get; set; }
    }


    public class PermissionListFilterVM : BaseFilterVM
    { }
}
