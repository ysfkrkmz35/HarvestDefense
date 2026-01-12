# HarvestDefense 🌾⚔️

**HarvestDefense** is a unique genre-blending game that combines **Farming Simulation** with **Tower Defense** mechanics. Cultivate your land, manage resources, and defend your farm from waves of enemies using real-time combat and magical spells, culminating in an epic boss battle.

---

## 📋 Project Overview

In HarvestDefense, you play as a guardian farmer. Your goal is to grow crops to earn resources while defending your farm from nightly invasions. As you progress, you unlock powerful spells and weapons to take on tougher enemies and the final boss.

### Key Features
*   **Hybrid Gameplay:** Seamlessly switch between farming crops and fighting monsters.
*   **Day/Night Cycle:** safe farming during the day, dangerous combat at night.
*   **Combat System:** Real-time sword combat and diverse spell casting (Area of Effect, Buffs, Healing).
*   **Boss Battles:** Challenging multi-phase boss fights with special mechanics.
*   **Economy:** Shop system to buy seeds, upgrades, and spells.
*   **Progression:** Wave-based difficulty scaling and leaderboards.

---

## 🛠️ Technology Stack & Dependencies

*   **Engine:** Unity 2022.3 LTS (2D Core)
*   **Language:** C# (.NET Standard 2.1)
*   **Core Packages:**
    *   **Unity UI Toolkit:** For modern UI interfaces (Shop, HUD).
    *   **Unity Physics 2D:** For combat detection and movement.
    *   **Cinemachine:** For dynamic camera control.
    *   **Input System:** New Unity Input System for controls.
    *   **TextMeshPro:** For high-quality text rendering.

---

## 📥 Installation & Setup

1.  **Prerequisites:**
    *   Install [Unity Hub](https://unity.com/download).
    *   Install **Unity Editor version 2022.3.x** (LTS recommended).
    *   Git (for cloning).

2.  **Clone the Repository:**
    ```bash
    git clone https://github.com/YourUsername/HarvestDefense.git
    cd HarvestDefense
    ```

3.  **Open in Unity:**
    *   Open Unity Hub.
    *   Click **Add** -> Select the `HarvestDefense` folder.
    *   Click the project to open it (allow time for initial library import).

4.  **First Run:**
    *   Navigate to `Assets/Game/Scenes/`.
    *   Open `Scene_15` (Main Game Loop) or `BossScene` for the boss encounter.
    *   Press the **Play** button.

---

## 🎮 Usage Instructions

### Controls
| Action | Input (Keyboard/Mouse) |
| :--- | :--- |
| **Move** | `W`, `A`, `S`, `D` or Arrow Keys |
| **Interact / Attack** | Left Mouse Button / `Space` |
| **Use Tool / Item** | `E` |
| **Switch Item** | Mouse Scroll or `1`-`9` |
| **Open Shop** | Interact with the Market Stall |
| **Pause Menu** | `Esc` |

### Gameplay Loop
1.  **Daytime:** Plant seeds, water crops, and sell produce at the market.
2.  **Preparation:** Buy spells (`Explosion`, `Nuke`, `Heal`) and upgrades.
3.  **Nighttime:** Defend against spawning enemy waves (Zombies, Spiders).
4.  **Boss:** Enter the specialized **Boss Zone** (Red square gizmo) to trigger the final encounter.

---

## 🔑 Environment Variables & API Keys

*   **No external API keys required.** This project runs entirely locally.
*   No `.env` file setup is needed for standard gameplay.

---

## 🐛 Known Issues & Troubleshooting

*   **Scene Transition:** If the player is invisible after entering the Boss Zone from the main scene, ensure you are starting from a scene that initializes `GameManager` correctly.
    *   *Fix:* The `BossZoneTrigger` handles clearing old player references automatically.
*   **UI Elements:** If Health/Spell bars appear distorted, ensure your Game View is set to **1920x1080** or a standard 16:9 aspect ratio.
*   **Compilation Errors:** If you see "The type or namespace name 'Cinemachine' could not be found", verify the CinemaChine package is installed via Window > Package Manager.

---

## 📄 License & Credits

### Credits
*   **Base Assets:** "Happy Harvest" 2D Farming Pack (Unity Technologies).
*   **AI Assistance:** Code generation, debugging, and system architecture support provided by Claude & Antigravity.

### License
This project is licensed under the [MIT License](LICENSE).
