using System.ComponentModel.DataAnnotations;

namespace Common.Enums
{
    public enum ImageSize
    {
        [Display(Name = "1920px * 860px")]
        Size1920x860,

        [Display(Name = "1920px * 520px")]
        Size1920x520,

        [Display(Name = "556px * 800px")]
        Size556x800,

        [Display(Name = "280px * 280px")]
        Size280x280,
    }
}
