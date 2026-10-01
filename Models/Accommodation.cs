using System;
using System.Collections.Generic;

namespace UP01_Chernenkaya_Book.Models;

public partial class Accommodation
{
    public int AccommodationId { get; set; }

    public string Name { get; set; } = null!;

    public int TypeId { get; set; }

    public int CityId { get; set; }

    public string Address { get; set; } = null!;

    public int Rooms { get; set; }

    public int Capacity { get; set; }

    public decimal PricePerNight { get; set; }

    public decimal? Rating { get; set; }

    public string? Description { get; set; }

    public string? Photo { get; set; }

    public virtual City City { get; set; } = null!;

    public virtual ICollection<Room> RoomsNavigation { get; set; } = new List<Room>();

    public virtual AccommodationType Type { get; set; } = null!;
}
