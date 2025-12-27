# How to Use AI Game Cheating

You can now use Google's Gemini Pro directly inside SaveState to chat about games and, more importantly, to **create cheats on the fly**.

## 1. Setup

1.  **Get a Gemini API Key**:
    *   Go to [Google AI Studio](https://aistudio.google.com/app/apikey).
    *   Create a new API key (it's free).
2.  **Configure SaveState**:
    *   Open SaveState.
    *   Go to **Settings**.
    *   Scroll down to the **Google Gemini** section.
    *   Paste your API Key.
    *   Click **Save Settings**.

## 2. Using the Chatbot

*   Click on the **AI Assistant** tab in the chatbot.
*   You can ask general questions like "Tips for Final Fantasy VI" or "What game should I play next?".

## 3. Hacking Games (The Agent)

The AI can now control a memory scanner to find health, ammo, or other values for you.

### Step-by-Step Guide: Scanning for Health

1.  **Launch your Game**: Start your game (e.g., Final Fantasy VI on Steam).
2.  **Attach SaveState**:
    *   In the AI Chat, type: `Attach to <ProcessName>`
    *   Example: `Attach to ff6` or `Attach to Final Fantasy`
    *   The AI should reply: "Successfully attached to ff6_pr.exe (1234)".
3.  **Start Scanning**:
    *   Look at your current Health (e.g., **100**).
    *   Tell the AI: `Scan for 100`.
    *   *The System will perform a memory scan.*
4.  **Filter Results**:
    *   Take damage in the game (e.g., Health goes to **90**).
    *   Tell the AI: `Next scan 90`.
    *   *The System will filter the address list.*
5.  **Repeat**:
    *   Repeat the "Take Damage -> Next Scan" process until only a few addresses remain (usually 1-3).
    *   The AI will listing the found addresses.
6.  **Hack It**:
    *   Tell the AI: `Write 9999 to address <address>`.
    *   *Check your game, you should have 9999 health!*

## Troubleshooting
*   **"No process attached"**: Make sure you type "Attach to..." first.
*   **Scanning takes too long**: The first scan might be slow if the game uses a lot of memory.
*   **Admin Rights**: SaveState might need to run as **Administrator** to access some games' memory.
