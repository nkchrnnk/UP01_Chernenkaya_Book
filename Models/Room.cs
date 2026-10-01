using System;
using System.Collections.Generic;

namespace UP01_Chernenkaya_Book.Models;

public partial class Room
{
    public int RoomId { get; set; }

    public int AccommodationId { get; set; }

    public string RoomNumber { get; set; } = null!;

    public int Floor { get; set; }

    public string RoomType { get; set; } = null!;

    public int Capacity { get; set; }

    public decimal PricePerNight { get; set; }

    public bool IsAvailable { get; set; }

    public virtual Accommodation Accommodation { get; set; } = null!;

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
