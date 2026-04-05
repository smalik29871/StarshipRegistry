namespace StarshipRegistry.Models.ViewModels
{
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
