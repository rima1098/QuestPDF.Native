using System.Diagnostics;
using FluentAssertions;
using NUnit.Framework;
using QuestPDF.Skia;
using QuestPDF.Skia.Text;

namespace NativeSkia.Tests;

public class ParagraphTests
{
    [Test]
    public void DrawParagraphOnPdfCanvas()
    {
        var typefaceProvider = new SkTypefaceProvider();
        using var monoTypefaceData = SkData.FromFile("input/JetBrainsMono-Regular.ttf");
        typefaceProvider.AddTypefaceFromData(monoTypefaceData, "JetBrains Mono");
        
        using var latoTypefaceData = SkData.FromFile("input/Lato/Lato-Light.ttf");
        typefaceProvider.AddTypefaceFromData(latoTypefaceData);
        
        // build simple paragraph;
        var fontCollection = SkFontCollection.Create(typefaceProvider, SkFontManager.Global);   
 
        var paragraphStyleConfiguration = new ParagraphStyleConfiguration
        {
            Alignment = ParagraphStyleConfiguration.TextAlign.Justify,
            Direction = ParagraphStyleConfiguration.TextDirection.Ltr,
            MaxLinesVisible = 40
        };
        
        using var paragraphBuilder = SkParagraphBuilder.Create(paragraphStyleConfiguration, fontCollection);

        var textStyleConfiguration = new TextStyleConfiguration
        {
            FontSize = 18,
            FontWeight = TextStyleConfiguration.FontWeights.Normal,
            
            FontFamilies = GetFontFamilyPointers("Times New Roman"),
            
            BackgroundColor = 0x00000000,
            ForegroundColor = 0xFF000000,
            
            DecorationColor = 0xFF009688,
            DecorationStyle = TextStyleConfiguration.TextDecorationStyle.Solid,
            DecorationType = TextStyleConfiguration.TextDecoration.NoDecoration,
            DecorationMode = TextStyleConfiguration.TextDecorationMode.Gaps, 
            
            LineHeight = 1.25f,
            WordSpacing = 0,
            LetterSpacing = 0
        };
        
        using var textStyle = new SkTextStyle(textStyleConfiguration);
        using var textStyleUnderline = new SkTextStyle(textStyleConfiguration with { DecorationType = TextStyleConfiguration.TextDecoration.Underline });
        
        using var monoTypefaceStyle = new SkTextStyle(textStyleConfiguration with { FontFamilies = GetFontFamilyPointers("JetBrains Mono"), BackgroundColor = 0x33000000 });
        using var latoTypefaceStyle = new SkTextStyle(textStyleConfiguration with { FontFamilies = GetFontFamilyPointers("Lato"), BackgroundColor = 0x22000000 });
        
        paragraphBuilder.AddText("Lorem Ipsum is simply \ndummy text of the printing and typesetting industry. ", textStyle);
        paragraphBuilder.AddText("🥰😅🥰🥰🥰 ", textStyle);
        paragraphBuilder.AddText("Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book.", textStyle);
        paragraphBuilder.AddPlaceholder(new SkPlaceholderStyle { Width = 75, Height = 25 });
        paragraphBuilder.AddText("هو حول الثقيل بالجانب. (مدن) مع 123 شدّت تمهيد. ", textStyle);
        paragraphBuilder.AddText("It has survived not only five centuries, but also the leap into electronic typesetting, remaining essentially unchanged. ", latoTypefaceStyle);
        paragraphBuilder.AddText("var input = calculate(5, myObject);", monoTypefaceStyle);
        paragraphBuilder.AddPlaceholder(new SkPlaceholderStyle { Width = 100, Height = 20 });
        paragraphBuilder.AddText("It was popularised in the 1960s with the release of Letraset sheets containing Lorem Ipsum passages, and more recently with desktop publishing software like Aldus PageMaker including versions of Lorem Ipsum.", textStyle);
        
        var paragraph = paragraphBuilder.CreateParagraph();
        
        // draw paragraph
        using var stream = new SkWriteStream();
        using var pdf = SkPdfDocument.Create(stream, new SkPdfDocumentMetadata());
        
        // draw text
        using var pageCanvas = pdf.BeginPage(500, 800);
        
        pageCanvas.Translate(50, 50);
        paragraph.PlanLayout(400);
        pageCanvas.DrawParagraph(paragraph);
        
        // draw placeholders
        var placeholderPositions = paragraph.GetPlaceholderPositions();
        
        foreach (var placeholderPosition in placeholderPositions)
        {
            pageCanvas.DrawFilledRectangle(placeholderPosition, 0x8800FF00);
        }
        
        pdf.EndPage();
        pdf.Close();

        using var documentData = stream.DetachData();
        TestFixture.SaveOutput("document_paragraph.pdf", documentData);
        
        var documentSize = documentData.ToBytes().Length;
        documentSize.Should().BeLessThan(300_000);
    }
    
