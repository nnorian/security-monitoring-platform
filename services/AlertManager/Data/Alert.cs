public class Alert
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Severity { get; set; } = "medium";
    public string SourceIp { get; set; } = "";
    public string MitreAttackTactic { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool Acknowledged { get; set; } = false;
}
