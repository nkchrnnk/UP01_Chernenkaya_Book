using System;
using System.IO;
using PdfSharpCore.Fonts;

namespace UP01_Chernenkaya_Book;

/// <summary>
/// Пользовательский резолвер системных шрифтов для корректной генерации PDF-документов с кириллицей
/// </summary>
public class FileFontResolver : IFontResolver
{
    /// <summary>
    /// Имя системного шрифта по умолчанию
    /// </summary>
    public string DefaultFontName => "Arial";

    /// <summary>
    /// Считывание байтов файла шрифта из стандартной системной папки Windows Fonts
    /// </summary>
    /// <param name="faceName">Имя файла шрифта</param>
    /// <returns>Массив байтов шрифта или null</returns>
    public byte[]? GetFont(string faceName)
    {
        string fontsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        string fontPath = Path.Combine(fontsFolder, faceName);
        if (File.Exists(fontPath))
        {
            return File.ReadAllBytes(fontPath);
        }
        string arialPath = Path.Combine(fontsFolder, "arial.ttf");
        if (File.Exists(arialPath))
        {
            return File.ReadAllBytes(arialPath);
        }
        return null;
    }

    /// <summary>
    /// Сопоставление названия гарнитуры и начертания (жирный, курсив) с файлом ttf
    /// </summary>
    /// <param name="familyName">Семейство шрифта</param>
    /// <param name="isBold">Жирное начертание</param>
    /// <param name="isItalic">Курсивное начертание</param>
    /// <returns>Информация о разрешенном шрифте</returns>
    public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        if (familyName.Equals("Arial", StringComparison.OrdinalIgnoreCase) ||
            familyName.Equals("Segoe UI", StringComparison.OrdinalIgnoreCase) ||
            familyName.Equals("Times New Roman", StringComparison.OrdinalIgnoreCase))
        {
            if (isBold && isItalic) return new FontResolverInfo("arialbi.ttf");
            if (isBold) return new FontResolverInfo("arialbd.ttf");
            if (isItalic) return new FontResolverInfo("ariali.ttf");
            return new FontResolverInfo("arial.ttf");
        }
        return new FontResolverInfo("arial.ttf");
    }
}
