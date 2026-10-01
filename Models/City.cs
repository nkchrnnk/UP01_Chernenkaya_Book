using System;
using System.Collections.Generic;

namespace UP01_Chernenkaya_Book.Models;

public partial class City
{
    public int CityId { get; set; }

    public string CityName { get; set; } = null!;

    public virtual ICollection<Accommodation> Accommodations { get; set; } = new List<Accommodation>();
}
