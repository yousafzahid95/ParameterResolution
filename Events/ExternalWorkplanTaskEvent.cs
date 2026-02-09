namespace AntlrTest1.Events
{
    public class ExternalWorkplanTaskEvent
    {
        public WorkplanTaskData WorkplanTask { get; set; } = new();
    }

    public class WorkplanTaskData
    {
        public Guid ProjectId { get; set; }
        public Guid WorkAreaId { get; set; }
        public List<WorkplanTaskEntity>? Entities { get; set; }
    }

    public class WorkplanTaskEntity
    {
        public Guid WorkAreaEntityId { get; set; }
    }
}
