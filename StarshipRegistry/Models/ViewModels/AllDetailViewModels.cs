namespace StarshipRegistry.Models.ViewModels
{
    public class FilmDetailsViewModel
    {
        public Film Film { get; set; } = new();
        public Dictionary<string, string> CharacterNames { get; set; } = new();
        public Dictionary<string, string> PlanetNames { get; set; } = new();
        public Dictionary<string, string> StarshipNames { get; set; } = new();
        public Dictionary<string, string> VehicleNames { get; set; } = new();
        public Dictionary<string, string> SpeciesNames { get; set; } = new();
    }

    public class PeopleDetailsViewModel
    {
        public Character Character { get; set; } = new();
        public Dictionary<string, string> FilmNames { get; set; } = new();
        public Dictionary<string, string> StarshipNames { get; set; } = new();
    }

    public class SpeciesDetailsViewModel
    {
        public Species Species { get; set; } = new();
        public Dictionary<string, string> CharacterNames { get; set; } = new();
        public Dictionary<string, string> FilmNames { get; set; } = new();
    }

    public class VehicleDetailsViewModel
    {
        public Vehicle Vehicle { get; set; } = new();
        public Dictionary<string, string> PilotNames { get; set; } = new();
        public Dictionary<string, string> FilmNames { get; set; } = new();
    }

    public class PlanetDetailsViewModel
    {
        public Planet Planet { get; set; } = new();
        public Dictionary<string, string> FilmNames { get; set; } = new();
    }

    /// <summary>
    /// View model for Starship details with pre-loaded relationship names.
    /// This eliminates per-item API calls by loading all related entity names in the controller.
    /// </summary>
    public class StarshipDetailsViewModel
    {
        /// <summary>
        /// The starship entity itself.
        /// </summary>
        public Starship Starship { get; set; } = new();

        /// <summary>
        /// Dictionary mapping film URLs to their titles (pre-loaded in controller).
        /// </summary>
        public Dictionary<string, string> FilmNames { get; set; } = new();

        /// <summary>
        /// Dictionary mapping character URLs to their names (pre-loaded in controller).
        /// </summary>
        public Dictionary<string, string> PilotNames { get; set; } = new();
    }
}
