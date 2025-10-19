# 1.0.9
- Added config to disable craft from bag

# 1.0.8
- Fixed bag updating teleportable of first load
- Added config to limit bag to one in player inventory
- localized EPI Bag slot: `$label_bag`

# 1.0.7
- Fixed bag not updating after player dies and retrieves bag

# 1.0.6
- Fixed logout / login registering charms again
- Updating bag weight on unequip to make sure effects are only applied while bag is equipped
- Fixed bag weight on first player load, so even unequipped bags have corrected weight

# 1.0.5 
- Added on-hover behavior to make sure bag container UI opens
- for compatibility with jeweler bag and `Quick Stack Store Sort Trash Restock`
- since QSSSTR hides take all button as well.
- some more charms to deco your bag

# 1.0.4
- Compatibility with jewelcrafting jewel bag
- Checks if `Take All` button is disabled (jewelcrafting disables this when jewel bag is open)
- If `Take All` is disabled, do not show Rusty bag
- Fixed bag not unequipping on death

# 1.0.3
- Added `Extended Player Inventory` API
- If `EPI API` is installed, new slot is added: `Bag`
- Added Crossbow Bolt Quiver
- Added carry weight config
- Added restriction config

# 1.0.2
- Updated bepinex dependency string to 5.4.2333

# 1.0.1
- Added YmlDotNet as dependency string

# 1.0.0
- Initial release
