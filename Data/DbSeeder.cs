using CemaApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CemaApp.Data
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // 1. Seed Roles
            string[] roleNames = { "Admin", "User" };
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 2. Seed Admin User
            string adminEmail = "admin@cema.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Administrator Hola",
                    DateOfBirth = new DateTime(2004, 1, 1),
                    EmailConfirmed = true
                };

                var createPowerUser = await userManager.CreateAsync(adminUser, "Admin@123");
                if (createPowerUser.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }

        public static async Task SeedSampleDataAsync(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            // 1. Check if we've already seeded our specific dummy data
            if (context.Movies.Any(m => m.Title == "Dune: Part Two"))
            {
                return; // Already seeded, skip everything!
            }

            // 2. WIPE EXISTING DATA (Respecting Foreign Key constraints)
            context.BookingSeats.RemoveRange(context.BookingSeats);
            context.Bookings.RemoveRange(context.Bookings);
            context.Screenings.RemoveRange(context.Screenings);
            context.Movies.RemoveRange(context.Movies);
            context.Seats.RemoveRange(context.Seats);
            context.Halls.RemoveRange(context.Halls);
            
            var allUsers = await userManager.Users.ToListAsync();
            foreach (var u in allUsers)
            {
                if (u.Email != "admin@cema.com")
                {
                    await userManager.DeleteAsync(u);
                }
            }
            await context.SaveChangesAsync();

            // 3. SEED USERS
            var users = new List<ApplicationUser>();
            string[] testEmails = { "john@cema.com", "sarah@cema.com", "mike@cema.com", "emma@cema.com" };
            foreach (var email in testEmails)
            {
                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = "Test User " + email.Split('@')[0],
                    DateOfBirth = new DateTime(1990, 1, 1),
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(user, "User@123");
                await userManager.AddToRoleAsync(user, "User");
                users.Add(user);
            }

            // 4. SEED HALLS AND SEATS
            var halls = new List<Hall>
            {
                new Hall { Name = "IMAX 1", TotalRows = 10, SeatsPerRow = 15 },
                new Hall { Name = "Standard 2", TotalRows = 8, SeatsPerRow = 10 },
                new Hall { Name = "VIP Lounge", TotalRows = 5, SeatsPerRow = 8 }
            };
            context.Halls.AddRange(halls);
            await context.SaveChangesAsync();

            foreach (var hall in halls)
            {
                char[] rowLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
                for (int r = 0; r < hall.TotalRows; r++)
                {
                    for (int s = 1; s <= hall.SeatsPerRow; s++)
                    {
                        context.Seats.Add(new Seat
                        {
                            HallId = hall.Id,
                            Row = rowLetters[r].ToString(),
                            Number = s
                        });
                    }
                }
            }
            await context.SaveChangesAsync();

            // 5. SEED MOVIES
            var movies = new List<Movie>
            {
                new Movie { Title = "Dune: Part Two", Genre = "Sci-Fi", DurationMinutes = 166, ReleaseDate = DateTime.Now.AddDays(-10), Description = "Paul Atreides unites with Chani and the Fremen while on a warpath of revenge against the conspirators who destroyed his family.", IsActive = true },
                new Movie { Title = "Oppenheimer", Genre = "Drama", DurationMinutes = 180, ReleaseDate = DateTime.Now.AddDays(-30), Description = "The story of American scientist, J. Robert Oppenheimer, and his role in the development of the atomic bomb.", IsActive = true },
                new Movie { Title = "The Matrix", Genre = "Sci-Fi", DurationMinutes = 136, ReleaseDate = DateTime.Now.AddDays(-100), Description = "A computer hacker learns from mysterious rebels about the true nature of his reality.", IsActive = true },
                new Movie { Title = "John Wick: Chapter 4", Genre = "Action", DurationMinutes = 169, ReleaseDate = DateTime.Now.AddDays(-5), Description = "John Wick uncovers a path to defeating The High Table.", IsActive = true },
                new Movie { Title = "Interstellar", Genre = "Sci-Fi", DurationMinutes = 169, ReleaseDate = DateTime.Now.AddDays(-200), Description = "A team of explorers travel through a wormhole in space in an attempt to ensure humanity's survival.", IsActive = true }
            };
            context.Movies.AddRange(movies);
            await context.SaveChangesAsync();

            // 6. SEED SCREENINGS
            var screenings = new List<Screening>();
            Random rand = new Random();
            foreach (var movie in movies)
            {
                for (int i = 0; i < 3; i++) // 3 screenings per movie
                {
                    var hall = halls[rand.Next(halls.Count)];
                    screenings.Add(new Screening
                    {
                        MovieId = movie.Id,
                        HallId = hall.Id,
                        StartTime = DateTime.Now.AddDays(rand.Next(1, 7)).AddHours(rand.Next(10, 22)),
                        Price = 15.00m
                    });
                }
            }
            context.Screenings.AddRange(screenings);
            await context.SaveChangesAsync();

            // 7. SEED BOOKINGS
            var allSeats = await context.Seats.ToListAsync();
            foreach (var user in users)
            {
                int numberOfBookings = rand.Next(1, 4);
                for (int i = 0; i < numberOfBookings; i++)
                {
                    var screening = screenings[rand.Next(screenings.Count)];
                    var booking = new Booking
                    {
                        UserId = user.Id,
                        ScreeningId = screening.Id,
                        BookingDate = DateTime.Now.AddDays(-rand.Next(1, 5)),
                        TotalPrice = screening.Price * 2, // Assume 2 seats
                        Status = (BookingStatus)rand.Next(0, 3) // 0: Pending, 1: Confirmed, 2: Cancelled
                    };
                    context.Bookings.Add(booking);
                    await context.SaveChangesAsync();

                    // Add 2 random seats to this booking
                    var hallSeats = allSeats.Where(s => s.HallId == screening.HallId).ToList();
                    if (hallSeats.Count >= 2)
                    {
                        for (int s = 0; s < 2; s++)
                        {
                            var seat = hallSeats[rand.Next(hallSeats.Count)];
                            context.BookingSeats.Add(new BookingSeat
                            {
                                BookingId = booking.Id,
                                SeatId = seat.Id
                            });
                        }
                        await context.SaveChangesAsync();
                    }
                }
            }
        }
    }
}