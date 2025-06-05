namespace IoTMonitoring.Api.Data.Models
{
    public class DataPurgeHistory
    {
        public long PurgeID { get; set; }
        public DateTime ExecutionTime { get; set; }
        public string TableName { get; set; }
        public int RowsDeleted { get; set; }
        public int RetentionDays { get; set; }
        public DateTime CutoffDate { get; set; }
    }
}