using Robust.Shared.GameStates;

namespace Content.Shared._FinalStand.Shop;

// Came with the player's job gear rather than from a shop, so selling it back pays nothing for the weapon itself.
// Networked so the sell button shows what the shop really pays.
[RegisterComponent, NetworkedComponent]
public sealed partial class FSIssuedWeaponComponent : Component;
