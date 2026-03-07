namespace Jube.App.Dto
{
    using System;

    public class EntityAnalysisModelProcessingCounterDto
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Instance { get; set; }
        public int ModelInvoke { get; set; }
        public int GatewayMatch { get; set; }
        public Guid EntityAnalysisModelGuid { get; set; }
        public int ResponseElevation { get; set; }
        public double ResponseElevationSum { get; set; }
        public double ActivationWatcher { get; set; }
        public int ResponseElevationLimit { get; set; }
        public long ModelTotalResponseTime { get; set; }
    }
}
