namespace ExternalApiIntegration.Models
{
    public class UserListResponse
    {
        public int Page { get; set; }
        public int Total_Pages { get; set; }
        public List<User> Data { get; set; } = new();
    }
}
