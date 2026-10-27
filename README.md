# ParkMinDev.UPM.Foundation

## DestroyOnAwake

`DestroyOnAwake` disables selected GameObjects and supported components in `Awake`, schedules their destruction, then removes itself. Assign targets through the `Game Objects` and `Components` arrays.

| Component | Action before destruction |
| --- | --- |
| `Behaviour` and derived types | Set `enabled` to `false`; includes scripts and installed package components deriving from `Behaviour`. |
| `Renderer` and derived types | Set `enabled` to `false`. |
| `LODGroup` | Set `enabled` to `false` before processing selected renderers, since disabling a group enables its renderers. Renderers must also be selected to disable their rendering. |
| `Collider` and derived types | Set `enabled` to `false`. |
| `Cloth` | Set `enabled` to `false`. |
| `Rigidbody` | Set `isKinematic` to `true` and `detectCollisions` to `false`. This suspends physics-driven movement and collision detection rather than fully disabling the component. |
| `Rigidbody2D` | Set `simulated` to `false`; attached colliders and joints also leave the simulation. |
| `ParticleSystem` | Set `stopAction` to `None`, then stop and clear only the selected system without including children. This prevents its stop action from destroying the owning GameObject or invoking a stop callback. |
| `CanvasRenderer` | Set `cull` to `true` to suppress drawing. |
| `WindZone` | Set main wind, turbulence, and pulse magnitude to zero. |

Physics, Physics 2D, Cloth, Particle System, UI, and Wind handlers are conditionally compiled through the runtime assembly's Version Defines when their respective `com.unity.modules.*` packages are installed. No optional package is installed or added as a dependency. Package components covered by `Behaviour` or `Renderer` do not require package-specific references.

`Transform`, including `RectTransform`, is skipped with a warning; select its GameObject instead. Other components retain the existing destruction behavior even if they have no supported pre-destruction disable operation. Only explicitly selected targets are processed; selecting a component does not deactivate its owning GameObject or recursively process children.
