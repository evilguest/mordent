using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mordent.WebAPI.Models
{
    public record Table(Guid Id, string Name, IReadOnlyList<Field> Fields);
    
}
