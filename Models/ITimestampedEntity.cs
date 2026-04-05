using System;

namespace StarshipRegistry.Models
{
    public interface ITimestampedEntity
    {
        DateTime? Created { get; set; }
        DateTime? Edited { get; set; }
    }
}