using Newtonsoft.Json.Linq;

namespace AntlrTest1.Interfaces
{
    public interface IDataRecord
    {
        JObject Data { get; set; }
        string Source { get; set; }
    }

    public class DataRecord : IDataRecord
    {
        public JObject Data { get; set; } = new();
        public string Source { get; set; } = string.Empty;
    }
}
