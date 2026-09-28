using System.Windows;
using System.Windows.Threading;
using at365.Clipboard365;
using at365.Common365;
using at365.Gesture365;
using at365.Native365;
using System.IO;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        (string Name, Action Run)[] tests =
        [
            ("All modules are disposed once in reverse order", DisposeModules),
            ("Failed initialization releases resources and can retry", InitializationFailure),
            ("Clipboard keeps repeated-text and non-text cleanup behavior", ClipboardPolicy),
            ("Failed clipboard clear retains previous comparison", ClipboardFailure),
            ("Throttle defers, coalesces and cancels on dispose", Throttle),
            ("Throttle can schedule another action from a callback", ReentrantThrottle),
            ("Gesture start/end stay ordered under rapid clicks", RapidGestures),
            ("Gesture action is selected before button release", GestureAction),
            ("Disposal cancels pending gesture replay", GestureDisposal),
            ("Unknown long movement keeps directions until release", LongMovement),
            ("Keyboard chord is one ordered batch with extended flags", KeyboardBatch),
            ("Clock refreshes at second zero and across hidden intervals", ClockRefresh),
            ("JSON settings round-trip every persisted option", SettingsRoundTrip),
            ("Existing JSON takes priority and missing properties use defaults", SettingsDefaults),
            ("Legacy settings migrate only when JSON is absent", SettingsMigration),
            ("Legacy reader still exposes all four typed settings", LegacySettingsReader),
            ("Broken JSON is backed up before using defaults", SettingsCorruption),
            ("Failed save preserves the previous JSON", SettingsSaveFailure),
        ];
        var failures = 0;
        foreach (var (name, run) in tests)
        {
            try { run(); Console.WriteLine($"PASS {name}"); }
            catch (Exception error) { failures++; Console.Error.WriteLine($"FAIL {name}: {error}"); }
        }
        Console.WriteLine($"{tests.Length - failures}/{tests.Length} passed");
        return failures == 0 ? 0 : 1;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void DisposeModules()
    {
        var order = new List<int>();
        var first = new ProbeModule(1, order);
        var second = new ProbeModule(2, order);
        first.Start(); second.Start();
        ModuleBase.DisposeAll(); ModuleBase.DisposeAll();
        ((IDisposable)first).Dispose();
        Assert(order.SequenceEqual(new[] { 2, 1 }), "Module cleanup was skipped, reordered or duplicated.");
    }

    private static void InitializationFailure()
    {
        var order = new List<int>();
        try { new ProbeModule(3, order, true).Start(); }
        catch (InvalidOperationException) { }
        Assert(order.SequenceEqual(new[] { 3 }), "Partial initialization leaked resources.");
        var attempts = 0;
        try { LazyInitializer<RetryProbe>.GetInstance(_ => { attempts++; throw new InvalidOperationException(); }); }
        catch (InvalidOperationException) { }
        _ = LazyInitializer<RetryProbe>.GetInstance(_ => attempts++);
        Assert(attempts == 2, "A partially initialized singleton was published.");
    }

    private static DataObject Text(string text) => new(DataFormats.Text, text);

    private static void ClipboardPolicy()
    {
        using var module = (IDisposable)new ClipboardModule();
        var clipboard = (ClipboardModule)module;
        var clears = 0;
        clipboard.CleanupClipboard(null, () => clears++);
        Assert(clears == 0, "Null data object should skip cleanup.");
        clipboard.CleanupClipboard(Text("A"), () => clears++);
        clipboard.CleanupClipboard(Text("B"), () => clears++);
        Assert(clears == 0, "Changed text should not be cleared.");
        clipboard.CleanupClipboard(Text("B"), () => clears++);
        Assert(clears == 1, "Re-copied identical text must still be cleared.");
        var files = new DataObject(DataFormats.FileDrop, new[] { @"C:\sample.txt" });
        clipboard.CleanupClipboard(files, () => clears++);
        Assert(clears == 2, "Non-text must still compare as null and clear after reset.");
        clipboard.CleanupClipboard(Text("C"), () => clears++);
        clipboard.CleanupClipboard(files, () => clears++);
        Assert(clears == 2, "Transition from text to non-text should first update preview.");
        clipboard.CleanupClipboard(files, () => clears++);
        Assert(clears == 3, "Repeated non-text must still clear.");
    }

    private static void ClipboardFailure()
    {
        using var disposable = (IDisposable)new ClipboardModule();
        var module = (ClipboardModule)disposable;
        module.CleanupClipboard(Text("A"), () => { });
        try { module.CleanupClipboard(Text("A"), () => throw new InvalidOperationException()); }
        catch (InvalidOperationException) { }
        var clears = 0;
        module.CleanupClipboard(Text("A"), () => clears++);
        Assert(clears == 1, "Failed clear should not reset preview.");
    }

    private static void Throttle()
    {
        var calls = new List<int>();
        using (var throttle = new ThrottleDispatcher(TimeSpan.FromMilliseconds(20)))
        {
            throttle.Throttle(() => calls.Add(1));
            throttle.Throttle(() => calls.Add(2));
            Assert(calls.Count == 0, "Action ran inside the caller/hook.");
            PumpUntil(() => calls.Count == 1);
            Assert(calls.SequenceEqual(new[] { 2 }), "Actions were not coalesced.");
            throttle.Throttle(() => calls.Add(3));
        }
        PumpFor(TimeSpan.FromMilliseconds(60));
        Assert(calls.Count == 1, "Action ran after disposal.");
    }

    private static void ReentrantThrottle()
    {
        using var throttle = new ThrottleDispatcher(TimeSpan.FromMilliseconds(1));
        var count = 0;
        throttle.Throttle(() => { count++; throttle.Throttle(() => count++); });
        PumpUntil(() => count == 2);
    }

    private static void RapidGestures()
    {
        var replays = 0;
        using var provider = new MouseGestureProvider(_ => replays++);
        for (var i = 0; i < 100; i++)
        {
            Assert(provider.BeginGesture(GestureButton.Right, "test.exe"), "Start failed.");
            Assert(provider.IsReady(), "State was not published synchronously.");
            Assert(!provider.BeginGesture(GestureButton.Middle, "test.exe"), "Overlapping start accepted.");
            Assert(!provider.EndGesture(GestureButton.Middle), "Wrong button ended gesture.");
            Assert(provider.EndGesture(GestureButton.Right), "Immediate release missed gesture.");
            Assert(!provider.IsReady(), "State was not reset synchronously.");
        }
        PumpUntil(() => replays == 100);
    }

    private static void GestureAction()
    {
        var actions = 0;
        var replays = 0;
        MouseGestureManager.Instance.RegisterMouseAction(MouseTrigger.LeftButtonDown, () => actions++, ["test.exe"]);
        using var provider = new MouseGestureProvider(_ => replays++);
        provider.BeginGesture(GestureButton.Right, "test.exe");
        Assert(provider.ExecuteAction(MouseTrigger.LeftButtonDown), "Action was not found.");
        Assert(provider.IsHandled(), "Handled state was delayed.");
        Assert(actions == 0, "Action ran in hook callback.");
        provider.EndGesture(GestureButton.Right);
        PumpUntil(() => actions == 1);
        Assert(replays == 0, "Handled gesture replayed its click.");
    }

    private static void GestureDisposal()
    {
        var replays = 0;
        var provider = new MouseGestureProvider(_ => replays++);
        provider.BeginGesture(GestureButton.Right, "test.exe");
        provider.EndGesture(GestureButton.Right);
        provider.Dispose();
        PumpFor(TimeSpan.FromMilliseconds(40));
        Assert(replays == 0, "Replay ran after disposal.");
    }

    private static void KeyboardBatch()
    {
        var inputs = KeyboardSimulator.BuildInputs([NativeMethods.VK_CONTROL, NativeMethods.VK_SHIFT], NativeMethods.VK_TAB);
        Assert(inputs.Length == 6, "Chord event count is incorrect.");
        Assert(inputs.Select(input => (uint)input.u.ki.wVk).SequenceEqual(new uint[] { NativeMethods.VK_CONTROL, NativeMethods.VK_SHIFT, NativeMethods.VK_TAB, NativeMethods.VK_TAB, NativeMethods.VK_SHIFT, NativeMethods.VK_CONTROL }), "Chord order is incorrect.");
        Assert(inputs.Take(3).All(input => input.u.ki.dwFlags == 0), "Unexpected down flags.");
        Assert(inputs.Skip(3).All(input => input.u.ki.dwFlags == NativeMethods.KEYEVENTF_KEYUP), "Missing key release.");
        var arrow = KeyboardSimulator.BuildInputs([NativeMethods.VK_LWIN], NativeMethods.VK_LEFT);
        Assert(arrow.All(input => (input.u.ki.dwFlags & NativeMethods.KEYEVENTF_EXTENDEDKEY) != 0), "Extended key flags are missing.");
        Assert(KeyboardSimulator.BuildInputs([], NativeMethods.VK_F11).Length == 2, "Unmodified key has extra events.");
    }

    private static void ClockRefresh()
    {
        var clock = new at365.Watch365.Watch();
        try
        {
            string Read(string name) => ((System.Windows.Controls.TextBlock)clock.FindName(name)).Text;
            clock.RefreshTime(new DateTime(2026, 9, 28, 10, 20, 0));
            Assert(Read("textBlockLeft") == "10:20" && Read("textBlockSecond") == ":00", "Initial second-zero update was skipped.");
            clock.RefreshTime(new DateTime(2026, 9, 28, 11, 20, 0));
            Assert(Read("textBlockLeft") == "11:20", "Same-second update after a hidden interval was skipped.");
            clock.RefreshTime(new DateTime(2026, 9, 29, 11, 20, 0));
            Assert(Read("textBlockRight") == "9/29 Tue", "Same-time update on the next day was skipped.");
            clock.RefreshTime(new DateTime(2026, 9, 29, 11, 20, 1));
            Assert(Read("textBlockSecond") == ":01" && Read("textBlockLeft") == "11:20", "Second-only update is incorrect.");
        }
        finally { clock.Close(); }
    }

    private static void LongMovement()
    {
        using var provider = new MouseGestureProvider(_ => { });
        var tracker = new GestureMoveTracker(provider);
        try
        {
            tracker.Check(new System.Drawing.Point(0, -60));
            tracker.Check(new System.Drawing.Point(60, -60));
            tracker.Check(new System.Drawing.Point(60, 0));
            tracker.Check(new System.Drawing.Point(0, 0));
            Assert(tracker.End().Count() == 4, "Unknown movement was cleared and would replay a right click.");
            Assert(!tracker.End().Any(), "Previous movement leaked into the next gesture.");
        }
        finally { tracker.Close(); }
    }

    private static void WithSettingsFile(Action<string> test)
    {
        var root = Path.GetFullPath(AppContext.BaseDirectory);
        var directory = Path.GetFullPath(Path.Combine(root, "config-tests-" + Guid.NewGuid().ToString("N")));
        Assert(directory.StartsWith(root, StringComparison.OrdinalIgnoreCase), "Test path escaped output directory.");
        Directory.CreateDirectory(directory);
        try { test(Path.Combine(directory, "nested", "config.json")); }
        finally { Directory.Delete(directory, recursive: true); }
    }

    private static ConfigurationStore TestStore(string path) => new(path, (_, _) => { });

    private static void SettingsRoundTrip() => WithSettingsFile(path =>
    {
        var store = TestStore(path);
        var initial = store.Load();
        Assert(File.Exists(path) && initial.Visible && !initial.AutoLockEnabled && initial.Monitor == 0 && initial.Alignment == 0, "Initial config/defaults missing.");
        initial.Monitor = 2;
        initial.Alignment = 2;
        initial.Visible = false;
        initial.AutoLockEnabled = true;
        initial.Blacklist = ["Example.EXE", "example.exe", " another.exe "];
        Assert(store.Save(initial), "Save failed.");
        var loaded = TestStore(path).Load();
        Assert(loaded.Monitor == 2 && loaded.Alignment == 2 && !loaded.Visible && loaded.AutoLockEnabled, "Settings did not survive reload.");
        Assert(loaded.Blacklist.SequenceEqual(new[] { "example.exe", "another.exe" }), "Blacklist did not survive normalization/reload.");
        Assert(Directory.GetFiles(Path.GetDirectoryName(path)!).Length == 1, "Temporary file was left behind.");
    });

    private static void SettingsDefaults() => WithSettingsFile(path =>
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "{\"Monitor\":-5,\"Alignment\":999,\"Blacklist\":null,\"Visible\":false}");
        var loaded = TestStore(path).Load(() => throw new InvalidOperationException("Legacy must not override JSON."));
        Assert(!loaded.Visible && !loaded.AutoLockEnabled && loaded.Monitor == 0 && loaded.Alignment == 0 && loaded.Blacklist.Length == 0, "Missing/invalid property defaults are incorrect.");
    });

    private static void SettingsMigration() => WithSettingsFile(path =>
    {
        var imports = 0;
        var loaded = TestStore(path).Load(() =>
        {
            imports++;
            return new ApplicationConfiguration { Monitor = 3, Alignment = 2, Visible = false, AutoLockEnabled = true, Blacklist = ["legacy.exe"] };
        });
        var restored = TestStore(path).Load(() => { imports++; return new(); });
        Assert(imports == 1 && File.Exists(path), "Migration must run once and persist JSON.");
        Assert(restored.Monitor == loaded.Monitor && restored.Alignment == 2 && !restored.Visible && restored.AutoLockEnabled && restored.Blacklist.Single() == "legacy.exe", "Migrated settings were lost.");
    });

    private static void LegacySettingsReader()
    {
        var legacy = new at365.Shell.Properties.Settings();
        Assert(legacy.Properties.Count == 4, "Legacy settings metadata changed.");
        _ = legacy.Monitor;
        _ = legacy.Alignment;
        _ = legacy.Visible;
        _ = legacy.AutoLockEnabled;
    }

    private static void SettingsCorruption() => WithSettingsFile(path =>
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        const string broken = "{\"Visible\":";
        File.WriteAllText(path, broken);
        var store = TestStore(path);
        var loaded = store.Load(() => throw new InvalidOperationException("Broken JSON is not a migration trigger."));
        Assert(loaded.Visible && !loaded.AutoLockEnabled, "Broken config did not fall back safely.");
        var backup = Directory.GetFiles(Path.GetDirectoryName(path)!, "config.json.invalid-*").Single();
        Assert(File.ReadAllText(backup) == broken && File.ReadAllText(path) == broken, "Broken input was not preserved.");
        Assert(store.Save(loaded) && TestStore(path).Load().Visible, "Could not save valid settings after recovery.");
    });

    private static void SettingsSaveFailure() => WithSettingsFile(path =>
    {
        var store = TestStore(path);
        var loaded = store.Load();
        var original = File.ReadAllText(path);
        loaded.Visible = false;
        using (var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
            Assert(!store.Save(loaded), "Saving over a locked file should fail.");
        Assert(File.ReadAllText(path) == original, "Failed save corrupted the previous config.");
        Assert(Directory.GetFiles(Path.GetDirectoryName(path)!).Length == 1, "Failed save left a temporary file.");
    });

    private static void PumpFor(TimeSpan duration)
    {
        var until = DateTime.UtcNow + duration;
        PumpUntil(() => DateTime.UtcNow >= until);
    }

    private static void PumpUntil(Func<bool> complete)
    {
        var frame = new DispatcherFrame();
        var deadline = DateTime.UtcNow.AddSeconds(5);
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(5) };
        timer.Tick += (_, _) => { if (complete() || DateTime.UtcNow >= deadline) frame.Continue = false; };
        timer.Start();
        try { Dispatcher.PushFrame(frame); }
        finally { timer.Stop(); }
        Assert(complete(), "Dispatcher test timed out.");
    }

    private sealed class ProbeModule(int id, List<int> order, bool fail = false) : ModuleBase
    {
        public void Start() => Load();
        protected override void InitializeCore() { if (fail) throw new InvalidOperationException(); }
        protected override void DisposeCore(bool disposing) => order.Add(id);
    }

    private sealed class RetryProbe { public RetryProbe() { } }
}
