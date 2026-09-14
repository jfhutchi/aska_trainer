namespace HutchASKA.Plugin.UI.Tabs;

internal sealed class CraftingTab(FeatureControls controls)
{
    public void Draw()
    {
        controls.Toggle("crafting.free");
        controls.Toggle("building.free");
        controls.Toggle("repairs.free");
    }
}
