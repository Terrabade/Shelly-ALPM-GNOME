public delegate void ReceiverCallback ();

[DBus (name = "org.shelly.Notifications")]
public class ShellyUiReceiver : Object {

    private ReceiverCallback? _on_refresh_settings;
    private ReceiverCallback? _on_updates_made_in_ui;

    public ShellyUiReceiver (owned ReceiverCallback? on_refresh_settings = null,
        owned ReceiverCallback? on_updates_made_in_ui = null) {
        _on_refresh_settings = (owned) on_refresh_settings;
        _on_updates_made_in_ui = (owned) on_updates_made_in_ui;
    }

    public void refresh_settings () throws DBusError, IOError {
        stdout.printf ("[shelly-ui-receiver] RefreshSettings\n");
        if (_on_refresh_settings != null)_on_refresh_settings ();
    }

    public void updates_made_in_ui () throws DBusError, IOError {
        stdout.printf ("[shelly-ui-receiver] UpdatesMadeInUi\n");
        if (_on_updates_made_in_ui != null)_on_updates_made_in_ui ();
    }
}
