using Godot;
using OsuSkinMixer.Models.Presets;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace OsuSkinMixer.StackScenes;

public partial class PresetSelector : StackScene
{
    [Signal]
    public delegate void PresetSelectedEventHandler(SkinPreset preset);

    private VBoxContainer PresetList;

    public override void _Ready()
    {
        PresetList = GetNode<VBoxContainer>("%PresetList");
        var presets = LoadPresets();

        foreach (var preset in presets)
        {
            var button = new Button();
            button.Text = preset.Name;
            button.Pressed += () => OnPresetButtonPressed(preset);
            PresetList.AddChild(button);
        }
    }

    private void OnPresetButtonPressed(SkinPreset preset)
    {
        EmitSignal(SignalName.PresetSelected, preset);
        EmitSignal(SignalName.ScenePopped);
    }

    private IEnumerable<SkinPreset> LoadPresets()
    {
        string presetsJson = File.ReadAllText("assets/presets.json");
        return JsonSerializer.Deserialize<IEnumerable<SkinPreset>>(presetsJson);
    }
}
