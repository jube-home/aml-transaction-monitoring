using MessagePack;

namespace Jube.Preservation.Models
{
    [MessagePackObject]
    public class Wrapper
    {
        [Key(0)] public int Version { get; set; }

        [Key(2)] public Guid Guid { get; set; }
        [Key(3)] public Payload? Payload { get; set; }
    }
}