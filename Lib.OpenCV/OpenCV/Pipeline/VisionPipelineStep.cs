using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace Lib.OpenCV.Pipeline
{
    public class VisionPipelineParameter
    {
        public VisionPipelineParameter()
        {
        }

        public VisionPipelineParameter(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class VisionPipelineStep
    {
        public string Name { get; set; } = string.Empty;
        public string ToolType { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public string InputLayer { get; set; } = string.Empty;
        public string OutputLayer { get; set; } = string.Empty;
        public bool UseAcceptance { get; set; }
        public bool ExpectedSuccess { get; set; } = true;
        public double MaxElapsedMilliseconds { get; set; }
        public string RequiredMessageText { get; set; } = string.Empty;
        public string AcceptanceMetricName { get; set; } = string.Empty;
        public bool UseAcceptanceMetricMinimum { get; set; }
        public double AcceptanceMetricMinimum { get; set; }
        public bool UseAcceptanceMetricMaximum { get; set; }
        public double AcceptanceMetricMaximum { get; set; }

        [XmlIgnore]
        public Dictionary<string, string> Parameters { get; } = new Dictionary<string, string>();

        [XmlArray("Parameters")]
        [XmlArrayItem("Parameter")]
        public VisionPipelineParameter[] XmlParameters
        {
            get => Parameters
                .Select(parameter => new VisionPipelineParameter(parameter.Key, parameter.Value))
                .ToArray();
            set
            {
                Parameters.Clear();
                if (value == null) { return; }

                foreach (VisionPipelineParameter parameter in value)
                {
                    if (parameter == null || string.IsNullOrWhiteSpace(parameter.Key)) { continue; }
                    Parameters[parameter.Key] = parameter.Value ?? string.Empty;
                }
            }
        }
    }
}
