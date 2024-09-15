namespace BlazorAut.Data
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; }

        public List<UserToken> Tokens { get; set; }
        public List<UserRole> UserRoles { get; set; }
    }

    public class UserToken
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Token { get; set; }
        public DateTime Expiration { get; set; }
        public DateTime CreatedAt { get; set; }

        public User User { get; set; }
    }
}
