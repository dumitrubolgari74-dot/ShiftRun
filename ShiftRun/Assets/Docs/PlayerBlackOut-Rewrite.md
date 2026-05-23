# PlayerBlackOut — переписывание с нуля

Эталон: `Assets/_Scenes/TestEnv/Test.unity`  
Плоскость игры: **XY**, камера смотрит вдоль **+Z**.

## Удалено (старый стек)

- `UmbrellaControl`, `UmbrellaTipContact`, `UmbrellaPhyisisc`
- `PlayerBlackOutStickJoint`, `HammerColliderPhysics`
- `PlayerBlackOutWiring`, `Bootstrap`, `WiringReport`, `PlayerBlackOutSettings`
- Editor: `TestSceneControlSetup`, `PlayerBlackOutWiringTests`
- Документация и cursor-rule по старому GOI

## План по шагам

| Шаг | Скрипт | Статус |
|-----|--------|--------|
| **1** | `GoiHammerAim` — мышь, поворот, reach | **Готово** |
| 2 | `GoiHammerContact` — grounded, нормаль, точка | |
| 3 | `GoiPlayerPhysics` — силы на Human (XY, лимиты) | |
| 4 | Kinematic RB + collider sync на HammerMesh | |
| 5 | (опционально) ConfigurableJoint stick | |
| 6 | Editor wiring — только по запросу | |

## Шаг 1 — проверка

1. Открыть `Test.unity`, Play.
2. На **HammerHead** висит `GoiHammerAim`: pivot = HammerHead, shaftEnd = HammerMesh.
3. Молот смотрит на курсор, колёсико меняет длину (min/max reach).
4. **Human** только Rigidbody + Capsule — пока не толкается (это шаг 3).

## Иерархия (не менять имена)

```
PlayerBlackOut
└── Human
    ├── HammerHead     ← GoiHammerAim
    │   └── HammerHandler
    │       └── HammerMesh   ← shaftEnd, collider
    └── CenterOfHuman
```

Следующий шаг: сказать «шаг 2» — добавим контакт с землёй.
