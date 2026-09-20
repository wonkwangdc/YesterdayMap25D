namespace YesterdayMap.Exploration
{
    public sealed class ExplorationResult
    {
        public string Summary { get; }
        public bool WasDangerous { get; }

        public ExplorationResult(string summary, bool wasDangerous)
        {
            Summary = summary;
            WasDangerous = wasDangerous;
        }
    }
}
