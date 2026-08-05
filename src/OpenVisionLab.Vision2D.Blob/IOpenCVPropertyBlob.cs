using OpenVisionLab.Vision2D.Property;

namespace OpenVisionLab.Vision2D.Blob
{
    /// <summary>Defines common 2D preprocessing plus Blob-specific area limits.</summary>
    public interface IOpenCVPropertyBlob : IOpenCVPropertyBase
    {
        int MIN_AREA { get; set; }
        int MAX_AREA { get; set; }
    }
}
