public class Alert{
    public Guid Id {get; set; } = "";
    public string Title {get; set; } = "";
    public string Description {get; set; } = "";
    public string Severity {get; set; } = "medium";
    public string SounrceIp {get; set; } = "";
    public string MitreAttackTactic {get; set; } = "";
    public DateTime CreatedAt {get; set;} = DateTime.UtcNow;
    public bool Acknowledged {get; set; } = false; 
}