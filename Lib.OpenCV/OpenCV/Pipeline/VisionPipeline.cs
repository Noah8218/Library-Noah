using System.Collections.Generic;
using System.Xml.Serialization;

namespace Lib.OpenCV.Pipeline
{
    [XmlRoot("VisionPipeline")]
    public class VisionPipeline
    {
        public string Name { get; set; } = string.Empty;

        [XmlArray("Steps")]
        [XmlArrayItem("Step")]
        public List<VisionPipelineStep> Steps { get; } = new List<VisionPipelineStep>();
    }
}
