namespace CinemaBD.Api.Contracts.Admin;

public record AdminShowtimeUpsertRequest(string MovieId, string RoomId, DateTime ShowDate, string StartTime, decimal TicketPrice);
