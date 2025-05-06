using System;
using System.Collections.Generic;

namespace TiketixAPI.Models;

public partial class Movie
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public int Duration { get; set; }

    public DateOnly ReleaseDate { get; set; }

    public string Poster { get; set; } = null!;

    public virtual ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
}
