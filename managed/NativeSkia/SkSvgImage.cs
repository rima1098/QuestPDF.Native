using System.Runtime.InteropServices;
using QuestPDF.Skia.Text;

namespace QuestPDF.Skia;

[StructLayout(LayoutKind.Sequential)]
internal struct SkSvgImageSize
{
    public float Width;
    public float Height;
    
    public Unit WidthUnit;
    public Unit HeightUnit;
    
    public enum Unit
    {
        Unknown,
        Number,
        Percentage,
        Pixels,
        Centimeters,
        Millimeters,
        Inches,
        Points,
        Picas, // 1 Pica = 12 points
    }
}

internal sealed class SkSvgImage : IDisposable
{
    public IntPtr Instance { get; private set; }
    public SkSvgImageSize Size;
    public SkRect ViewBox;
    
    public SkSvgImage(string svgString, SkFontManager fontManager)
    {
        using var data = SkData.FromBinary(System.Text.Encoding.UTF8.GetBytes(svgString));

        Instance = API.svg_create(data.Instance, fontManager.Instance);
        
        if (Instance == IntPtr.Zero)
            throw new Exception("Cannot decode the provided SVG image.");
        
        API.svg_get_size(Instance, out Size, out ViewBox);
    }
    
    internal float AspectRatio
    {
        get
        {
            if (Size.WidthUnit is SkSvgImageSize.Unit.Percentage || Size.HeightUnit is SkSvgImageSize.Unit.Percentage)
                return ViewBox.Width / ViewBox.Height;
        
            return Size.Width / Size.Height;
        }
    }
    
    ~SkSvgImage()
    {
        Dispose();
    }
    
    public void Dispose()
    {
        if (Instance == IntPtr.Zero)
            return;
        
        API.svg_unref(Instance);
        Instance = IntPtr.Zero;
    }
    
    private static class API
    {
        [DllImport(SkiaAPI.LibraryName)]
        public static extern IntPtr svg_create(IntPtr data, IntPtr fontManager);
        
        [DllImport(SkiaAPI.LibraryName)]
        public static extern void svg_unref(IntPtr svg);
        
        [DllImport(SkiaAPI.LibraryName)]
        public static extern void svg_get_size(IntPtr svg, out SkSvgImageSize size, out SkRect viewBox);
    }
}