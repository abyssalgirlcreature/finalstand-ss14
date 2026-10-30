using System.Numerics;
using Content.Client._FinalStand.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared._FinalStand.Titles;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;

namespace Content.Client._FinalStand.Profile;

public sealed class FSTitlesWindow : FancyWindow
{
    private readonly FSTitlesPanel _panel = new();

    public event Action<string?>? OnEquip;

    public FSTitlesWindow()
    {
        ((Control) this).Stylesheet = FSMenuStylesheet.Get(
            IoCManager.Resolve<IUserInterfaceManager>(), IoCManager.Resolve<IResourceCache>());

        Title = Loc.GetString("fs-titles-window-title");
        SetSize = new Vector2(560, 560);
        MinSize = new Vector2(460, 360);

        _panel.OnEquip += title => OnEquip?.Invoke(title);
        ContentsContainer.AddChild(_panel);
    }

    public void Update(FSTitlesStateEvent state) => _panel.Update(state);
}
