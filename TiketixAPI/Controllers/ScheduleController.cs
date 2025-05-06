using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiketixAPI.Models;

namespace TiketixAPI.Controllers
{
    [Route("api/schedule")]
    [ApiController]
    public class ScheduleController : ControllerBase
    {
        private readonly TiketixContext _dB;
        public ScheduleController(TiketixContext dB) { _dB = dB; }

        [HttpGet("{id}/seatprice")]
        public async Task<IActionResult> FetchScheduleSeatPriceById(int id)
        {
            var selectedSchedule = await _dB.Schedules.Include(q => q.Transactions).ThenInclude(q => q.TransactionDetails).FirstOrDefaultAsync(q => q.Id == id);

            if (selectedSchedule == null)
            {
                return NotFound("Movie not found");
            }

            return Ok(new
            {
                unavailableSeats = selectedSchedule.Transactions.Select(q => q.TransactionDetails.Select(s => s.Seat)),
                price = selectedSchedule.Price,
            });
        }
    }
}
