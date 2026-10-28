using ArquipelagoDoCeu.Saves;

namespace ArquipelagoDoCeu.Equipment;

// A aparência do Personagem: o Papel (que escolhe o kit de roupa), o modelo da armadura, do capacete e do acessório, as cores e os Cosméticos. Os atributos moram em GearCatalog.
// `Cosmetics` são só peças de aparência; não entram em nenhum cálculo de atributo.
public sealed record EquipmentLoadout(string Armor = "original", string Helmet = "none",
	string ClothColor = "#315BB3", string MetalColor = "#AAB8C9", string AccentColor = "#D6AA51", IReadOnlyList<CosmeticLook>? Cosmetics = null,
	string Accessory = "none", string Role = "");

// O que o equipamento soma ao Personagem. `Extras` leva os efeitos que não são os três de sempre (um efeito novo).
public sealed record EquipmentBonuses(float SpeedMultiplier = 1f, int ExtraEnergy = 0, float InteractionReachBonus = 0f,
	IReadOnlyDictionary<string, double>? Extras = null);

// Como o Personagem aparece com um conjunto: só as aparências que têm modelo 3D; uma peça sem modelo aparece como a original.
public static class EquipmentLooks
{
	static readonly string[] Armors = ["scout", "guardian", "scholar"];

	static readonly string[] Helmets = ["open", "crystal"];

	static readonly string[] Accessories = ["wings", "lantern", "amulet"];

	public static EquipmentLoadout For(Gear gear, IReadOnlyList<CosmeticLook>? cosmetics = null, string role = "") =>
		new(Armor: Known(Armors, gear.In("armor"), "original"), Helmet: Known(Helmets, gear.In("helmet"), "none"), Cosmetics: cosmetics,
			Accessory: Known(Accessories, gear.In("accessory"), "none"), Role: role);

	static string Known(string[] looks, string? id, string fallback) => id is not null && looks.Contains(id) ? id : fallback;
}
