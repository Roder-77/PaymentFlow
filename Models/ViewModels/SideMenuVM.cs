namespace Models.ViewModels
{
    public class SideMenuVM
    {
        public int? Id { get; set; }

        public string? Name { get; set; }

        public int? Order { get; set; }

        public IEnumerable<SideMenuVM> Children { get; set; }
    }
}
