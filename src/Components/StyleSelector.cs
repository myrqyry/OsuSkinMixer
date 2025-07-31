using Godot;
using OsuSkinMixer.Models;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace OsuSkinMixer.Components;

public partial class StyleSelector : Popup
{
    [Signal]
    public delegate void StyleSelectedEventHandler(string style);

    private VBoxContainer StyleList;

    public override void _Ready()
    {
        StyleList = GetNode<VBoxContainer>("%StyleList");
        var styles = LoadStyles();

        foreach (var style in styles)
        {
            var button = new Button();
            button.Text = style.Name;
            button.Pressed += () => OnStyleButtonPressed(style.Name);
            StyleList.AddChild(button);
        }
    }

    private void OnStyleButtonPressed(string style)
    {
        EmitSignal(SignalName.StyleSelected, style);
        Out();
    }

    private IEnumerable<SkinStyle> LoadStyles()
    {
        string stylesJson = File.ReadAllText("assets/styles.json");
        return JsonSerializer.Deserialize<IEnumerable<SkinStyle>>(stylesJson);
    }
}

public class SkinStyle
{
    public string Name { get; set; }
    public string Description { get; set; }
}
