using static OpenVisionLab.Vision2D.Tool.MeanTool;

namespace OpenVisionLab.Vision2D.Property
{
    public interface IOpenCVPropertyMean : IOpenCVPropertyBase
    {            
        int MEAN_MAX { get; set; }         
        int MEAN_MIN { get; set; } 
        MeanType MEAN_TYPES { get; set; } 
    }
}
