namespace HutchASKA.Plugin.UI.Tabs;

internal sealed class TribeSummonTab(FeatureControls controls, RecruitRerollPanel reroll)
{
    public void Clear() => reroll.Clear();

    public void Draw()
    {
        controls.Toggle("tribe.recruitment");
        reroll.Draw();
    }
}
