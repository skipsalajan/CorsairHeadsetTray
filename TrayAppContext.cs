using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Forms;

namespace CorsairHeadsetTray;

internal sealed class TrayAppContext : ApplicationContext
{
    private const string HeadsetControlPath =
        @"C:\Program Files\headsetcontrol 4.1.0\bin\headsetcontrol.exe";

    private readonly NotifyIcon trayIcon;
    private readonly System.Windows.Forms.Timer refreshTimer;
    private Icon? currentIcon;
    private bool isRefreshing;

    public TrayAppContext()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Refresh now", null, async (_, _) => await RefreshBatteryAsync());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitApplication());

        currentIcon = CreateIcon(null, Color.FromArgb(0, 170, 155));

        trayIcon = new NotifyIcon
        {
            Icon = currentIcon,
            Text = "Corsair headset: checking battery...",
            ContextMenuStrip = menu,
            Visible = true
        };

        refreshTimer = new System.Windows.Forms.Timer
        {
            Interval = 30_000
        };
        refreshTimer.Tick += async (_, _) => await RefreshBatteryAsync();
        refreshTimer.Start();

        _ = RefreshBatteryAsync();
    }

    private async Task RefreshBatteryAsync()
    {
        if (isRefreshing)
            return;

        isRefreshing = true;

        try
        {
            if (!File.Exists(HeadsetControlPath))
            {
                SetUnavailable("HeadsetControl not found");
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = HeadsetControlPath,
                Arguments = "-b -o json",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);

            if (process is null)
            {
                SetUnavailable("Could not start HeadsetControl");
                return;
            }

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                SetUnavailable(string.IsNullOrWhiteSpace(error)
                    ? "HeadsetControl query failed"
                    : error.Trim());
                return;
            }

            using var document = JsonDocument.Parse(output);
            JsonElement root = document.RootElement;

            if (!root.TryGetProperty("devices", out JsonElement devices) ||
                devices.GetArrayLength() == 0)
            {
                SetUnavailable("No headset detected");
                return;
            }

            JsonElement device = devices[0];

            if (!device.TryGetProperty("battery", out JsonElement battery) ||
                !battery.TryGetProperty("level", out JsonElement levelElement) ||
                !levelElement.TryGetInt32(out int level))
            {
                SetUnavailable("Battery reading unavailable");
                return;
            }

            string status = battery.TryGetProperty("status", out JsonElement statusElement)
                ? statusElement.GetString() ?? "Unknown"
                : "Unknown";

            bool isCharging = status.Contains(
                "CHARG",
                StringComparison.OrdinalIgnoreCase);

            level = Math.Clamp(level, 0, 100);

            Color tileColor = isCharging
                ? Color.FromArgb(45, 135, 220)
                : level > 30
                    ? Color.FromArgb(0, 170, 155)
                    : level >= 15
                        ? Color.FromArgb(232, 180, 35)
                        : Color.FromArgb(220, 65, 65);

            SetIcon(level, tileColor);

            trayIcon.Text = isCharging
                ? $"Corsair headset: {level}% (charging)"
                : $"Corsair headset: {level}%";
        }
        catch (Exception ex)
        {
            SetUnavailable($"Battery check failed: {ex.Message}");
        }
        finally
        {
            isRefreshing = false;
        }
    }

    private void SetUnavailable(string message)
    {
        SetIcon(null, Color.FromArgb(90, 90, 90));
        trayIcon.Text = $"Corsair headset: {message}";
    }

    private void SetIcon(int? level, Color tileColor)
    {
        Icon newIcon = CreateIcon(level, tileColor);
        Icon? oldIcon = currentIcon;

        currentIcon = newIcon;
        trayIcon.Icon = newIcon;

        oldIcon?.Dispose();
    }

    private static Icon CreateIcon(int? level, Color tileColor)
    {
        using var bitmap = new Bitmap(32, 32);

        using (Graphics graphics = Graphics.FromImage(bitmap))
        using (var tileBrush = new SolidBrush(tileColor))
        using (var whiteBrush = new SolidBrush(Color.White))
        using (var outlinePen = new Pen(Color.Black, 1.0f))
        using (var fontFamily = new FontFamily("Segoe UI"))
        using (var centered = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        })
        using (GraphicsPath tilePath = CreateRoundedRectangle(
            new Rectangle(1, 2, 30, 28),
            2))
        {
            graphics.Clear(Color.Transparent);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.FillPath(tileBrush, tilePath);

            string text = level.HasValue
                ? level.Value.ToString()
                : "--";

            using var textPath = new GraphicsPath();
            textPath.AddString(
                text,
                fontFamily,
                (int)FontStyle.Bold,
                21,
                new PointF(0, 0),
                centered);

            RectangleF textBounds = textPath.GetBounds();
            const float horizontalPadding = 2.5f;
            const float verticalPadding = 3.0f;
            float availableWidth = 30 - horizontalPadding * 2;
            float availableHeight = 28 - verticalPadding * 2;

            float scale = Math.Min(
                availableWidth / textBounds.Width,
                availableHeight / textBounds.Height);

            using (var transform = new Matrix())
            {
                transform.Translate(-textBounds.X, -textBounds.Y);
                transform.Scale(scale, scale);
                transform.Translate(
                    16 - textBounds.Width * scale / 2,
                    16 - textBounds.Height * scale / 2);
                textPath.Transform(transform);
            }

            graphics.DrawPath(outlinePen, textPath);
            graphics.FillPath(whiteBrush, textPath);
        }

        IntPtr iconHandle = bitmap.GetHicon();

        try
        {
            using Icon temporaryIcon = Icon.FromHandle(iconHandle);
            return (Icon)temporaryIcon.Clone();
        }
        finally
        {
            DestroyIcon(iconHandle);
        }
    }

    private static GraphicsPath CreateRoundedRectangle(
        Rectangle bounds,
        int radius)
    {
        int diameter = radius * 2;
        var path = new GraphicsPath();

        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(
            bounds.Right - diameter,
            bounds.Y,
            diameter,
            diameter,
            270,
            90);
        path.AddArc(
            bounds.Right - diameter,
            bounds.Bottom - diameter,
            diameter,
            diameter,
            0,
            90);
        path.AddArc(
            bounds.X,
            bounds.Bottom - diameter,
            diameter,
            diameter,
            90,
            90);
        path.CloseFigure();

        return path;
    }

    private void ExitApplication()
    {
        refreshTimer.Stop();
        trayIcon.Visible = false;
        trayIcon.Dispose();
        refreshTimer.Dispose();
        currentIcon?.Dispose();
        currentIcon = null;
        ExitThread();
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
