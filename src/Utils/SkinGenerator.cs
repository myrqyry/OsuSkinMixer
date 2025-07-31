using OsuSkinMixer.Models;
using System.IO;

namespace OsuSkinMixer.Utils;

public static class SkinGenerator
{
    private static HttpRequest HttpRequest;

    public static void CreateSkin(string name, HttpRequest httpRequest)
    {
        HttpRequest = httpRequest;

        string path = Path.Combine(Settings.SkinsFolderPath, name);
        Directory.CreateDirectory(path);

        var skinIni = new OsuSkinIni(name, "osu! skin mixer");
        File.WriteAllText(Path.Combine(path, "skin.ini"), skinIni.ToString());
    }

    public static void CreateCursor(string skinName)
    {
        // TODO: Implement this method.
        GD.Print($"Creating cursor for skin '{skinName}'");
    }

    public static void CreateHitcircle(string skinName)
    {
        // TODO: Implement this method.
        GD.Print($"Creating hitcircle for skin '{skinName}'");
    }

    public static void CreateMenu-background(string skinName)
    {
        // TODO: Implement this method.
        GD.Print($"Creating menu background for skin '{skinName}'");
    }

    public static void CreateAnimatedMenuBackground(string skinName)
    {
        string skinFolderPath = Path.Combine(Settings.SkinsFolderPath, skinName);
        string videoPath = Path.Combine(skinFolderPath, "menu-background.mp4");

        // TODO: Generate video and save to videoPath
        GD.Print($"Creating animated menu background for skin '{skinName}' at path '{videoPath}'");
    }
}
