using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiketixAPI.Models;

namespace TiketixAPI.Controllers
{
    [Route("api/movie")]
    [ApiController]
    public class MovieController : ControllerBase
    {
        private readonly TiketixContext _dB;
        public MovieController(TiketixContext dB) { _dB = dB; }

        [HttpGet("popular")]
        public async Task<IActionResult> FetchMostPopularMovie() // fetch most popular movie in this week
        {
            var oneWeekAgo = DateOnly.FromDateTime(DateTime.Parse("04-11-2024")).AddDays(-7);

            // Transactions in the last 7 days
            var recentTransactions = await _dB.TransactionDetails
                .Where(q => q.Transaction.Schedule.Date >= oneWeekAgo)
                .Include(q => q.Transaction.Schedule.Movie.MovieGenres)
                .ThenInclude(q => q.Genre)
                .ToListAsync();

            // No transactions at all
            if (!recentTransactions.Any())
            {
                // Nothing to return
                return NotFound("No popular movies found in the last 7 days");
            }

            // Group by movie
            var groupedMovies = recentTransactions
                .GroupBy(q => q.Transaction.Schedule.Movie)
                .Select(g => new
                {
                    Movie = g.Key,
                    Count = g.Count()
                })
                .ToList();

            var maxCount = groupedMovies.Max(g => g.Count); // in case there are multiple movies with the same amount ticket bought 
            var topMovies = groupedMovies
                .Where(q => q.Count == maxCount)
                .Select(q => new
                {
                     id = q.Movie.Id,
                     title = q.Movie.Title,
                     genre = q.Movie.MovieGenres?
                                 .OrderBy(g => g.Genre.Name)
                                 .Select(g => g.Genre.Name)
                                 .FirstOrDefault(), // order genre by alphabet
                     duration = q.Movie.Duration,
                     imagePath = q.Movie.Poster
                })
                .ToList();

            return Ok(topMovies);
        }

        [HttpGet("newlyreleased")]
        public async Task<IActionResult> FetchNewlyReleasedMovie()
        {
            var oneWeekAgo = DateOnly.FromDateTime(DateTime.Parse("04-11-2024")).AddDays(-7); // the date is static for test purpose

            var newlyReleasedMovies = await _dB.Movies
                    .Where(q => q.ReleaseDate >= oneWeekAgo)
                    .OrderByDescending(q => q.ReleaseDate)
                    .Include(q => q.MovieGenres)
                    .ThenInclude(q => q.Genre)
                    .ToListAsync();

            if (newlyReleasedMovies.Any())
                return Ok(newlyReleasedMovies.Select(q => new
                {
                    id = q.Id,
                    title = q.Title,
                    genre = q.MovieGenres?
                                 .OrderBy(g => g.Genre.Name)
                                 .Select(g => g.Genre.Name)
                                 .FirstOrDefault(), // order genre by alphabet
                    duration = q.Duration,
                    imagePath = q.Poster
                }));

            // Final fallback: nothing to return
            return NotFound("No newly released movies found in the last 7  last 7 days");
        }

        [HttpGet]
        public async Task<IActionResult> FetchAllMovies() // fetch all existing movies in database
        {
            var allMovies = await _dB.Movies.OrderByDescending(q => q.ReleaseDate).Include(q => q.MovieGenres).ThenInclude(q => q.Genre).ToListAsync();

            if (allMovies.Any())
            {
                return Ok(allMovies.Select(q => new
                {
                    id = q.Id,
                    title = q.Title,
                    genre = q.MovieGenres?
                                 .OrderBy(g => g.Genre.Name)
                                 .Select(g => g.Genre.Name)
                                 .FirstOrDefault(), // order genre by alphabet
                    duration = q.Duration,
                    imagePath = q.Poster
                }));
            }

            return NotFound("Movies not found");
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> FetchMovieById(int id) // fetch movie detail by ID
        {
            var selectedMovie = await _dB.Movies.Include(q => q.Schedules)
                .ThenInclude(s => s.Theater).Include(q => q.Schedules).ThenInclude(q => q.Transactions).ThenInclude(q => q.TransactionDetails).Include(q => q.MovieGenres).ThenInclude(q => q.Genre).FirstOrDefaultAsync(q => q.Id == id);

            if (selectedMovie == null) // if not found
            {
                return NotFound("Movie not found");
            }


            var today = DateOnly.FromDateTime(DateTime.Parse("04-11-2024")); // the date is static for test purpose
            var now = TimeOnly.FromDateTime(DateTime.Now);

            return Ok(new
            {
                id = selectedMovie.Id,
                title = selectedMovie.Title,
                desc = selectedMovie.Description,
                genres = selectedMovie.MovieGenres.OrderBy(q => q.Genre.Name).Select(q => q.Genre.Name), // genres ordered by alphabet
                duration = selectedMovie.Duration,
                imagePath = selectedMovie.Poster,
                scheduleDate = selectedMovie.Schedules.Where(q =>
                q.Date > today || (q.Date == today && q.Time >= now)).GroupBy(q => q.Theater).Select(q => new // the schedule grouped by the same theater, with any schedule after today or if it's today, then only those with a time after now
                {
                    theater = new
                    {
                        id = q.Key.Id,
                        name = q.Key.Name,
                        section = q.Key.Section,
                        col = q.Key.Column,
                        row = q.Key.Row,
                    },
                    dateTime = q.GroupBy(d => d.Date).Select(dt => new // after grouped by the same theater, it will be grouped by the same date
                    {
                        date = dt.Key,
                        time = dt.Select(t => new // in the same date, there might be multiple times
                        {
                            scheduleId = t.Id, // scheduleId based on the selected time
                            time = t.Time,
                        })
                    }),
                }),
            });
        }
    }
}