    [Test]
    public void GetLineHeights()
    {
        using var typefaceProvider = new SkTypefaceProvider();
        using var fontCollection = SkFontCollection.Create(typefaceProvider, SkFontManager.Global);

        var paragraphStyleConfiguration = new ParagraphStyleConfiguration
        {
            Direction = ParagraphStyleConfiguration.TextDirection.Ltr,
            MaxLinesVisible = 1000
        };
        
        using var paragraphBuilder = SkParagraphBuilder.Create(paragraphStyleConfiguration, fontCollection);

        using var textStyle = new SkTextStyle(new TextStyleConfiguration
        {
            FontSize = 18,
            FontWeight = TextStyleConfiguration.FontWeights.Normal,
            FontFamilies = GetFontFamilyPointers("Times New Roman")
        });
        
        paragraphBuilder.AddPlaceholder(new SkPlaceholderStyle { Width = 20, Height = 20, Alignment = SkPlaceholderStyle.PlaceholderAlignment.Bottom });
        paragraphBuilder.AddText("Line 1\n", textStyle);
        
        paragraphBuilder.AddPlaceholder(new SkPlaceholderStyle { Width = 30, Height = 30, Alignment = SkPlaceholderStyle.PlaceholderAlignment.Bottom });
        paragraphBuilder.AddText("Line 2 \n", textStyle);
        
        paragraphBuilder.AddPlaceholder(new SkPlaceholderStyle { Width = 40, Height = 40, Alignment = SkPlaceholderStyle.PlaceholderAlignment.Bottom });
        paragraphBuilder.AddText("Line 3", textStyle);
        
        using var paragraph = paragraphBuilder.CreateParagraph();
        
        paragraph.PlanLayout(400);

        var lineMetrics = paragraph.GetLineMetrics();
        lineMetrics.Select(x => x.Height).Should().BeEquivalentTo(new[] { 21, 31, 41 });
    }
    
    [Test]
    public void GetUnresolvedCodepoints()
    {
        using var typefaceProvider = new SkTypefaceProvider();
        using var latoTypefaceData = SkData.FromFile("input/Lato/Lato-Light.ttf");
        typefaceProvider.AddTypefaceFromData(latoTypefaceData);

        using var fontCollection = SkFontCollection.Create(typefaceProvider, SkFontManager.Empty);

        var paragraphStyleConfiguration = new ParagraphStyleConfiguration();
        using var paragraphBuilder = SkParagraphBuilder.Create(paragraphStyleConfiguration, fontCollection);

        using var textStyle = new SkTextStyle(new TextStyleConfiguration
        {
            FontSize = 20,
            FontWeight = TextStyleConfiguration.FontWeights.Normal,
            ForegroundColor = 0xFF000000,
            FontFamilies = GetFontFamilyPointers("Lato")
        });
        
        const string text = "Fire as emoji 🔥 and in Japanese 火.";
        
        paragraphBuilder.AddText(text, textStyle);
        using var paragraph = paragraphBuilder.CreateParagraph();
        
        paragraph.PlanLayout(400);

        var unresolvedCodepoints = paragraph.GetUnresolvedCodepoints();
        unresolvedCodepoints.Should().BeEquivalentTo(new[] { 128293, 28779 });
    }
    
