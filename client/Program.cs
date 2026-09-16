using System.Drawing.Drawing2D;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;

namespace FlintfixClient;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new LoginForm());
    }
}

internal sealed class LoginForm : Form
{
    private static readonly Color AccentColor = Color.FromArgb(156, 89, 182);
    private static readonly Color SurfaceColor = Color.FromArgb(24, 20, 30);
    private static readonly Color FieldColor = Color.FromArgb(36, 30, 43);
    private static readonly Color MutedColor = Color.FromArgb(177, 166, 185);
    private static readonly Color AccentHoverColor = Color.FromArgb(177, 111, 201);
    private static readonly string DownloadUrl = Environment.GetEnvironmentVariable("FLINTFIX_API_URL")
        ?? "http://127.0.0.1:8000/download/client";
    private readonly TextBox usernameBox = new();
    private readonly TextBox passwordBox = new();
    private readonly Button downloadButton = new();
    private readonly Label statusLabel = new();

    public LoginForm()
    {
        Text = "FlintFix Client";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(14, 12, 18);
        ClientSize = new Size(940, 570);
        Font = new Font("Segoe UI", 9.5f);

        var canvas = new GridPanel { Dock = DockStyle.Fill };
        var artworkPanel = new Panel { Location = new Point(30, 30), Size = new Size(360, 510) };
        artworkPanel.Paint += PaintArtwork;
        var artworkPicture = new CoverPictureBox
        {
            Image = LoadArtwork(),
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(37, 21, 48)
        };
        artworkPanel.Controls.Add(artworkPicture);

        var brand = new Label
        {
            Text = "FLINTFIX",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 24, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(24, 24),
            BackColor = Color.Transparent
        };
        var brandCaption = new Label
        {
            Text = "CLIENT PORTAL",
            ForeColor = Color.FromArgb(235, 196, 247),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(28, 65),
            BackColor = Color.Transparent
        };
        artworkPanel.Controls.Add(brandCaption);
        artworkPanel.Controls.Add(brand);

        var loginPanel = new Panel
        {
            Location = new Point(430, 30),
            Size = new Size(480, 510),
            BackColor = SurfaceColor
        };
        loginPanel.Paint += PaintLoginPanel;

        var eyebrow = new Label
        {
            Text = "SECURE MEMBER ACCESS",
            ForeColor = AccentColor,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(46, 48)
        };
        var heading = new Label
        {
            Text = "Welcome back",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 26, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(43, 73)
        };
        var description = new Label
        {
            Text = "Sign in with the details sent to you on Discord.",
            ForeColor = MutedColor,
            AutoSize = true,
            Location = new Point(47, 118)
        };

        AddField(loginPanel, "USERNAME", usernameBox, 164);
        AddField(loginPanel, "PASSWORD", passwordBox, 244);
        passwordBox.UseSystemPasswordChar = true;

        downloadButton.Text = "SIGN IN AND DOWNLOAD";
        downloadButton.FlatStyle = FlatStyle.Flat;
        downloadButton.FlatAppearance.BorderSize = 0;
        downloadButton.BackColor = AccentColor;
        downloadButton.ForeColor = Color.White;
        downloadButton.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        downloadButton.Cursor = Cursors.Hand;
        downloadButton.Size = new Size(235, 43);
        downloadButton.Location = new Point(46, 334);
        downloadButton.Click += DownloadButton_Click;
        downloadButton.MouseEnter += (_, _) => downloadButton.BackColor = AccentHoverColor;
        downloadButton.MouseLeave += (_, _) => downloadButton.BackColor = AccentColor;

        var discordButton = new Button
        {
            Text = "Need an account?  Join Discord",
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = MutedColor,
            Font = new Font("Segoe UI", 9, FontStyle.Underline),
            Cursor = Cursors.Hand,
            Size = new Size(190, 32),
            Location = new Point(282, 340)
        };
        discordButton.FlatAppearance.BorderSize = 0;
        discordButton.Click += (_, _) => OpenDiscord();

        statusLabel.AutoSize = false;
        statusLabel.ForeColor = MutedColor;
        statusLabel.Location = new Point(47, 399);
        statusLabel.Size = new Size(380, 50);

        var footer = new Label
        {
            Text = "FlintFix Client  •  Version 1.0.0",
            ForeColor = Color.FromArgb(119, 108, 127),
            AutoSize = true,
            Location = new Point(47, 466)
        };

        loginPanel.Controls.AddRange(new Control[] { eyebrow, heading, description, downloadButton, discordButton, statusLabel, footer });
        canvas.Controls.Add(artworkPanel);
        canvas.Controls.Add(loginPanel);
        Controls.Add(canvas);
        AcceptButton = downloadButton;
    }

