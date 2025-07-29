using Godot;
using OsuSkinMixer.Models.Presets;
using OsuSkinMixer.Statics;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace OsuSkinMixer.StackScenes;

public partial class PresetSelector : StackScene
{
    public override string Title => "Presets";

    private PackedScene PresetButtonScene;

    private VBoxContainer PresetsContainer;

    public override void _Ready()
    {
        base._Ready();

        PresetButtonScene = GD.Load<PackedScene>("res://src/Components/MenuButton.tscn");
        PresetsContainer = GetNode<VBoxContainer>("%PresetsContainer");

        string presetsJson = File.ReadAllText(ProjectSettings.GlobalizePath("res://assets/presets.json"));
        List<SkinPreset> presets = JsonSerializer.Deserialize<List<SkinPreset>>(presetsJson);

        foreach (var preset in presets)
        {
            var button = PresetButtonScene.Instantiate<Button>();
            button.Text = preset.Name;
            button.Pressed += () =>
            {
                Settings.LoadPreset(preset.Name);
                EmitSignal(SignalName.ScenePopped);
            };
            PresetsContainer.AddChild(button);
        }
    }
}
