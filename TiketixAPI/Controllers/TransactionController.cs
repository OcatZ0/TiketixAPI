using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiketixAPI.Models;
using TiketixAPI.Models.DTO;

namespace TiketixAPI.Controllers
{
    [Route("api/transaction")]
    [ApiController]
    public class TransactionController : ControllerBase
    {
        private readonly TiketixContext _dB;
        public TransactionController(TiketixContext dB) { _dB = dB; }

        [HttpPost]
        public async Task<IActionResult> CreateNewTransaction(TransactionDTO transactionDTO)
        {
            if (!ModelState.IsValid) // validate DTO
            {
                return BadRequest(ModelState);
            }

            if (!await _dB.Users.AnyAsync()) // validate user
            {
                return NotFound("User not found");
            }

            var selectedSchedule = await _dB.Schedules.Include(q => q.Theater).FirstOrDefaultAsync(q => q.Id == transactionDTO.scheduleID);
            
            if (selectedSchedule == null) // validate schedule
            {
                return NotFound("Schedule not found");
            }


            var today = DateOnly.FromDateTime(DateTime.Parse("04-11-2024")); // the date is static for test purpose
            var now = TimeOnly.FromDateTime(DateTime.Now);

            if (selectedSchedule.Date < today || (selectedSchedule.Date == today && selectedSchedule.Time <= now)) // any schedule before today or if it's today, then only those with a time before now
            {
                return BadRequest("Schedule has already passed.");
            }

            var allTransactionDetails = await _dB.TransactionDetails
                    .Include(td => td.Transaction)
                    .ToListAsync();
            var takenSeats = allTransactionDetails
                    .Where(td => td.Transaction.ScheduleId == transactionDTO.scheduleID && transactionDTO.seats.Contains(td.Seat))
                    .Select(td => td.Seat)
                    .ToList();

            if (takenSeats.Any()) // validate taken seats
            {
                return BadRequest($"These seat(s) are already taken");
            }
            if (transactionDTO.seats.Distinct().Count() != transactionDTO.seats.Count) // validate duplicate seats
                return BadRequest("Duplicate seats are not allowed.");

            var theater = selectedSchedule.Theater;
            int maxRows = theater.Row;       
            int maxCols = theater.Column;

            // Generate valid seat codes
            var validSeats = new HashSet<string>();
            for (int c = 0; c < maxCols; c++)
            {
                char columnLetter = (char)('A' + c);
                for (int r = 1; r <= maxRows; r++)
                {
                    validSeats.Add($"{columnLetter}{r}");
                }
            }

            // Validate each seat
            var invalidSeats = transactionDTO.seats
                .Where(seat => !validSeats.Contains(seat.ToUpper()))
                .ToList();

            if (invalidSeats.Any()) // validate invalid seat
            {
                return BadRequest($"Invalid seat(s): {string.Join(", ", invalidSeats)}");
            }

            // Actual Transaction Below //

            var newTransaction = new Transaction
            {
                UserId = transactionDTO.userID,
                ScheduleId = transactionDTO.scheduleID,
                TransactionDate = DateTime.Now,
            };

            _dB.Transactions.Add(newTransaction);
            await _dB.SaveChangesAsync();

            foreach (var seat in transactionDTO.seats)
            {
                var newTransactionDetail = new TransactionDetail
                {
                    TransactionId = newTransaction.Id,
                    Seat = seat,
                    Price = selectedSchedule.Price
                };

                _dB.TransactionDetails.Add(newTransactionDetail);
            }
            await _dB.SaveChangesAsync();

            return Created();
        }
    }
}
