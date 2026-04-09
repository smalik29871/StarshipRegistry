namespace StarshipRegistry.Models.ViewModels
{
    public class StarshipDetailsViewModel
    {
        public Starship Starship { get; set; } = new();
        public Dictionary<string, string> FilmNames { get; set; } = new();
        public Dictionary<string, string> PilotNames { get; set; } = new();
    }

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
}
