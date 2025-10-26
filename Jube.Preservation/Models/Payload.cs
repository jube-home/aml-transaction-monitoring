using Jube.Data.Poco;
using MessagePack;

namespace Jube.Preservation.Models
{
    [MessagePackObject]
    public class Payload
    {
        [Key(0)] public IEnumerable<EntityAnalysisModel>? EntityAnalysisModel { get; set; }

        [Key(1)] public IEnumerable<VisualisationRegistry>? VisualisationRegistry { get; set; }
    }
}