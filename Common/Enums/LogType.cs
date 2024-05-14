using System.ComponentModel.DataAnnotations;

namespace Common.Enums
{
    public enum LogMenuType
    {
        [Display(Name = "無")]
        None = 0,

        [Display(Name = "帳號管理")]
        Account,

        [Display(Name = "權限管理")]
        Permission,

        [Display(Name = "全域管理")]
        GlobalSetting,

        [Display(Name = "主頁 Banner 管理")]
        HomeBanner,

        [Display(Name = "主頁內容管理")]
        HomeContent,
    }

    public enum LogAction
    {
        其他,
        新增,
        更新,
        刪除
    }
}
