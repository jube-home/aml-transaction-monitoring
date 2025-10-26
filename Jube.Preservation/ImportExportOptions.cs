namespace Jube.Preservation
{
    public class ImportExportOptions
    {
        public string? Password { get; set; }
        public bool Exhaustive { get; set; }
        public bool Suppressions { get; set; }
        public bool Lists { get; set; }
        public bool Dictionaries { get; set; }
        public bool Visualisations { get; set; }
    }
}