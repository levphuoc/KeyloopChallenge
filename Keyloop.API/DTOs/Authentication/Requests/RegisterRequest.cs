namespace Keyloop.API.DTOs.Authentication.Requests;

public sealed record RegisterRequest(string Email, string Password, string DisplayName);
