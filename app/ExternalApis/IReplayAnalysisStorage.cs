using System.Threading.Tasks;

namespace FortniteReplayAnalyzer.ExternalApis
{
    public interface IReplayAnalysisStorage
    {
        Task UploadJson(string guid, string json);
        Task<string> ReadJsonDataAsync(string guid);
    }
}