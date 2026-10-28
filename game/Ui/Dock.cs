using System;
using Godot;

namespace ArquipelagoDoCeu;

// O canto inferior esquerdo: o Inventário (selo "novo" em texto) e o Papel de agora, que troca entre Matemático e Escriba no Lobby.
// Numa Dungeon o Papel fica travado (uma torre, um Papel). No celular vira o TouchCluster; no PC, dois botões de texto.
public partial class Dock : Control
{
	const int ButtonHeight = 72;
	const int ButtonWidth = 220;
	const int Margin = 12;

	readonly TouchCluster? cluster;
	readonly Button? inventory;
	readonly Button? role;
	string roleName = "";
	string inventoryText = "Inventário";

	public Dock()
	{
		Name = "Dock";
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		if (DeviceInfo.IsTouch)
		{
			cluster = new TouchCluster();
			cluster.InventoryPressed += () => InventoryRequested?.Invoke();
			cluster.RolePressed += () => RoleSwitchRequested?.Invoke();
			AddChild(cluster);
			return;
		}

		var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		inventory = BookStyle.Button("Inventário", () => InventoryRequested?.Invoke(), "InventoryButton", ButtonHeight);
		inventory.CustomMinimumSize = new Vector2(ButtonWidth, ButtonHeight);
		inventory.TooltipText = "Abrir o Inventário [I]";
		row.AddThemeConstantOverride("separation", 8);
		row.AddChild(inventory);
		role = BookStyle.Button("", () => RoleSwitchRequested?.Invoke(), "RoleButton", ButtonHeight);
		role.Icon = Icons.For("swap", 28);
		role.IconAlignment = HorizontalAlignment.Right;
		role.AddThemeFontSizeOverride("font_size", 20);
		role.TooltipText = "Trocar de Papel";
		row.AddChild(role);
		AddChild(row);
		row.SetAnchorsPreset(LayoutPreset.BottomLeft);
		row.GrowVertical = GrowDirection.Begin;
		row.OffsetLeft = Margin;
		row.OffsetBottom = -Margin;
		row.OffsetTop = -Margin - ButtonHeight;
	}

	public event Action? InventoryRequested;

	public string InventoryText => inventoryText;

	public event Action? RoleSwitchRequested;

	public string RoleText => roleName;

	public void SetRole(string name)
	{
		roleName = name;
		if (role is not null) role.Text = name;
	}

	// Dentro de uma Dungeon o Papel é o da torre: o botão só mostra o nome.
	public void LockRole()
	{
		cluster?.LockRole();
		if (role is null) return;
		role.Disabled = true;
		role.Icon = null;
		role.TooltipText = "Esta torre tem um Papel só";
	}

	// Com itens ainda não vistos o botão diz quantos são, em texto: o selo nunca é só uma cor.
	public void SetNewCount(int count)
	{
		inventoryText = count > 0 ? $"Inventário · {(count == 1 ? "1 novo" : $"{count} novos")}" : "Inventário";
		if (inventory is not null) inventory.Text = inventoryText;
		if (cluster is not null) cluster.SetInventoryBadge(count > 0 ? count.ToString(System.Globalization.CultureInfo.InvariantCulture) : "");
	}

	// Na luta o celular mantém só o direcional: a bolsa de peças ocupa o resto da parte de baixo.
	public void SetInCombat(bool fighting) => cluster?.SetCompact(fighting);

	public void Press() => InventoryRequested?.Invoke();
}