    [Test]
    public void Subscript()
    {
        // build simple paragraph
        using var typefaceProvider = new SkTypefaceProvider();
        var fontCollection = SkFontCollection.Create(typefaceProvider, SkFontManager.Global);   
 
        var paragraphStyleConfiguration = new ParagraphStyleConfiguration
        {
            Alignment = ParagraphStyleConfiguration.TextAlign.Left,
            Direction = ParagraphStyleConfiguration.TextDirection.Ltr,
            MaxLinesVisible = 40
        };
        
        using var paragraphBuilder = SkParagraphBuilder.Create(paragraphStyleConfiguration, fontCollection);

        var textStyleConfiguration = new TextStyleConfiguration
        {
            FontSize = 18,
            FontWeight = TextStyleConfiguration.FontWeights.Normal,
            
            FontFamilies = GetFontFamilyPointers("Times New Roman"),
            
            BackgroundColor = 0x00000000,
            ForegroundColor = 0xFF000000,
            
            DecorationColor = 0xFF009688,
            DecorationStyle = TextStyleConfiguration.TextDecorationStyle.Solid,
            DecorationType = TextStyleConfiguration.TextDecoration.NoDecoration,
            DecorationMode = TextStyleConfiguration.TextDecorationMode.Gaps, 
            
            WordSpacing = 0,
            LetterSpacing = 0
        };
        
        using var textStyle = new SkTextStyle(textStyleConfiguration);
        using var boldStyle = new SkTextStyle(textStyleConfiguration with { FontWeight = TextStyleConfiguration.FontWeights.Bold });
        using var boldSubscriptTextStyle = new SkTextStyle(textStyleConfiguration with { FontWeight = TextStyleConfiguration.FontWeights.ExtraBold, FontSize = 12, BaselineOffset = 4});

        paragraphBuilder.AddText("Water is an inorganic compound with the chemical formula ", textStyle);
        paragraphBuilder.AddText("H", boldStyle);
        paragraphBuilder.AddText("2", boldSubscriptTextStyle);
        paragraphBuilder.AddText("O", boldStyle);
        paragraphBuilder.AddText(". It is a transparent, tasteless, odorless, and nearly colorless chemical substance.", textStyle);

        var paragraph = paragraphBuilder.CreateParagraph();
        
        // draw paragraph
        using var stream = new SkWriteStream();
        using var pdf = SkPdfDocument.Create(stream, new SkPdfDocumentMetadata() { CompressDocument = true });
        
        // draw text
        using var pageCanvas = pdf.BeginPage(500, 800);
        
        pageCanvas.Translate(50, 50);
        paragraph.PlanLayout(400);
        pageCanvas.DrawParagraph(paragraph);
        
        pdf.EndPage();
        pdf.Close();

        using var documentData = stream.DetachData();
        TestFixture.SaveOutput("document_subscript.pdf", documentData);
    }
    
    [Test]
    public void DrawParagraphWithHyperlink()
    {
        using var typefaceProvider = new SkTypefaceProvider();
        using var fontCollection = SkFontCollection.Create(typefaceProvider, SkFontManager.Global);
        
        using var stream = new SkWriteStream();
        using var pdf = SkPdfDocument.Create(stream, new SkPdfDocumentMetadata());

        using var pageCanvas = pdf.BeginPage(350, 500);

        using var paragraph = BuildParagraph();
        paragraph.PlanLayout(300);
        pageCanvas.Translate(25, 25);
        
        var linkBoxes = paragraph.GetTextRangePositions(45, 57);
        
        foreach (var linkBox in linkBoxes)
            pageCanvas.DrawStrokeRectangle(linkBox,  3,0xFFFF0000);
        
        pageCanvas.DrawParagraph(paragraph);

        pdf.EndPage();
        pdf.Close();

        using var documentData = stream.DetachData();
        TestFixture.SaveOutput("document_with_paragraph_and_inlined_hyperlink.pdf", documentData);

        SkParagraph BuildParagraph()
        {
            var paragraphStyleConfiguration = new ParagraphStyleConfiguration
            {
                MaxLinesVisible = 1000,
                Direction = ParagraphStyleConfiguration.TextDirection.Ltr
            };
            
            using var paragraphBuilder = SkParagraphBuilder.Create(paragraphStyleConfiguration, fontCollection);
            
            var baseTextStyleConfiguration = new TextStyleConfiguration
            {
                FontSize = 18,
                ForegroundColor = 0xFF000000,
                FontWeight = TextStyleConfiguration.FontWeights.Medium,
                FontFamilies = GetFontFamilyPointers("Times New Roman")
            };

            var linkStyleConfiguration = baseTextStyleConfiguration with
            {
                ForegroundColor = 0xFF0088FF,
                DecorationType = TextStyleConfiguration.TextDecoration.Underline,
                DecorationColor = 0xFF0088FF
            };
            
            using var normalTextStyle = new SkTextStyle(baseTextStyleConfiguration);
            using var linkTextStyle = new SkTextStyle(linkStyleConfiguration);
            using var boldLinkTextStyle = new SkTextStyle(linkStyleConfiguration with { FontWeight = TextStyleConfiguration.FontWeights.Bold });
            
            paragraphBuilder.AddText("To start learning QuestPDF, please visit the ", normalTextStyle);
            paragraphBuilder.AddText("QuestPDF", boldLinkTextStyle);
            paragraphBuilder.AddText(".com", linkTextStyle);
            paragraphBuilder.AddText(" webpage, and have fun with the library!", normalTextStyle);
            
            return paragraphBuilder.CreateParagraph();
        }
    }
    
    IntPtr[] GetFontFamilyPointers(params string[] texts)
    {
        var result = new IntPtr[TextStyleConfiguration.FONT_FAMILIES_LENGTH];
                
        for (var i = 0; i < Math.Min(result.Length, texts.Length); i++)
            result[i] = new SkText(texts[i]).Instance;
                
        return result;
    }
}