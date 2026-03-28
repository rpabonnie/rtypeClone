using Raylib_cs;
using rtypeClone.Core;
using rtypeClone.Systems.ModuleSystem;
using ModuleSystemClass = rtypeClone.Systems.ModuleSystem.ModuleSystem;

namespace rtypeClone.Systems.UI;

/// <summary>
/// Loadout screen showing the player's 4 weapon slots and their stats.
/// When debug overlay is active (F3), a debug panel lets you equip any registered module.
/// </summary>
public class LoadoutScreen
{
    private const int SlotBoxWidth = 380;
    private const int SlotBoxHeight = 200;
    private const int SlotSpacing = 24;
    private const int Columns = 2;
    private const int Rows = 2;

    // Debug panel constants
    private const int DebugPanelWidth = 400;
    private const int DebugPanelItemHeight = 28;
    private const int DebugPanelMaxVisible = 16;

    private int _cursorSlot;
    private int _cursorSubSlot; // -1 = weapon, 0..1 = support slot
    private bool _shouldClose;

    // Debug panel state
    private bool _debugPanelOpen;
    private int _debugCursor;
    private int _debugScrollOffset;
    private ModuleDefinition[] _debugModuleList = [];

    public bool ShouldClose => _shouldClose;

    public void Reset()
    {
        _cursorSlot = 0;
        _cursorSubSlot = -1;
        _shouldClose = false;
        _debugPanelOpen = false;
        _debugCursor = 0;
        _debugScrollOffset = 0;
    }

    public void Update(InputManager input, bool debugOverlay, ModuleSystemClass moduleSystem)
    {
        if (_debugPanelOpen)
        {
            UpdateDebugPanel(input, moduleSystem);
            return;
        }

        // Navigation across the 2x2 grid + sub-slots
        int col = _cursorSlot % Columns;
        int row = _cursorSlot / Columns;

        if (input.NavigateRight)
        {
            if (_cursorSubSlot < PlayerLoadout.SupportSlotCount - 1)
                _cursorSubSlot++;
            else if (col < Columns - 1)
            {
                col++;
                _cursorSubSlot = -1;
            }
        }
        if (input.NavigateLeft)
        {
            if (_cursorSubSlot > -1)
                _cursorSubSlot--;
            else if (col > 0)
            {
                col--;
                _cursorSubSlot = PlayerLoadout.SupportSlotCount - 1;
            }
        }
        if (input.NavigateDown && row < Rows - 1)
        {
            row++;
            _cursorSubSlot = -1;
        }
        if (input.NavigateUp && row > 0)
        {
            row--;
            _cursorSubSlot = -1;
        }

        _cursorSlot = row * Columns + col;

        // Debug panel: Confirm opens it when debug overlay is active
        if (debugOverlay && input.ConfirmPressed)
        {
            OpenDebugPanel(moduleSystem);
            return;
        }

        // Close on cancel (B / Escape) or menu button
        if (input.CancelPressed || input.MenuPressed)
            _shouldClose = true;
    }

    private void OpenDebugPanel(ModuleSystemClass moduleSystem)
    {
        // Build the list: filter by category based on what sub-slot we're on
        var all = moduleSystem.Registry.All;
        var filtered = new List<ModuleDefinition>();

        // Add a "Clear" sentinel (null ID handled in equip logic)
        if (_cursorSubSlot == -1)
        {
            foreach (var m in all)
                if (m.Category == ModuleCategory.Weapon)
                    filtered.Add(m);
        }
        else
        {
            foreach (var m in all)
                if (m.Category == ModuleCategory.Support)
                    filtered.Add(m);
        }

        _debugModuleList = filtered.ToArray();
        _debugCursor = 0;
        _debugScrollOffset = 0;
        _debugPanelOpen = true;
    }

    private void UpdateDebugPanel(InputManager input, ModuleSystemClass moduleSystem)
    {
        if (input.CancelPressed)
        {
            _debugPanelOpen = false;
            return;
        }

        if (input.NavigateDown && _debugCursor < _debugModuleList.Length)
            _debugCursor++;
        if (input.NavigateUp && _debugCursor > 0)
            _debugCursor--;

        // Keep cursor in visible range
        if (_debugCursor < _debugScrollOffset)
            _debugScrollOffset = _debugCursor;
        if (_debugCursor >= _debugScrollOffset + DebugPanelMaxVisible)
            _debugScrollOffset = _debugCursor - DebugPanelMaxVisible + 1;

        if (input.ConfirmPressed)
        {
            // Index 0 = "Clear slot"
            if (_debugCursor == 0)
            {
                moduleSystem.Loadout.ClearSlot(_cursorSlot, _cursorSubSlot);
            }
            else
            {
                int moduleIndex = _debugCursor - 1;
                if (moduleIndex < _debugModuleList.Length)
                {
                    var def = _debugModuleList[moduleIndex];
                    if (_cursorSubSlot == -1)
                        moduleSystem.Loadout.TryEquipWeapon(_cursorSlot, def.Id, moduleSystem.Registry);
                    else
                        moduleSystem.Loadout.TryEquipSupport(_cursorSlot, _cursorSubSlot, def.Id, moduleSystem.Registry);
                }
            }
            moduleSystem.RebuildCache();
            _debugPanelOpen = false;
        }
    }

