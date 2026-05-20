namespace CinemaBD.Api.Contracts.Reviews;

public record ReviewResponse(
    int Id,
    string MovieId,
    string CustomerId,
    string Content,
    int Rating,
    bool IsHidden,
    bool CanReview,
    string? ReviewRuleMessage,
    DateTime CreatedAt,
    string? MovieTitle,
    string? CustomerName
);
