using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiketixAPI.Models;

namespace TiketixAPI.Controllers
{
    [Route("api/user")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly TiketixContext _dB;
        public UserController(TiketixContext dB) { _dB = dB; }

        [HttpGet("{id}/transaction")]
        public async Task<IActionResult> FetchTicketsByUserID(int id)
        {
            if (!await _dB.Users.AnyAsync(q => q.Id == id)) // validate user
            {
                return NotFound("User not found");
            }

            var today = DateTime.Parse("04-11-2024").Date.Add(DateTime.Now.TimeOfDay); ; // the date is static for test purpose

            var allUserTickets = await _dB.Transactions.Include(q => q.TransactionDetails).Include(q => q.Schedule.Movie).Where(q => q.UserId == id).ToListAsync();
            var userTickets = allUserTickets
                .Where(q => today <= q.Schedule.Date.ToDateTime(q.Schedule.Time).AddMinutes(q.Schedule.Movie.Duration)).ToList(); // on going or not started yet

            return Ok(userTickets.Select(q => new
            {
                id = q.Id,
                movie = new
                {
                    title = q.Schedule.Movie.Title,
                    poster = q.Schedule.Movie.Poster,
                },
                quantity = q.TransactionDetails.Count(),
                schedule = q.Schedule.Date.ToDateTime(q.Schedule.Time).ToString("dd MMM yyyy HH:mm"),
                seat = q.TransactionDetails.Select(td => td.Seat).ToList() is var seats
                    ? seats.Count switch
                    {
                        0 => "",
                        1 => seats[0],
                        2 => $"{seats[0]} & {seats[1]}",
                        _ => string.Join(", ", seats.Take(seats.Count - 1)) + " & " + seats.Last()
                    }
                    : "",
                price = q.TransactionDetails.Sum(td => td.Price)
            }));
        }
    }
}
