# 1.1.3
- added zen construction and azu crafty boxes as soft dependency to make sure mod loads after theirs to check for conflicts
- fixed auto-pickup not picking up when bag is full and inventory has space
- added abyssal harpoon attach point

# 1.1.2
- fixed quiver allowing to pull bow back even when no arrows in player inventory nor quiver

# 1.1.1
- Fixed auto-stack not updating player inventory weight on pickup
- added scyth attach
- added automatic un-patch craft-from-bag if conflicts found
- fixed repairing items in bag not being saved
- lantern and charms remove movement speed penalty (toggleable)

# 1.1.0
- Fixed inventory drag not allowing to drag into player inventory while single bag config is on

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
