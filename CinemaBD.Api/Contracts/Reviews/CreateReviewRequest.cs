namespace CinemaBD.Api.Contracts.Reviews;

public record CreateReviewRequest(string MovieId, string Content, int Rating = 5);
