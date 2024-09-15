namespace BlazorAut.Data
{

    public class Role
    {
        public int Id { get; set; }
        public string RoleName { get; set; }
        public List<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }

    public class UserRole
    {
        public int UserId { get; set; }
        public User User { get; set; }

        public int RoleId { get; set; }
        public Role Role { get; set; }
    }
}

