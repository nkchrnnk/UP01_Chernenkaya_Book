using System;
using System.Collections.Generic;

namespace UP01_Chernenkaya_Book.Models;

public partial class Booking
{
    public int BookingId { get; set; }

    public int UserId { get; set; }

    public int RoomId { get; set; }

    public int StatusId { get; set; }

    public DateOnly CheckIn { get; set; }

    public DateOnly CheckOut { get; set; }

    public int Guests { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? Comment { get; set; }

    public virtual ICollection<BookingService> BookingServices { get; set; } = new List<BookingService>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Room Room { get; set; } = null!;

    public virtual BookingStatus Status { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
