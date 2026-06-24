namespace Application.DTOs.Users
{
    public class LoginResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public UserDto User { get; set; } = new();
    }
}
