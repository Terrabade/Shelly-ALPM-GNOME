using GLib;

public class NextNotification {

    public static uint get_next_seconds (ShellyConfig config) {
        var fallback = (uint) (config.tray_check_interval_hours * 3600);

        if (!config.use_weekly_schedule
            || config.scheduled_days.length == 0
            || config.scheduled_hour < 0) {
            return fallback;
        }

        var now = new DateTime.now_local ();
        int today = (int) now.get_day_of_week () % 7;

        int now_of_day = now.get_hour () * 3600 + now.get_minute () * 60 + now.get_second ();
        int sched_of_day = config.scheduled_hour * 3600 + config.scheduled_minute * 60;

        if (day_in_schedule (today, config.scheduled_days)) {
            int remaining = sched_of_day - now_of_day;
            if (remaining > 0) {
                stdout.printf ("[shelly-scheduler] Next check today in %.1f h\n",
                               remaining / 3600.0);
                return (uint) remaining;
            }
        }

        for (int i = 1; i <= 7; i++) {
            int candidate = (today + i) % 7;
            if (day_in_schedule (candidate, config.scheduled_days)) {
                uint secs = (uint) ((86400 - now_of_day) + (i - 1) * 86400 + sched_of_day);
                stdout.printf ("[shelly-scheduler] Next check in %d day(s) (%.1f h)\n",
                               i, secs / 3600.0);
                return secs;
            }
        }

        return fallback;
    }

    private static bool day_in_schedule (int day, int[] scheduled) {
        foreach (var d in scheduled)if (d == day)return true;
        return false;
    }
}
