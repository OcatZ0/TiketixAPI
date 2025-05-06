using System;
using System.Collections.Generic;

namespace TiketixAPI.Models;

public partial class Schedule
{
    public int Id { get; set; }

    public int MovieId { get; set; }

    public int TheaterId { get; set; }

    public DateOnly Date { get; set; }

    public TimeOnly Time { get; set; }

    public double Price { get; set; }

    public virtual Movie Movie { get; set; } = null!;

    public virtual Theater Theater { get; set; } = null!;

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
