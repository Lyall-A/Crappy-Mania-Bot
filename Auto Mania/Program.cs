#pragma warning disable // BE QUIET

using System.Runtime.InteropServices;
using System.Drawing;
using System.Diagnostics.Metrics;
using System.Drawing.Imaging;
using System.Diagnostics;

class AutoMania
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    private static void Main(string[] rawArgs)
    {
        Dictionary<string, string> args = new Dictionary<string, string>
        {
            // Default args
            { "keycount", "4" }, // How many keys
            { "keybinds", "d,f,j,k" }, // Array of keybinds split by ','
            { "width", "1920" }, // Screen width
            { "height", "1080" }, // Screen height
            { "window-offset-x", "0" }, // Window offset X
            { "window-offset-y", "0" }, // Window offset Y
            { "key-y-bottom", "115" }, // OR key-y-top. Y position of the keys from bottom, recommended to be the bottom of keys
            // { "key-y-top", "965" }, // OR key-y-bottom. Y position of the keys from top, recommended to be the bottom of keys
            //{ "color", "#C8C8C8" }, // OR colors. Color of all keys
            { "colors", "#C8C8C8,#95A7FC,#95A7FC,#C8C8C8" }, // OR color. Array of keys split by ','
            { "key-width", "130" }, // Width of keys
            { "key-height", "130" }, // Height of keys, can be set to 0
            { "centered", "yes" }, // OR start-x. If keys are in center of window horizontally
            { "start-x", "105" }, // OR centered. X position of center of first key
            { "log-fps", "yes" }, // Log FPS
            { "log-keys", "yes" }, // OR log-key-presses AND log-key-releases. Log keys
            // { "log-key-presses", "yes" }, // OR log-keys. Log key presses
            // { "log-key-releases", "yes" }, // OR log-keys. Log key releases
            // { "key-press-delay", "15" }, // Add delay before pressing key
            // { "key-release-delay", "1" }, // Add delay before releasing key
            // { "delay", "1" }, // Add delay before checking keys
        };

        // Get args
        for (int i = 0; i < rawArgs.Length; i++)
        {
            if (rawArgs[i].StartsWith("--") && rawArgs.Length > i + 1 && !rawArgs[i + 1].StartsWith("--"))
            {
                args[rawArgs[i].Substring(2).ToLower()] = rawArgs[i + 1];
            }
        }

        // Validate arg types (TODO)

        // Define arg variables
        int keyCount = 0;
        string[] keybinds = new string[0];
        int width = 0;
        int height = 0;
        int windowOffsetX = 0;
        int windowOffsetY = 0;
        int keyYBottom = 0;
        int keyYTop = 0;
        int keyPressDelay = 0;
        int keyReleaseDelay = 0;
        int delay = 0;
        string color = "";
        string[] colors = new string[0];
        int keyWidth = 0;
        int keyHeight = 0;
        bool centered = false;
        int startX = 0;
        bool logFps = false;
        bool logKeys = false;
        bool logKeyPresses = false;
        bool logKeyReleases = false;

        // Set arg variables
        if (args.ContainsKey("keycount")) keyCount = ArgInt(args["keycount"]);
        if (args.ContainsKey("keybinds")) keybinds = args["keybinds"].Split(',');
        if (args.ContainsKey("width")) width = ArgInt(args["width"]);
        if (args.ContainsKey("height")) height = ArgInt(args["height"]);
        if (args.ContainsKey("window-offset-x")) windowOffsetX = ArgInt(args["window-offset-x"]);
        if (args.ContainsKey("window-offset-y")) windowOffsetY = ArgInt(args["window-offset-y"]);
        if (args.ContainsKey("key-y-bottom")) keyYBottom = ArgInt(args["key-y-bottom"]);
        if (args.ContainsKey("key-y-top")) keyYTop = ArgInt(args["key-y-top"]);
        if (args.ContainsKey("key-press-delay")) keyPressDelay = ArgInt(args["key-press-delay"]);
        if (args.ContainsKey("key-release-delay")) keyReleaseDelay = ArgInt(args["key-release-delay"]);
        if (args.ContainsKey("delay")) delay = ArgInt(args["delay"]);
        if (args.ContainsKey("color")) color = args["color"];
        if (args.ContainsKey("colors")) colors = args["colors"].Split(",");
        if (args.ContainsKey("key-width")) keyWidth = ArgInt(args["key-width"]);
        if (args.ContainsKey("key-height")) keyHeight = ArgInt(args["key-height"]);
        if (args.ContainsKey("centered")) centered = ArgBool(args["centered"]);
        if (args.ContainsKey("start-x")) startX = ArgInt(args["start-x"]);
        if (args.ContainsKey("log-fps")) logFps = ArgBool(args["log-fps"]);
        if (args.ContainsKey("log-keys")) logKeys = ArgBool(args["log-keys"]);
        if (args.ContainsKey("log-key-presses")) logKeyPresses = ArgBool(args["log-key-presses"]);
        if (args.ContainsKey("log-key-releases")) logKeyReleases = ArgBool(args["log-key-releases"]);

        // Define extra variables
        Color[] parsedColors = new Color[keyCount];
        bool[] pressedKeys = new bool[keyCount];

        // Start
        if (windowOffsetX != 0) keyWidth += windowOffsetX;
        if (centered && startX == 0) startX = (width / 2) - (keyWidth * (keyCount - 1)) / 2;
        if (keyYBottom != 0 && keyYTop == 0) keyYTop = (height - keyYBottom);
        if (windowOffsetY != 0) keyYTop += windowOffsetY;
        for (int i = 0; i < keyCount; i++)
        {
            parsedColors[i] = ColorTranslator.FromHtml(colors.Length > 0 ? colors[i] : color);
        }

        int screenshotWidth = keyWidth * (keyCount - 1) + 1;
        int screenshotHeight = keyHeight + 1;

        int frameCount = 0;
        Stopwatch stopwatch = Stopwatch.StartNew();

        while (true)
        {
            using (Bitmap screenshot = new Bitmap(screenshotWidth, screenshotHeight))
            {
                using (Graphics graphic = Graphics.FromImage(screenshot))
                {
                    graphic.CopyFromScreen(startX, keyYTop - screenshotHeight, 0, 0, new Size(screenshotWidth, screenshotHeight), CopyPixelOperation.SourceCopy);
                    BitmapData bitmapData = screenshot.LockBits(new Rectangle(0, 0, screenshot.Width, screenshot.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                    Parallel.For(0, keyCount, keyIndex =>
                    {
                        unsafe
                        {
                            byte* p = (byte*)bitmapData.Scan0;
                            int stride = bitmapData.Stride;

                            int x = keyWidth * keyIndex;
                            int topY = 0;
                            int topIndex = topY * stride + x * 4;
                            int bottomY = keyHeight;
                            int bottomIndex = bottomY * stride + x * 4;

                            Color topCurrentColor = Color.FromArgb(p[topIndex + 3], p[topIndex + 2], p[topIndex + 1], p[topIndex]);
                            Color bottomCurrentColor = Color.FromArgb(p[bottomIndex + 3], p[bottomIndex + 2], p[bottomIndex + 1], p[bottomIndex]);

                            if (delay > 0) Thread.Sleep(delay);

                            if (bottomCurrentColor.ToArgb() == parsedColors[keyIndex].ToArgb() && !pressedKeys[keyIndex])
                            {
                                if (keyPressDelay > 0) Thread.Sleep(keyPressDelay);
                                pressedKeys[keyIndex] = true;
                                PressKeys(keybinds[keyIndex]);
                                if (logKeys || logKeyPresses) Console.WriteLine($"Key {keyIndex + 1} ({keybinds[keyIndex]}): Pressed");
                                return;
                            }

                            if (topCurrentColor.ToArgb() != parsedColors[keyIndex].ToArgb() && pressedKeys[keyIndex])
                            {
                                if (keyReleaseDelay > 0) Thread.Sleep(keyReleaseDelay);
                                pressedKeys[keyIndex] = false;
                                ReleaseKeys(keybinds[keyIndex]);
                                if (logKeys || logKeyReleases) Console.WriteLine($"Key {keyIndex + 1} ({keybinds[keyIndex]}): Released");
                                return;
                            }
                        }
                    });
                    screenshot.UnlockBits(bitmapData);
                }
            }

            if (logFps)
            {
                frameCount++;
                if (stopwatch.ElapsedMilliseconds >= 1000)
                {
                    Console.WriteLine($"FPS: {frameCount}");
                    frameCount = 0;
                    stopwatch.Restart();
                }
            }
        }
    }

    private static void PressKeys(string keys)
    {
        foreach (char key in keys)
        {
            byte keyCode = (byte)char.ToUpper(key);
            keybd_event(keyCode, 0, 0x0000, UIntPtr.Zero);
        }
    }

    private static void ReleaseKeys(string keys)
    {
        foreach (char key in keys)
        {
            byte keyCode = (byte)char.ToUpper(key);
            keybd_event(keyCode, 0, 0x0002, UIntPtr.Zero);
        }
    }

    private static bool ArgBool(string arg)
    {
        return (arg == "1" || arg == "true" || arg == "yes") ? true : false;
    }

    private static int ArgInt(string arg)
    {
        return int.Parse(arg);
    }
}