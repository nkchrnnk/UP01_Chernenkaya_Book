using System;
using System.Collections.Generic;

namespace UP01_Chernenkaya_Book.Models;

public partial class AccommodationType
{
    public int TypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public virtual ICollection<Accommodation> Accommodations { get; set; } = new List<Accommodation>();
}
