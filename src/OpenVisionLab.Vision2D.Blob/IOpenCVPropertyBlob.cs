using OpenVisionLab.Vision2D.Property;

namespace OpenVisionLab.Vision2D.Blob
{
    public interface IOpenCVPropertyBlob : IOpenCVPropertyBase
    {
        int MIN_AREA { get; set; }
        int MAX_AREA { get; set; }
    }
}
