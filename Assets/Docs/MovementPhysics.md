# Movement Physics Notes

Player movement uses Rigidbody2D velocity writes in `FixedUpdate`.

The `PlayerController` ground and wall casts must only query the `Ground` layer. Do not include `Enemy` in `groundLayer`; enemies are combat targets, not movement blockers.

Current intended setup:

- Player GameObject layer: `Player`
- Ground/platform tilemaps GameObject layer: `Ground`
- Enemy prefabs GameObject layer: `Enemy`
- `PlayerController.groundLayer`: `Ground` only
- `PlayerCombat.attackLayerMask`: `Enemy` only
- Physics 2D matrix:
  - `Player` vs `Ground`: enabled
  - `Player` vs `Enemy`: disabled
  - `Enemy` vs `Ground`: enabled
  - `Enemy` vs `Enemy`: disabled

Rigidbody2D recommendations:

- Player: Dynamic, Freeze Rotation Z, Interpolate
- Enemies: Dynamic, Freeze Rotation Z, Interpolate

If physical pushing between the player and enemies is ever reintroduced, keep `PlayerController` casts restricted to `Ground` and use no-friction physics materials on both player and enemy colliders.
