using HutchASKA.Plugin.Tribe;
using UnityEngine;

namespace HutchASKA.Plugin.UI.Tabs;

internal sealed class RecruitRerollPanel(RecruitRerollFeature reroll, FeatureControls controls)
{
    private IReadOnlyList<RecruitChoicePreview> choices = Array.Empty<RecruitChoicePreview>();
    private RecruitChoicePreview? proposed;
    private int selected = -1;
    private string? message;

    internal void Clear()
    {
        choices = Array.Empty<RecruitChoicePreview>();
        proposed = null;
        selected = -1;
        message = null;
        reroll.Clear();
    }

    internal void Draw()
    {
        GUILayout.Space(10);
        GUILayout.Label("Upcoming recruit traits");
        GUILayout.Label("Keep the recruit's name and appearance. Preview new starting traits, then apply before choosing a recruit in the game.");
        GUILayout.Label("For ordinary recruitment with multiple choices. Special single-choice summons and lost villagers are excluded.");
        var enabled = GUI.enabled;
        GUI.enabled = enabled && controls.CanChange(controls.Get("tribe.reroll"));
        if (GUILayout.Button("Refresh recruits"))
        {
            proposed = null;
            selected = -1;
            reroll.TryRefresh(out choices, out message);
        }
        for (var i = 0; i < choices.Count; i++)
        {
            if (GUILayout.Toggle(selected == i, choices[i].Name) && selected != i)
            {
                selected = i;
                proposed = null;
                message = null;
            }
        }
        if (selected >= 0 && selected < choices.Count)
        {
            GUILayout.Label("Current traits");
            GUILayout.Label(choices[selected].Traits);
            if (GUILayout.Button("Reroll preview")) reroll.TryReroll(choices[selected].Id, out proposed, out message);
        }
        if (proposed is not null)
        {
            GUILayout.Label("New traits (not applied yet)");
            GUILayout.Label(proposed.Traits);
            if (GUILayout.Button("Apply these traits"))
            {
                var applied = reroll.TryApply(out var error);
                Clear();
                message = applied
                    ? "Traits applied. Open the game's normal recruit selection screen to review and summon this recruit. Unchosen previews may change after reloading the village."
                    : error;
            }
        }
        GUI.enabled = enabled;
        if (message is not null) GUILayout.Label(message);
        if (controls.Get("tribe.reroll").StatusReason is { } reason) GUILayout.Label(reason);
    }
}
