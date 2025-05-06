using System;
using System.Collections.Generic;

namespace TiketixAPI.Models;

public partial class Theater
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int Section { get; set; }

    public int Column { get; set; }

    public int Row { get; set; }

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
}
