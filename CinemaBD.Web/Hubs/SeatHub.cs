using CinemaBD.Web.Services;
using Microsoft.AspNetCore.SignalR;

namespace CinemaBD.Web.Hubs;

public class SeatHub : Hub
{
    private readonly CinemaApiClient _apiClient;

    public SeatHub(CinemaApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task JoinShowtime(string showtimeId)
    {
        if (!string.IsNullOrWhiteSpace(showtimeId))
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(showtimeId));
    }

    public async Task LeaveShowtime(string showtimeId)
    {
        if (!string.IsNullOrWhiteSpace(showtimeId))
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(showtimeId));
    }

    public async Task HoldSeat(string showtimeId, string seatId, string seatCode)
    {
        if (string.IsNullOrWhiteSpace(showtimeId) || string.IsNullOrWhiteSpace(seatId))
            return;

        var token = Context.GetHttpContext()?.Session.GetString("UserToken");
        if (string.IsNullOrWhiteSpace(token))
        {
            await Clients.Caller.SendAsync("SeatHoldFailed", seatId, "Cần đăng nhập để giữ ghế.");
            return;
        }

        var result = await _apiClient.HoldSeatsAsync(token, showtimeId, new[] { seatId }, Context.ConnectionAborted);
        if (result?.Success != true)
        {
            await Clients.Caller.SendAsync("SeatHoldFailed", seatId, result?.Message ?? "Không giữ được ghế.");
            return;
        }

        await Clients.OthersInGroup(GroupName(showtimeId)).SendAsync("SeatHeld", seatId, seatCode, Context.ConnectionId);
        await Clients.Caller.SendAsync("SeatHoldConfirmed", seatId, result.ExpiresAt);
    }

    public async Task ReleaseSeat(string showtimeId, string seatId)
    {
        if (string.IsNullOrWhiteSpace(showtimeId) || string.IsNullOrWhiteSpace(seatId))
            return;

        var token = Context.GetHttpContext()?.Session.GetString("UserToken");
        if (!string.IsNullOrWhiteSpace(token))
            await _apiClient.ReleaseSeatsAsync(token, showtimeId, new[] { seatId }, Context.ConnectionAborted);

        await Clients.OthersInGroup(GroupName(showtimeId)).SendAsync("SeatReleased", seatId, Context.ConnectionId);
    }

    public async Task ConfirmSeats(string showtimeId, IReadOnlyList<string> seatIds)
    {
        if (string.IsNullOrWhiteSpace(showtimeId) || seatIds.Count == 0)
            return;

        await Clients.OthersInGroup(GroupName(showtimeId)).SendAsync("SeatsConfirmed", seatIds);
    }

    private static string GroupName(string showtimeId) => $"showtime:{showtimeId.Trim()}";
}
