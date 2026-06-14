// Both Microsoft.Maui and Microsoft.Maui.Graphics define an IImage; the effects only ever mean the
// graphics one (the drawable image), so pin the alias once for the whole assembly.
global using IImage = Microsoft.Maui.Graphics.IImage;
