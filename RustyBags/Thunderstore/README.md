# 👜 Rusty Bags

A bag plugin with a twist — the look of your bag changes based on what’s inside!  
Tools like the Cultivator, Hoe, Lantern, Pickaxe, Melee Weapons, Atgeir, Fishing Rod, and Hammer will appear visibly attached when stored in your bag.

---

## ✨ Features

- 🧳 4 unique bags and 2 functional quivers
- 🧵 Visual updates based on bag contents
- ⚒️ Craft directly from your bag
- 📦 Items picked up will stack automatically into a matching bag (toggleable)
- 🏹 Dynamic quivers:
    - Arrows inside can be used for shooting
    - Quiver visuals update based on the first arrow and stack size

---

## ⚖️ Notes

- Bags increase carry weight but reduce movement speed
- Bags cannot be stored inside other bags
- Quivers only accept arrows
- If you reduce your inventory size, you may lose stored items

---

![](https://i.imgur.com/PSH6qZ3.png)

![](https://i.imgur.com/5u5FEX8.png)

![](https://i.imgur.com/ZzjP5Ze.png)


## Example Configuration
```properties
[Barrel Bag]

## Setup inventory size for quality 1, width x height [Synced with Server]
# Setting type: String
# Default value: 8x2
Inventory Size Qlty.1 = 8x2

## Setup inventory size for quality 2, width x height [Synced with Server]
# Setting type: String
# Default value: 8x3
Inventory Size Qlty.2 = 8x3

## Setup inventory size for quality 3, width x height [Synced with Server]
# Setting type: String
# Default value: 8x4
Inventory Size Qlty.3 = 8x4

## Setup inventory size for quality 4, width x height [Synced with Server]
# Setting type: String
# Default value: 8x5
Inventory Size Qlty.4 = 8x5

## Crafting station where Barrel Bag is available.
# Setting type: CraftingTable
# Default value: Forge
# Acceptable values: Disabled, Inventory, Workbench, Cauldron, MeadCauldron, Forge, ArtisanTable, StoneCutter, MageTable, PrepTable, BlackForge, Custom
Crafting Station = Forge

# Setting type: String
# Default value: 
Custom Crafting Station = 

## Required crafting station level to craft Barrel Bag.
# Setting type: Int32
# Default value: 1
Crafting Station Level = 1

## Maximum crafting station level to upgrade and repair Barrel Bag.
# Setting type: Int32
# Default value: 4
Maximum Crafting Station Level = 4

## Whether only one of the ingredients is needed to craft Barrel Bag
# Setting type: Toggle
# Default value: Off
# Acceptable values: Off, On
Require only one resource = Off

## Multiplies the crafted amount based on the quality of the resources when crafting Barrel Bag. Only works, if Require Only One Resource is true.
# Setting type: Single
# Default value: 1
Quality Multiplier = 1

## Item costs to craft Barrel Bag
# Setting type: String
# Default value: ElderBark:10,Iron:5,Guck:5,LeatherScraps:20
Crafting Costs = ElderBark:10,Iron:5,Guck:5,LeatherScraps:20

## Item costs per level to upgrade Barrel Bag
# Setting type: String
# Default value: ElderBark:5:2,Iron:2:2,Guck:3:2,LeatherScraps:10:2,WolfPelt:10:3,Silver:5:3,WolfClaw:5:3,WolfHairBundle:5:3,WolfPelt:5:4,Silver:2:4,WolfClaw:3:4,WolfHairBundle:2:4
Upgrading Costs = ElderBark:5:2,Iron:2:2,Guck:3:2,LeatherScraps:10:2,WolfPelt:10:3,Silver:5:3,WolfClaw:5:3,WolfHairBundle:5:3,WolfPelt:5:4,Silver:2:4,WolfClaw:3:4,WolfHairBundle:2:4
```