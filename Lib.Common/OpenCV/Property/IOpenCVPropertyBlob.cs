namespace Lib.OpenCV.Property
{
    public interface IOpenCVPropertyBlob : IOpenCVPropertyBase
    {
        int MIN_AREA { get; set; }
        int MAX_AREA { get; set; }
    }
}
