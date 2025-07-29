using Godot;
using OsuSkinMixer.Statics;
using System.IO;
using System.Text;
using System.Text.Json;

namespace OsuSkinMixer.StackScenes;

using OsuSkinMixer.Models;

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

        if (firstPart.TryGetProperty("functionCall", out var functionCall))
        {
            var functionName = functionCall.GetProperty("name").GetString();
            if (functionName == "replaceSkinElement")
            {
                var args = functionCall.GetProperty("args");
                var elementName = args.GetProperty("elementName").GetString();
                ReplaceSkinElement(elementName, SelectedImagePath);
            }
        }
        else if (firstPart.TryGetProperty("inlineData", out var inlineData))
        {
            var data = inlineData.GetProperty("data").GetString();
            var imageBytes = Convert.FromBase64String(data);
            var image = new Image();
            image.LoadPngFromBuffer(imageBytes);

            var texture = ImageTexture.CreateFromImage(image);
            TextureRect.Texture = texture;
        }
    }

    private void ReplaceSkinElement(string elementName, string imagePath)
    {
        string skinFolderPath = Skin.Directory.FullName;
        string newImagePath = Path.Combine(skinFolderPath, elementName + ".png");

        if (File.Exists(newImagePath))
            File.Delete(newImagePath);

        File.Copy(imagePath, newImagePath);

        if (elementName == "cursor" || elementName == "hitcircle")
        {
            string skinIniPath = Path.Combine(Skin.Directory.FullName, "skin.ini");
            if (File.Exists(skinIniPath))
            {
                var skinIni = new OsuSkinIni(File.ReadAllText(skinIniPath));
                if (elementName == "cursor")
                {
                    var generalSection = skinIni.Sections.Find(s => s.Name == "General");
                    if (generalSection != null)
                    {
                        generalSection["CursorRotate"] = "0";
                        generalSection["CursorExpand"] = "0";
                    }
                }
                else if (elementName == "hitcircle")
                {
                    var generalSection = skinIni.Sections.Find(s => s.Name == "General");
                    if (generalSection != null)
                    {
                        generalSection["HitCircleOverlayAboveNumber"] = "0";
                    }
                }
                File.WriteAllText(skinIniPath, skinIni.ToString());
            }
        }

        OsuData.RequestSkinInfo(new[] { Skin });
        EmitSignal(SignalName.ScenePopped);
    }
}
