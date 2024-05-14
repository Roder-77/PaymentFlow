using System.ComponentModel.DataAnnotations;


namespace Models.ViewModels
{
    public class LogInAccountVM
    {
        [Required(ErrorMessage = "帳號為必填")]
        public string? UserName { get; set; }

        [Required(ErrorMessage = "密碼為必填")]
        public string? Password { get; set; }

        public bool RememberMe { get; set; }
    }

    public class AccountListFilterVM : BaseFilterVM
    {
        public int? PermissionId { get; set; }

        public SortCondition<AccountSortColumn>? SortCondition { get; set; }
    }

    public enum AccountSortColumn
    {
        Id
    }

    public class AccountVM : BaseStatusVM
    {
        public int? Id { get; set; }

        /// <summary>
        /// 信箱 (帳號)
        /// </summary>
        [EmailAddress(ErrorMessage = "信箱格式不正確")]
        public string? Email { get; set; }

        /// <summary>
        /// 人員姓名
        /// </summary>
        [Required(ErrorMessage = "姓名必填")]
        public string? Name { get; set; }

        /// <summary>
        /// 所屬單位
        /// </summary>
        public string? Department { get; set; }

        /// <summary>
        /// 權限代碼
        /// </summary>
        [Required(ErrorMessage = "角色必填")]
        public int? PermissionId { get; set; }

        /// <summary>
        /// 建立時間
        /// </summary>
        public string? CreateTime { get; set; }

        /// <summary>
        /// 建立時間
        /// </summary>
        public string? UpdateTime { get; set; }

        public PermissionVM? Permission { get; set; }
    }

    public class UpdateAccountPasswordVM
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "密碼必填")]
        [StringLength(20)]
        public string? Password { get; set; }

        [Required(ErrorMessage = "再次輸入密碼必填")]
        [StringLength(20)]
        public string? PasswordAgain { get; set; }
    }
}
