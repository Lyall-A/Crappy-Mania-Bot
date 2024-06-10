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
            { "keycount", "4" },
            { "keybinds", "d,f,j,k" },
            { "width", "1920" },
            { "height", "1080" },
            { "window-offset-x", "0" },
            { "window-offset-y", "0" },
            { "key-y-bottom", "115" }, // OR key-y-top.
            //{ "color", "#C8C8C8" }, // OR colors
            { "colors", "#C8C8C8,#95A7FC,#95A7FC,#C8C8C8" }, // OR color
            { "key-width", "130" },
            { "key-height", "130" },
            { "centered", "yes" }, // OR start-x. start-x = (width / 2) - (x-offset * (key-count / 2)) + (x-offset / 2)
            { "log-fps", "yes" },
            { "log-keys", "yes" }, // OR log-key-presses OR/AND log-key-releases
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

                            if (bottomCurrentColor.ToArgb() == parsedColors[keyIndex].ToArgb() && !pressedKeys[keyIndex])
                            {
                                pressedKeys[keyIndex] = true;
                                PressKeys(keybinds[keyIndex]);
                                if (logKeys || logKeyPresses) Console.WriteLine($"Key {keyIndex + 1} ({keybinds[keyIndex]}): Pressed");
                                return;
                            }

                            if (topCurrentColor.ToArgb() != parsedColors[keyIndex].ToArgb() && pressedKeys[keyIndex])
                            {
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