    private static Image? LoadArtwork()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FlintfixClient.profile.png");
        return stream is null ? null : new Bitmap(stream);
    }

    private static void AddField(Control parent, string labelText, TextBox textBox, int top)
    {
        var label = new Label
        {
            Text = labelText,
            ForeColor = Color.FromArgb(203, 190, 208),
            AutoSize = true,
            Location = new Point(47, top)
        };
        textBox.Location = new Point(46, top + 21);
        textBox.Size = new Size(380, 31);
        textBox.BackColor = FieldColor;
        textBox.ForeColor = Color.White;
        textBox.BorderStyle = BorderStyle.FixedSingle;
        parent.Controls.Add(label);
        parent.Controls.Add(textBox);
    }

    private static void PaintArtwork(object? sender, PaintEventArgs eventArgs)
    {
        using var outline = new Pen(AccentColor, 2);
        eventArgs.Graphics.DrawRectangle(outline, 1, 1, ((Control)sender!).Width - 3, ((Control)sender!).Height - 3);
    }

    private static void PaintLoginPanel(object? sender, PaintEventArgs eventArgs)
    {
        using var outline = new Pen(Color.FromArgb(73, 48, 82), 1);
        eventArgs.Graphics.DrawRectangle(outline, 0, 0, ((Control)sender!).Width - 1, ((Control)sender!).Height - 1);
    }

    private static void OpenDiscord()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://discord.gg/flintfix",
                UseShellExecute = true
            });
        }
        catch (System.ComponentModel.Win32Exception)
        {
        }
    }

    private async void DownloadButton_Click(object? sender, EventArgs e)
    {
        var username = usernameBox.Text.Trim();
        var password = passwordBox.Text;
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            SetStatus("Enter both your username and password.", Color.FromArgb(237, 128, 128));
            return;
        }

        downloadButton.Enabled = false;
        SetStatus("Connecting to FlintFix...", MutedColor);
        try
        {
            using var httpClient = new HttpClient();
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            using var response = await httpClient.GetAsync(DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                SetStatus("Login failed. Check your Discord credentials.", Color.FromArgb(237, 128, 128));
                return;
            }
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                SetStatus("The server has no client package yet.", Color.FromArgb(237, 128, 128));
                return;
            }

            response.EnsureSuccessStatusCode();
            var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                ?? response.Content.Headers.ContentDisposition?.FileName
                ?? "FlintFixClient.exe";
            fileName = Path.GetFileName(fileName.Trim('"'));
            var outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            await using (var source = await response.Content.ReadAsStreamAsync())
            await using (var destination = File.Create(outputPath))
            {
                await source.CopyToAsync(destination);
            }
            var fileToOpen = outputPath;
            if (Path.GetExtension(outputPath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                var extractDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads",
                    $"FlintFixClient-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid():N}");
                ZipFile.ExtractToDirectory(outputPath, extractDirectory);
                var executables = Directory.GetFiles(extractDirectory, "*.exe", SearchOption.AllDirectories);
                if (executables.Length != 1)
                {
                    SetStatus("Download complete, but the ZIP must contain exactly one EXE.", Color.FromArgb(237, 180, 110));
                    OpenInExplorer(outputPath);
                    return;
                }
                fileToOpen = executables[0];
            }

            SetStatus("Download complete. Confirm to open the client.", Color.FromArgb(132, 216, 158));
            var openResult = MessageBox.Show(
                $"The client is ready:\n\n{fileToOpen}\n\nOpen it now? Only open files you trust.",
                "FlintFix Client",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);
            if (openResult == DialogResult.Yes)
            {
                OpenDownloadedFile(fileToOpen);
            }
        }
        catch (HttpRequestException)
        {
            SetStatus("Could not connect. Start the API first.", Color.FromArgb(237, 128, 128));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            SetStatus("Could not save the downloaded file.", Color.FromArgb(237, 128, 128));
        }
        catch (InvalidDataException)
        {
            SetStatus("The downloaded ZIP is invalid or could not be extracted.", Color.FromArgb(237, 128, 128));
        }
        finally
        {
            downloadButton.Enabled = true;
        }
    }

    private void SetStatus(string message, Color color)
    {
        statusLabel.Text = message;
        statusLabel.ForeColor = color;
    }

    private static void OpenDownloadedFile(string filePath)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = filePath,
            UseShellExecute = true
        });
    }

    private static void OpenInExplorer(string filePath)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"/select,\"{filePath}\"",
            UseShellExecute = true
        });
    }
}

internal sealed class CoverPictureBox : PictureBox
{
    public CoverPictureBox()
    {
        DoubleBuffered = true;
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        if (Image is null)
        {
            base.OnPaint(eventArgs);
            return;
        }

        var scale = Math.Max((double)Width / Image.Width, (double)Height / Image.Height);
        var drawWidth = (int)(Image.Width * scale);
        var drawHeight = (int)(Image.Height * scale);
        var drawX = (Width - drawWidth) / 2;
        var drawY = (Height - drawHeight) / 2;
        eventArgs.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        eventArgs.Graphics.DrawImage(Image, new Rectangle(drawX, drawY, drawWidth, drawHeight));
    }
}

internal sealed class GridPanel : Panel
{
    public GridPanel()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
    }

    protected override void OnPaintBackground(PaintEventArgs eventArgs)
    {
        using var gradient = new LinearGradientBrush(ClientRectangle, Color.FromArgb(29, 18, 39), Color.FromArgb(11, 10, 15), 25f);
        eventArgs.Graphics.FillRectangle(gradient, ClientRectangle);
        using var gridPen = new Pen(Color.FromArgb(24, 156, 89, 182), 1);
        for (var x = 0; x < Width; x += 32)
        {
            eventArgs.Graphics.DrawLine(gridPen, x, 0, x, Height);
        }
        for (var y = 0; y < Height; y += 32)
        {
            eventArgs.Graphics.DrawLine(gridPen, 0, y, Width, y);
        }
    }
}