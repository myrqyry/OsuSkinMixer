using Godot;
using OsuSkinMixer.Statics;
using System.IO;
using System.Text;
using System.Text.Json;

namespace OsuSkinMixer.StackScenes;

using OsuSkinMixer.Models;
using System.Collections.Generic;

public class SkinElementMetadata
{
    public string Name { get; set; }
    public string Author { get; set; }
    public List<string> Colors { get; set; }
}

public partial class AiGenerator : StackScene
{
    public override string Title => "AI Skin Element Generator";

    public OsuSkin Skin { get; set; }

    private LineEdit PromptEdit;
    private Button SelectImageButton;
    private Button GenerateButton;
    private TextureRect TextureRect;
    private HttpRequest HttpRequest;
    private FileDialog FileDialog;

    private string SelectedImagePath;

    public override void _Ready()
    {
        base._Ready();

        PromptEdit = GetNode<LineEdit>("%Prompt");
        SelectImageButton = GetNode<Button>("%SelectImageButton");
        GenerateButton = GetNode<Button>("%GenerateButton");
        TextureRect = GetNode<TextureRect>("%TextureRect");
        HttpRequest = GetNode<HttpRequest>("HttpRequest");
        FileDialog = GetNode<FileDialog>("FileDialog");

        SelectImageButton.Pressed += () => FileDialog.PopupCentered();
        FileDialog.FileSelected += OnFileSelected;
        GenerateButton.Pressed += OnGenerateButtonPressed;
        HttpRequest.RequestCompleted += OnRequestCompleted;
    }

    private void OnFileSelected(string path)
    {
        SelectedImagePath = path;
        var image = Image.LoadFromFile(path);
        var texture = ImageTexture.CreateFromImage(image);
        TextureRect.Texture = texture;
    }

    private void OnGenerateButtonPressed()
    {
        string apiKey = "AIzaSyC0W595gQfd_V-pDmy2e2wRqoGOFJbhYKI";
        string apiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-pro:generateContent";

        var requestData = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = PromptEdit.Text },
                        new
                        {
                            inline_data = new
                            {
                                mime_type = "image/png",
                                data = Convert.ToBase64String(File.ReadAllBytes(SelectedImagePath))
                            }
                        }
                    }
                }
            },
            tools = new[]
            {
                new
                {
                    function_declarations = new[]
                    {
                        new
                        {
                            name = "replaceSkinElement",
                            description = "Replace a skin element with a new one.",
                            parameters = new
                            {
                                type = "object",
                                properties = new
                                {
                                    elementName = new
                                    {
                                        type = "string",
                                        description = "The name of the skin element to replace."
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        string[] headers = new string[]
        {
            "Content-Type: application/json",
            $"x-goog-api-key: {apiKey}"
        };

        requestData.generationConfig = new
        {
            response_mime_type = "application/json",
            response_schema = new
            {
                type = "object",
                properties = new
                {
                    name = new { type = "string" },
                    author = new { type = "string" },
                    colors = new
                    {
                        type = "array",
                        items = new { type = "string" }
                    }
                }
            }
        };

        HttpRequest.Request(apiEndpoint, headers, HttpClient.Method.Post, JsonSerializer.Serialize(requestData));
    }

    private void OnRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
    {
        if (responseCode != 200)
        {
            GD.PrintErr($"Request failed with response code {responseCode}");
            return;
        }

        var json = JsonDocument.Parse(Encoding.UTF8.GetString(body));
        var candidates = json.RootElement.GetProperty("candidates");
        var firstCandidate = candidates[0];
        var content = firstCandidate.GetProperty("content");
        var parts = content.GetProperty("parts");
        var firstPart = parts[0];

        var json = JsonDocument.Parse(Encoding.UTF8.GetString(body));
        var metadata = JsonSerializer.Deserialize<SkinElementMetadata>(json.RootElement.GetProperty("text").GetString());

        var imageBytes = Convert.FromBase64String(json.RootElement.GetProperty("inlineData").GetProperty("data").GetString());
        var image = new Image();
        image.LoadPngFromBuffer(imageBytes);

        var texture = ImageTexture.CreateFromImage(image);
        TextureRect.Texture = texture;

        ReplaceSkinElement(metadata.Name, SelectedImagePath, metadata);
    }

    private void ReplaceSkinElement(string elementName, string imagePath, SkinElementMetadata metadata)
    {
        string skinFolderPath = Skin.Directory.FullName;
        string newImagePath = Path.Combine(skinFolderPath, elementName + ".png");

        if (File.Exists(newImagePath))
            File.Delete(newImagePath);

        File.Copy(imagePath, newImagePath);

        string skinIniPath = Path.Combine(Skin.Directory.FullName, "skin.ini");
        if (File.Exists(skinIniPath))
        {
            var skinIni = new OsuSkinIni(File.ReadAllText(skinIniPath));
            var generalSection = skinIni.Sections.Find(s => s.Name == "General");
            if (generalSection != null)
            {
                generalSection["Name"] = metadata.Name;
                generalSection["Author"] = metadata.Author;

                if (elementName == "cursor")
                {
                    generalSection["CursorRotate"] = "0";
                    generalSection["CursorExpand"] = "0";
                }
                else if (elementName == "hitcircle")
                {
                    generalSection["HitCircleOverlayAboveNumber"] = "0";
                }
            }

            var coloursSection = skinIni.Sections.Find(s => s.Name == "Colours");
            if (coloursSection != null)
            {
                for (int i = 0; i < metadata.Colors.Count; i++)
                {
                    coloursSection[$"Combo{i + 1}"] = metadata.Colors[i];
                }
            }

            File.WriteAllText(skinIniPath, skinIni.ToString());
        }

        OsuData.RequestSkinInfo(new[] { Skin });
        EmitSignal(SignalName.ScenePopped);
    }
}
