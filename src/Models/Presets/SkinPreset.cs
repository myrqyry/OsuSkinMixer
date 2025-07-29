using System.Collections.Generic;
using OsuSkinMixer.Models.SkinOptions;

namespace OsuSkinMixer.Models.Presets;

public class SkinPreset
{
    public string Name { get; set; }

    public List<SkinOption> Options { get; set; }
}
