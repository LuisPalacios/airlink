using AirLink.Services;

namespace AirLink;

/// <summary>
/// Minimal dialog for capturing a global hotkey combination.
/// Built programmatically — no designer file needed.
/// </summary>
public sealed class HotkeyConfigForm : Form
{
    private readonly TextBox _hotkeyTextBox;
    private readonly CheckBox _enabledCheckBox;
    private readonly Button _okButton;
    private readonly Button _cancelButton;
    private readonly Label _hintLabel;

    private uint _capturedModifiers;
    private Keys _capturedKey;

    public uint ResultModifiers => _capturedModifiers;
    public Keys ResultKey => _capturedKey;
    public bool ResultEnabled => _enabledCheckBox.Checked;

    public HotkeyConfigForm(uint currentModifiers, Keys currentKey, bool isEnabled)
    {
        _capturedModifiers = currentModifiers;
        _capturedKey = currentKey;

        Text = "AirLink - Configure Shortcut";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(360, 230);
        KeyPreview = true;

        var shortcutLabel = new Label
        {
            Text = "Press a key combination:",
            Location = new Point(16, 16),
            AutoSize = true,
        };

        _hotkeyTextBox = new TextBox
        {
            Location = new Point(16, 40),
            Size = new Size(310, 28),
            ReadOnly = true,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            TextAlign = HorizontalAlignment.Center,
            Text = HotkeyService.FormatHotkey(currentModifiers, currentKey),
        };

        _hintLabel = new Label
        {
            Text = "Use Ctrl, Alt, Shift, Win + a letter or number key",
            Location = new Point(16, 72),
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
        };

        _enabledCheckBox = new CheckBox
        {
            Text = "Enable shortcut",
            Location = new Point(16, 96),
            AutoSize = true,
            Checked = isEnabled,
        };

        _okButton = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Location = new Point(160, 128),
            Size = new Size(80, 28),
        };

        _cancelButton = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Location = new Point(246, 128),
            Size = new Size(80, 28),
        };

        AcceptButton = _okButton;
        CancelButton = _cancelButton;

        Controls.AddRange([shortcutLabel, _hotkeyTextBox, _hintLabel, _enabledCheckBox, _okButton, _cancelButton]);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        e.Handled = true;
        e.SuppressKeyPress = true;

        // Ignore standalone modifier presses
        if (e.KeyCode is Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin or Keys.RWin)
        {
            base.OnKeyDown(e);
            return;
        }

        // Require at least one modifier
        uint modifiers = 0;
        if (e.Control) modifiers |= HotkeyService.MOD_CONTROL;
        if (e.Alt) modifiers |= HotkeyService.MOD_ALT;
        if (e.Shift) modifiers |= HotkeyService.MOD_SHIFT;

        // Detect Win key via GetAsyncKeyState
        if ((GetAsyncKeyState(Keys.LWin) & 0x8000) != 0 ||
            (GetAsyncKeyState(Keys.RWin) & 0x8000) != 0)
        {
            modifiers |= HotkeyService.MOD_WIN;
        }

        if (modifiers == 0)
        {
            base.OnKeyDown(e);
            return;
        }

        _capturedModifiers = modifiers;
        _capturedKey = e.KeyCode;
        _hotkeyTextBox.Text = HotkeyService.FormatHotkey(_capturedModifiers, _capturedKey);

        base.OnKeyDown(e);
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(Keys vKey);
}
