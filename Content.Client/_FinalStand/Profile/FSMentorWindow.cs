using System.Numerics;
using Content.Client._FinalStand.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared._FinalStand.Titles;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;

namespace Content.Client._FinalStand.Profile;

public sealed class FSMentorWindow : FancyWindow
{
    private readonly FSMentorPanel _panel = new();

    public event Action<string>? OnBuy;

    public FSMentorWindow()
    {
        ((Control) this).Stylesheet = FSMenuStylesheet.Get(
            IoCManager.Resolve<IUserInterfaceManager>(), IoCManager.Resolve<IResourceCache>());

        Title = Loc.GetString("fs-mentor-window-title");
        SetSize = new Vector2(600, 620);
        MinSize = new Vector2(480, 420);

        _panel.OnBuy += item => OnBuy?.Invoke(item);
        ContentsContainer.AddChild(_panel);
    }

    public void Update(FSMentorStateEvent state) => _panel.Update(state);
}
