namespace CinemaBD.Api.Contracts.Common;

public record ApiResponse<T>(bool Success, string Message, T? Data);