    public void Draw(ModuleSystemClass moduleSystem, bool debugOverlay)
    {
        // Dim overlay
        Raylib.DrawRectangle(0, 0, Constants.ScreenWidth, Constants.ScreenHeight,
            new Color((byte)0, (byte)0, (byte)0, (byte)200));

        // Title
        const string title = "MODULE BAY";
        int titleWidth = Raylib.MeasureText(title, 48);
        Raylib.DrawText(title,
            Constants.ScreenWidth / 2 - titleWidth / 2, 60, 48, Color.White);

        // Calculate grid origin (centered)
        int gridWidth = Columns * SlotBoxWidth + (Columns - 1) * SlotSpacing;
        int gridHeight = Rows * SlotBoxHeight + (Rows - 1) * SlotSpacing;
        int gridX = Constants.ScreenWidth / 2 - gridWidth / 2;
        int gridY = 140;

        for (int slot = 0; slot < PlayerLoadout.WeaponSlotCount; slot++)
        {
            int col = slot % Columns;
            int row = slot / Columns;
            int boxX = gridX + col * (SlotBoxWidth + SlotSpacing);
            int boxY = gridY + row * (SlotBoxHeight + SlotSpacing);

            bool isSelectedSlot = slot == _cursorSlot;
            DrawSlotBox(boxX, boxY, slot, isSelectedSlot, _cursorSubSlot, moduleSystem);
        }

        // Controls hint
        string hint = debugOverlay
            ? "[D-Pad/Arrows] Navigate   [A/Enter] Equip (debug)   [B/Esc] Close"
            : "[D-Pad/Arrows] Navigate   [B/Esc] Close";
        int hintWidth = Raylib.MeasureText(hint, 20);
        Raylib.DrawText(hint,
            Constants.ScreenWidth / 2 - hintWidth / 2,
            gridY + gridHeight + 40,
            20, Color.DarkGray);

        if (debugOverlay)
        {
            Raylib.DrawText("[DEBUG] F3 active — press A/Enter on a slot to equip modules",
                gridX, gridY + gridHeight + 70, 18, Color.Lime);
        }

        // Debug panel overlay
        if (_debugPanelOpen)
            DrawDebugPanel(moduleSystem);
    }

    private static void DrawSlotBox(int x, int y, int slot, bool isSelectedSlot, int cursorSubSlot, ModuleSystemClass moduleSystem)
    {
        // Box background
        Color bgColor = isSelectedSlot
            ? new Color((byte)40, (byte)40, (byte)80, (byte)255)
            : new Color((byte)20, (byte)20, (byte)40, (byte)255);
        Raylib.DrawRectangle(x, y, SlotBoxWidth, SlotBoxHeight, bgColor);

        // Border — highlight weapon slot when sub-slot is -1
        bool weaponHighlighted = isSelectedSlot && cursorSubSlot == -1;
        Color borderColor = weaponHighlighted ? Color.Yellow
            : isSelectedSlot ? new Color((byte)120, (byte)120, (byte)180, (byte)255)
            : new Color((byte)80, (byte)80, (byte)120, (byte)255);
        float borderThick = weaponHighlighted ? 2f : 1f;
        Raylib.DrawRectangleLinesEx(new Rectangle(x, y, SlotBoxWidth, SlotBoxHeight), borderThick, borderColor);

        // Slot label
        string slotLabel = $"Slot {slot + 1}";
        Raylib.DrawText(slotLabel, x + 12, y + 8, 20, Color.Gray);

        var moduleId = moduleSystem.Loadout.WeaponModules[slot].ModuleId;
        if (moduleId == null)
        {
            Raylib.DrawText("[ Empty ]", x + 12, y + 40, 24, new Color((byte)100, (byte)100, (byte)100, (byte)255));
            DrawSupportSlots(x, y, slot, isSelectedSlot, cursorSubSlot, moduleSystem, supY: y + 94);
            return;
        }

        if (!moduleSystem.Registry.TryGet(moduleId, out var def) || def == null)
            return;

        // Module name
        Raylib.DrawText(def.DisplayName, x + 12, y + 36, 28, Color.White);

        // Category tag
        string catTag = def.WeaponCategory?.ToString() ?? def.Category.ToString();
        Raylib.DrawText(catTag, x + 12, y + 68, 18, Color.SkyBlue);

        // Base stats
        ref readonly var baseP = ref moduleSystem.ResolvedParameters[slot];
        int statY = y + 94;
        Raylib.DrawText($"Normal:  {baseP.Damage} dmg  {baseP.Speed:0} spd", x + 12, statY, 18, Color.LightGray);

        // Charged stats (if available)
        if (moduleSystem.HasChargedMode[slot])
        {
            ref readonly var chargedP = ref moduleSystem.ResolvedChargedParameters[slot];
            Raylib.DrawText($"Charged: {chargedP.Damage} dmg  {chargedP.Speed:0} spd",
                x + 12, statY + 22, 18, Color.Orange);
        }

        // Support slots
        DrawSupportSlots(x, y, slot, isSelectedSlot, cursorSubSlot, moduleSystem, statY + 52);
    }

