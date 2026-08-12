namespace Game.Data
{
    /// <summary>Stat quality tier rolled at acquisition. F is worst, SSS is best.</summary>
    public enum Tier { F, E, D, C, B, A, S, SS, SSS }

    public enum ElementType { Neutral, Fire, Water, Wind, Earth, Light, Dark }

    public enum ClassType { Warrior, Guardian, Ranger, Mage, Healer, Assassin, Summoner }

    /// <summary>Drop/craft scarcity. Independent of Tier, which applies only to units.</summary>
    public enum Rarity { Common, Uncommon, Rare, Epic, Legendary }

    /// <summary>Era band. Maps to the archipelago's genre-per-island structure.</summary>
    public enum Age { Primitive, Medieval, Industrial, Modern, Futuristic, Mythic }

    public enum EquipSlot { Weapon, Armor, Accessory }

    public enum DistrictType { Residential, Industrial, Agricultural, Military, Arcane, Commercial }

    public enum GearSource { Drop, Summon, Craft }

    public enum Faction { Player, Enemy, Neutral }

    public enum TerrainType { Plain, Grass, Stone, Water, Sand, Ruins }

    public enum SkillPool { Standard, Class, Element }
}
