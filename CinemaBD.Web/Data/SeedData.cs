using CinemaBD.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Web.Data;

public static class SeedData
{
    public static async Task InitializeAsync(CinemaDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        if (!await db.Movies.AnyAsync())
        {
            var movies = new List<Movie>
            {
                new() { Id = "P001", Title = "Dune: Part Two", Genre = "Sci-Fi", DurationMinutes = 166, Director = "Denis Villeneuve", Cast = "Timothée Chalamet", Country = "US", Status = "Đang chiếu", PosterUrl = "/legacy/Content/img/Posters/dune.jpg", ReleaseDate = DateTime.Today.AddDays(-20), EndDate = DateTime.Today.AddDays(30), Description = "Bom tấn khoa học viễn tưởng." },
                new() { Id = "P002", Title = "Inside Out 2", Genre = "Animation", DurationMinutes = 97, Director = "Kelsey Mann", Cast = "Amy Poehler", Country = "US", Status = "Sắp chiếu", PosterUrl = "/legacy/Content/img/Posters/camxuc.jpg", ReleaseDate = DateTime.Today.AddDays(10), EndDate = DateTime.Today.AddDays(60), Description = "Câu chuyện cảm xúc mới." }
            };
            db.Movies.AddRange(movies);
        }

        if (!await db.Showtimes.AnyAsync())
        {
            db.Showtimes.AddRange(
                new Showtime { Id = "SC001", MovieId = "P001", RoomId = "R1", RoomName = "Phòng 1", ShowDate = DateTime.Today, StartTime = "09:00", TicketPrice = 50000 },
                new Showtime { Id = "SC002", MovieId = "P001", RoomId = "R2", RoomName = "Phòng 2", ShowDate = DateTime.Today, StartTime = "14:00", TicketPrice = 60000 },
                new Showtime { Id = "SC003", MovieId = "P001", RoomId = "R3", RoomName = "Phòng 3", ShowDate = DateTime.Today.AddDays(1), StartTime = "19:00", TicketPrice = 70000 }
            );
        }

        if (!await db.Seats.AnyAsync())
        {
            var seats = new List<Seat>();
            var showtimes = await db.Showtimes.ToListAsync();
            foreach (var st in showtimes)
            {
                foreach (var row in new[] { "A", "B", "C", "D", "E" })
                {
                    for (int col = 1; col <= 10; col++)
                    {
                        var seatType = row is "D" or "E" ? "VIP" : "Thường";
                        var extra = seatType == "VIP" ? 20000 : 0;
                        seats.Add(new Seat
                        {
                            Id = $"{st.Id}_{row}{col}",
                            ShowtimeId = st.Id,
                            Row = row,
                            Column = col,
                            SeatType = seatType,
                            IsBooked = false,
                            Price = st.TicketPrice + extra
                        });
                    }
                }
            }
            db.Seats.AddRange(seats);
        }

        if (!await db.Combos.AnyAsync())
        {
            db.Combos.AddRange(
                new Combo { Id = "C001", Name = "Combo 1", Price = 65000, Description = "1 bắp + 1 nước", ImageUrl = "/legacy/Content/img/combo1.jpg" },
                new Combo { Id = "C002", Name = "Combo 2", Price = 85000, Description = "1 bắp lớn + 2 nước", ImageUrl = "/legacy/Content/img/combo2.jpg" },
                new Combo { Id = "C003", Name = "Combo 3", Price = 120000, Description = "2 bắp + 2 nước", ImageUrl = "/legacy/Content/img/combo3.jpg" }
            );
        }

        if (!await db.Users.AnyAsync())
        {
            db.Users.AddRange(
                new User
                {
                    Id = "U0001",
                    FullName = "Demo User",
                    Username = "demo",
                    PasswordHash = "demo123",
                    Email = "demo@cinemabd.local",
                    PhoneNumber = "0900000000"
                },
                new User
                {
                    Id = "UADMIN",
                    FullName = "Cinema Admin",
                    Username = "admin",
                    PasswordHash = "admin123",
                    Email = "admin@cinemabd.local",
                    PhoneNumber = "0900000001"
                }
            );
        }

        await db.SaveChangesAsync();
    }
}


