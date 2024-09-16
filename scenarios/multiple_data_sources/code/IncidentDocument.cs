public class IncidentDocument : PartitionedEntity
{
    private string _id;
    public string? Id
    { 
        get => _id ?? $"{Type}_{IncidentNumber}";
        set => _id = value;
    }
    public string IncidentNumber { get; set; }
    public string ShortDescription { get; set; }
    public string Caller { get; set; }
    public string Priority { get; set; }
    public string State { get; set; }
    public string AssignmentGroup { get; set; }
    public string AssignedTo { get; set; }
    public string BusinessDuration { get; set; }
    public string BusinessResolveTime { get; set; }
    public string Category { get; set; }
    public string Comments { get; set; }
    public string Description { get; set; }
    public string Duration { get; set; }
    public string Impact { get; set; }
    public string IncidentState { get; set; }
    public string Severity { get; set; }
    public string Subcategory { get; set; }
    public string WorkNotes { get; set; }
    public bool? Active { get; set; }
    public string Channel { get; set; }
    public string Company { get; set; }
    public string Escalation { get; set; }
    public string Location { get; set; }
    public string Problem { get; set; }
    public string Resolved { get; set; }
    public string ResolvedBy { get; set; }
    public string TaskType { get; set; }
    public string Urgency { get; set; }
    public string GetIncidentWebUrl(string myServiceNow) => $"https://{myServiceNow}.service-now.com/incident.do?sysparm_query=number={IncidentNumber}";
}