using System;
using System.Collections.Generic;

namespace UP01_Chernenkaya_Book.Models;

public partial class BookingService
{
    public int BookingId { get; set; }

    public int ServiceId { get; set; }

    public int Quantity { get; set; }

    public decimal PriceAtBooking { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual Service Service { get; set; } = null!;
}
