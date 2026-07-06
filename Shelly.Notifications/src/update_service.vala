using GLib;

public class SyncPackage {
    public string name;
    public string version;
    public string? old_version;

    public SyncPackage (string name, string version, string? old_version = null) {
        this.name = name;
        this.version = version;
        this.old_version = old_version;
    }

    public string display_label () {
        if (old_version != null && old_version.length > 0)
            return "%s  %s → %s".printf (name, old_version, version);
        return "%s  %s".printf (name, version);
    }
}

public class SyncFlatpak {
    public string id;
    public string? name;
    public string version;

    public SyncFlatpak (string id, string? name, string version) {
        this.id = id;
        this.name = name;
        this.version = version;
    }

    public string display_label () {
        var display = (name != null && name.length > 0) ? name : id;
        return "%s  %s".printf (display, version);
    }
}

public class SyncModel {
    public GenericArray<SyncPackage> packages = new GenericArray<SyncPackage> ();
    public GenericArray<SyncPackage> aur = new GenericArray<SyncPackage> ();
    public GenericArray<SyncFlatpak> flatpaks = new GenericArray<SyncFlatpak> ();

    public int total () {
        return (int) (packages.length + aur.length + flatpaks.length);
    }
}

public class UpdateService : Object {

    private string find_cli () {
        string[] candidates = { "/usr/bin/shelly", "/usr/local/bin/shelly", "/opt/shelly/shelly" };
        foreach (var p in candidates) {
            if (FileUtils.test (p, FileTest.IS_EXECUTABLE))return p;
        }
        return "shelly";
    }

    public async SyncModel check_for_updates () throws Error {
        var cli = find_cli ();
        string[] argv = { cli, "check-updates", "-al", "--json" };
        stdout.printf ("[shelly-updates] Running: %s check-updates -al --json\n", cli);

        var proc = new Subprocess.newv (
                                        argv, SubprocessFlags.STDOUT_PIPE | SubprocessFlags.STDERR_PIPE
        );

        var reader = new DataInputStream (proc.get_stdout_pipe ());
        var sb = new StringBuilder ();
        string? line;
        while ((line = yield reader.read_line_async (Priority.DEFAULT, null)) != null) {
            sb.append (line);
            sb.append_c ('\n');
        }

        yield proc.wait_async (null);

        if (!proc.get_successful ())
            stdout.printf ("[shelly-updates] CLI exited %d\n", proc.get_exit_status ());

        return parse_output (sb.str.strip ());
    }

    private SyncModel parse_output (string raw) {
        var model = new SyncModel ();
        if (raw.length == 0)return model;

        var json_start = raw.index_of ("{");
        if (json_start < 0) { stdout.printf ("[shelly-updates] No JSON in output\n"); return model; }

        try {
            var parser = new Json.Parser ();
            parser.load_from_data (raw.substring (json_start));

            var root = parser.get_root ();
            if (root == null || root.get_node_type () != Json.NodeType.OBJECT)return model;
            var obj = root.get_object ();

            if (obj.has_member ("Packages")) {
                obj.get_array_member ("Packages").foreach_element ((_, __, node) => {
                    var o = node.get_object ();
                    string? old_v = null;
                    if (o.has_member ("OldVersion")
                        && o.get_member ("OldVersion").get_node_type () != Json.NodeType.NULL)
                        old_v = o.get_string_member ("OldVersion");
                    model.packages.add (new SyncPackage (
                                                         o.get_string_member ("Name"),
                                                         o.get_string_member ("Version"),
                                                         old_v
                    ));
                });
            }

            if (obj.has_member ("Aur")) {
                obj.get_array_member ("Aur").foreach_element ((_, __, node) => {
                    var o = node.get_object ();
                    string? old_v = null;
                    if (o.has_member ("OldVersion")
                        && o.get_member ("OldVersion").get_node_type () != Json.NodeType.NULL)
                        old_v = o.get_string_member ("OldVersion");
                    model.aur.add (new SyncPackage (
                                                    o.get_string_member ("Name"),
                                                    o.get_string_member ("Version"),
                                                    old_v
                    ));
                });
            }

            if (obj.has_member ("Flatpaks")) {
                obj.get_array_member ("Flatpaks").foreach_element ((_, __, node) => {
                    var o = node.get_object ();
                    string? fname = null;
                    if (o.has_member ("Name")
                        && o.get_member ("Name").get_node_type () != Json.NodeType.NULL)
                        fname = o.get_string_member ("Name");
                    model.flatpaks.add (new SyncFlatpak (
                                                         o.get_string_member ("Id"),
                                                         fname,
                                                         o.get_string_member ("Version")
                    ));
                });
            }

            stdout.printf ("[shelly-updates] %d shelly  %d AUR  %d Flatpak  → %d total\n",
                           (int) model.packages.length, (int) model.aur.length,
                           (int) model.flatpaks.length, model.total ());
        } catch (Error e) {
            printerr ("[shelly-notifications] JSON parse error: %s\n", e.message);
        }

        return model;
    }
}
