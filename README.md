# The 2D Budget Fighter | SlutProjekt
**Hampus & Kalle SU23b**

1080p is the required resolution to play!

Welcome to our 2D LAN street fighting game test/demo! (Bugs can be expected, play at own risk)

You can play with two instances on the same machine, or with two machines on the same local network.

---

## How to Play

1. **Build & Run**
    - Open the **StartMenu** scene in Unity and press Play (or build the project).
    - You will see options to **Host** or **Join** a game.

2. **Hosting**
    - Choose **Host**.
    - The game will act as the server and wait for one other player to join.

3. **Joining**
    - On the same PC or another PC on the same LAN, launch the game.
    - Choose **Join**.
    - The client will automatically discover the host via LAN broadcast and connect.

4. **Character Select**
    - Both players use arrow keys or press the arrows to scroll through the fighter carousel.
    - Click **Select** to view details.
    - In the detail panel, scroll through **Idle**, **Basic Attack**, and **Ability** previews.
    - Click **Confirm** to lock in your fighter.

5. **Fight!**
    - Once both players have confirmed, the match loads.
    - Player 1 (host) and Player 2 (joiner) spawn at opposite sides.
    - Use movement, jump, dash, attack, and special keys to battle.

6. **Match End**
    - When one fighter’s health reaches zero, the death animation plays for both clients.
    - After a brief delay, both clients are disconnected and returned to the **StartMenu**.
    - Each player’s local stats (matches, wins, losses, kills, deaths) are saved in separate files.

---

## Controls

| Action         | Keyboard          |
|----------------|-------------------|
| Move Left      | A / ←             |
| Move Right     | D / →             |
| Jump           | W / ↑             |
| Dash           | Left Shift        |
| Melee Attack   | Left Mouse Button |
| Ranged Attack  | Left Mouse Button |
| Ability Attack | Space             |

---

## Requirements

- Unity 6000.0.31f1 LTS or later
- .NET 4.x runtime
- **Netcode for GameObjects** package
- **DOTween**, **TextMeshPro** packages

---

## LAN Setup

1. **Same PC**
    - Open two instances of the built game (or one editor + one build).
    - Host in one, Join in the other.

2. **Two PCs on LAN**
    - Ensure both machines are on the same local network.
    - Firewall ports **8888** (or your configured Netcode port) must be open.
    - Build and run the game on both machines.
    - Host on Machine A, Join on Machine B.

---

## Saved Data

- Each player’s stats are stored locally in `player_stats_{profile}.dat` under your *OS’s persistent-data* folder but are **encrypted** with **AES-256**.
- Host uses profile **Player1**, joiner uses **Player2** by default.
- You can view or reset these files to track wins, losses, kills, and deaths separately.

---

## Troubleshooting

- **Can’t discover host?**
    - Check both devices are on the same subnet (e.g. 192.168.x.x).
    - Disable firewall or allow UDP broadcasts on the game’s port.
- **Sliders not updating?**
    - Make sure the HUDManager prefab is present in the scene and marked `DontDestroyOnLoad`.
- **Animations not syncing?**
    - Confirm that your Fighter prefab has `NetworkTransform` and `NetworkAnimator` components.

---

**Enjoy the fight!**