    private static void DrawSupportSlots(int x, int y, int slot, bool isSelectedSlot, int cursorSubSlot,
        ModuleSystemClass moduleSystem, int supY)
    {
        Raylib.DrawText("Supports:", x + 12, supY, 16, Color.Gray);
        for (int s = 0; s < PlayerLoadout.SupportSlotCount; s++)
        {
            var supId = moduleSystem.Loadout.SupportModules[slot, s].ModuleId;
            string supText = supId != null ? supId : "---";
            Color supColor;
            int supX = x + 100 + s * 140;

            bool highlighted = isSelectedSlot && cursorSubSlot == s;
            if (highlighted)
            {
                supColor = Color.Yellow;
                Raylib.DrawText(">", supX - 14, supY, 16, Color.Yellow);
            }
            else
            {
                supColor = supId != null ? Color.White : new Color((byte)60, (byte)60, (byte)60, (byte)255);
            }

            // Look up display name
            if (supId != null && moduleSystem.Registry.TryGet(supId, out var supDef) && supDef != null)
                supText = supDef.DisplayName;

            Raylib.DrawText(supText, supX, supY, 16, supColor);
        }
    }

    private void DrawDebugPanel(ModuleSystemClass moduleSystem)
    {
        // Semi-transparent backdrop
        int panelX = Constants.ScreenWidth / 2 - DebugPanelWidth / 2;
        int panelY = 160;
        int totalItems = _debugModuleList.Length + 1; // +1 for "Clear"
        int visibleCount = Math.Min(totalItems, DebugPanelMaxVisible);
        int panelHeight = visibleCount * DebugPanelItemHeight + 60;

        Raylib.DrawRectangle(panelX - 4, panelY - 4, DebugPanelWidth + 8, panelHeight + 8,
            new Color((byte)0, (byte)0, (byte)0, (byte)230));
        Raylib.DrawRectangleLines(panelX - 4, panelY - 4, DebugPanelWidth + 8, panelHeight + 8, Color.Lime);

        string panelTitle = _cursorSubSlot == -1 ? "DEBUG: Select Weapon Module" : "DEBUG: Select Support Module";
        Raylib.DrawText(panelTitle, panelX + 8, panelY + 8, 20, Color.Lime);

        int listY = panelY + 36;
        for (int i = _debugScrollOffset; i < Math.Min(totalItems, _debugScrollOffset + DebugPanelMaxVisible); i++)
        {
            int drawY = listY + (i - _debugScrollOffset) * DebugPanelItemHeight;
            bool selected = i == _debugCursor;
            Color textColor = selected ? Color.Yellow : Color.White;

            if (selected)
                Raylib.DrawRectangle(panelX, drawY, DebugPanelWidth, DebugPanelItemHeight,
                    new Color((byte)60, (byte)60, (byte)100, (byte)200));

            if (i == 0)
            {
                Raylib.DrawText("[ Clear Slot ]", panelX + 12, drawY + 4, 18, selected ? Color.Yellow : Color.Red);
            }
            else
            {
                var def = _debugModuleList[i - 1];
                Raylib.DrawText(def.DisplayName, panelX + 12, drawY + 4, 18, textColor);

                // Show brief stats on right side
                string info = def.Category == ModuleCategory.Weapon
                    ? $"{def.WeaponCategory}  {def.BaseProjectileParameters.Damage}dmg"
                    : FormatModifiers(def.Modifiers);
                int infoWidth = Raylib.MeasureText(info, 14);
                Raylib.DrawText(info, panelX + DebugPanelWidth - infoWidth - 12, drawY + 6, 14, Color.Gray);
            }
        }

        // Scroll indicator
        if (totalItems > DebugPanelMaxVisible)
        {
            if (_debugScrollOffset > 0)
                Raylib.DrawText("▲", panelX + DebugPanelWidth - 20, listY - 14, 14, Color.Gray);
            if (_debugScrollOffset + DebugPanelMaxVisible < totalItems)
                Raylib.DrawText("▼", panelX + DebugPanelWidth - 20, listY + DebugPanelMaxVisible * DebugPanelItemHeight, 14, Color.Gray);
        }
    }

    private static string FormatModifiers(ModuleModifiers m)
    {
        var parts = new List<string>(4);
        if (m.DamageFlat != 0) parts.Add($"+{m.DamageFlat}dmg");
        if (m.DamageMultiplier != 1f) parts.Add($"x{m.DamageMultiplier:0.#}dmg");
        if (m.PierceDelta != 0) parts.Add($"+{m.PierceDelta}pierce");
        if (m.SpeedFlat != 0f) parts.Add($"+{m.SpeedFlat:0}spd");
        if (m.HomingOverride) parts.Add("homing");
        if (m.CountDelta != 0) parts.Add($"+{m.CountDelta}count");
        return parts.Count > 0 ? string.Join(" ", parts) : "---";
    }
}
