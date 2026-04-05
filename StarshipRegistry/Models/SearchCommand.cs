namespace StarshipRegistry.Models
{
    public class SearchCommand
    {
        public string Concept { get; set; } = "";
        public string SortBy { get; set; } = "";
        public string Order { get; set; } = "asc";
        public int Take { get; set; } = 10;
    }
}