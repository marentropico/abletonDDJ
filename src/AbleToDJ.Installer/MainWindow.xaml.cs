using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Diagnostics;

namespace AbleToDJ.Installer;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Header_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            this.DragMove();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private async void Install_Click(object sender, RoutedEventArgs e)
    {
        WelcomePanel.Visibility = Visibility.Collapsed;
        InstallPanel.Visibility = Visibility.Visible;
        InstallButton.IsEnabled = false;
        CancelButton.IsEnabled = false;

        Log("Iniciando instalação do AbleToDJ...");
        UpdateProgress(5, "Verificando caminhos de sistema...");

        string localAppFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AbleToDJ");
        string targetExe = Path.Combine(localAppFolder, "AbleToDJ.exe");

        try
        {
            await Task.Run(async () =>
            {
                // Caminhos
                string abletonRemoteFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Ableton", "User Library", "Remote Scripts", "DDJ_LiveBridge");
                string desktopFolder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string startMenuFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");

                // 1. Limpar pastas anteriores
                Log("Limpando instalações anteriores se existirem...");
                if (Directory.Exists(localAppFolder))
                {
                    try { Directory.Delete(localAppFolder, true); } catch { }
                }
                if (Directory.Exists(abletonRemoteFolder))
                {
                    try { Directory.Delete(abletonRemoteFolder, true); } catch { }
                }

                Directory.CreateDirectory(localAppFolder);
                Directory.CreateDirectory(abletonRemoteFolder);
                await Task.Delay(500);

                // 2. Extrair Aplicação
                UpdateProgress(30, "Extraindo arquivos do aplicativo AbleToDJ...");
                Log("Carregando recursos de arquivo...");
                var assembly = Assembly.GetExecutingAssembly();
                
                using (Stream appStream = assembly.GetManifestResourceStream("AbleToDJ.Installer.Resources.app.zip")!)
                {
                    if (appStream == null) throw new Exception("Recurso app.zip não encontrado!");
                    string tempAppZip = Path.Combine(Path.GetTempPath(), "abletodj_app.zip");
                    using (FileStream fs = new FileStream(tempAppZip, FileMode.Create, FileAccess.Write))
                    {
                        await appStream.CopyToAsync(fs);
                    }
                    Log("Extraindo aplicativo para " + localAppFolder);
                    ZipFile.ExtractToDirectory(tempAppZip, localAppFolder, overwriteFiles: true);
                    try { File.Delete(tempAppZip); } catch { }
                }
                await Task.Delay(500);

                // 3. Extrair Remote Script
                UpdateProgress(60, "Configurando Remote Scripts do Ableton Live...");
                using (Stream scriptStream = assembly.GetManifestResourceStream("AbleToDJ.Installer.Resources.ableton_script.zip")!)
                {
                    if (scriptStream == null) throw new Exception("Recurso ableton_script.zip não encontrado!");
                    string tempScriptZip = Path.Combine(Path.GetTempPath(), "abletodj_script.zip");
                    using (FileStream fs = new FileStream(tempScriptZip, FileMode.Create, FileAccess.Write))
                    {
                        await scriptStream.CopyToAsync(fs);
                    }
                    Log("Extraindo scripts remotos do Ableton para " + abletonRemoteFolder);
                    ZipFile.ExtractToDirectory(tempScriptZip, abletonRemoteFolder, overwriteFiles: true);
                    try { File.Delete(tempScriptZip); } catch { }
                }
                await Task.Delay(500);

                // 4. Criar Atalhos
                UpdateProgress(85, "Criando atalhos no Desktop e Menu Iniciar...");
                
                // Desktop
                string desktopShortcut = Path.Combine(desktopFolder, "AbleToDJ.lnk");
                CreateShortcut(desktopShortcut, targetExe, "Ponte de Integração Pioneer DDJ-400 para Ableton Live");

                // Start Menu
                string startMenuShortcut = Path.Combine(startMenuFolder, "AbleToDJ.lnk");
                CreateShortcut(startMenuShortcut, targetExe, "Ponte de Integração Pioneer DDJ-400 para Ableton Live");

                await Task.Delay(500);
                UpdateProgress(100, "Instalação concluída com sucesso!");
                Log("AbleToDJ v1.0.0 foi instalado com êxito!");
            });

            StatusTitle.Text = "Instalação concluída!";
            LaunchCheckBox.Visibility = Visibility.Visible;
            InstallButton.Content = "Concluir";
            InstallButton.IsEnabled = true;
            InstallButton.Click -= Install_Click;
            InstallButton.Click += (s, ev) =>
            {
                if (LaunchCheckBox.IsChecked == true)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = targetExe,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        Log("Erro ao iniciar o AbleToDJ: " + ex.Message);
                    }
                }
                Application.Current.Shutdown();
            };
        }
        catch (Exception ex)
        {
            Log("ERRO: " + ex.Message);
            StatusTitle.Text = "A instalação falhou.";
            StatusTitle.Foreground = System.Windows.Media.Brushes.Red;
            CancelButton.IsEnabled = true;
        }
    }

    private void Log(string message)
    {
        Dispatcher.Invoke(() =>
        {
            LogText.Text += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
            LogText.Focus();
        });
    }

    private void UpdateProgress(double val, string status)
    {
        Dispatcher.Invoke(() =>
        {
            InstallProgress.Value = val;
            StatusTitle.Text = status;
        });
    }

    private void CreateShortcut(string shortcutPath, string targetPath, string description)
    {
        try
        {
            string psCommand = $"$s = (New-Object -ComObject WScript.Shell).CreateShortcut('{shortcutPath}'); $s.TargetPath = '{targetPath}'; $s.Description = '{description}'; $s.IconLocation = '{targetPath},0'; $s.Save()";
            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"-NoProfile -Command \"{psCommand}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                process.Start();
                process.WaitForExit();
            }
            Log($"Atalho criado: {Path.GetFileName(shortcutPath)}");
        }
        catch (Exception ex)
        {
            Log($"Erro ao criar atalho {shortcutPath}: {ex.Message}");
        }
    }
}
