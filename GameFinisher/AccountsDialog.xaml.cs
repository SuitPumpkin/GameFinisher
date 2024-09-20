using HandyControl.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static GameFinisher.MainWindow.HowLongToBeat;
using static GameFinisher.MainWindow;
using Newtonsoft.Json;
using IGDB.Models;

namespace GameFinisher
{
    public partial class AccountsDialog : UserControl
    {
        public AccountsDialog()
        {
            InitializeComponent();
            PrepararVentana();
        }
        private void PrepararVentana()
        {
            //cargar la info guardada del usuario
            GogUsernameTextBox.Text = Properties.Settings.Default.GOGUsername;
            SteamIdTextBox.Text = Properties.Settings.Default.SteamID;
        }
        private void CerrarDialogo(object sender, MouseButtonEventArgs e)
        {
            HandyControl.Controls.Dialog.Close("Root");
        }
        private async void GuardarSteam(object sender, RoutedEventArgs e)
        {
            // Guardar configuraciones
            Properties.Settings.Default.SteamID = SteamIdTextBox.Text;
            Properties.Settings.Default.Save();
            // Mostrar mensaje de éxito
            GrowlInfo info = new()
            {
                Message = "Steam ID succesfully saved.",
                ShowDateTime = false,
                Token = "GrowlContainer",
            };
            HandyControl.Controls.Growl.Success(info);
        }
        private async void GuardarGOG(object sender, RoutedEventArgs e)
        {
            // Guardar configuraciones
            Properties.Settings.Default.GOGUsername = GogUsernameTextBox.Text;
            Properties.Settings.Default.Save();
            // Mostrar mensaje de éxito
            GrowlInfo info = new()
            {
                Message = "GOG User succesfully saved.",
                ShowDateTime = false,
                Token = "GrowlContainer",
            };
            HandyControl.Controls.Growl.Success(info);
        }
        private async void ImportarJuegos(object sender, RoutedEventArgs e)
        {
            var resultado = MessageBox.Show("Seguro? \nEste proceso puede tomar un rato la primera vez si tienes muchos juegos", "Continuar?", MessageBoxButton.YesNo);
            if (resultado == MessageBoxResult.No) { return;}
            await Importar();
        }
        private async Task Importar()
        {
            if (System.Windows.Window.GetWindow(this) is MainWindow Padre)
            {
                // PREPARAR UI
                Importador.Content = "Importando Juegos";
                Importador.IsEnabled = false;
                BotónCierre.IsEnabled = false;
                Properties.Settings.Default.LastGameImport = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                Properties.Settings.Default.Save();
                LastUpdate.Text = $"Última vez: {Properties.Settings.Default.LastGameImport}";


                // OBTENER JUEGOS
                int juegosagregados = 0;
                var importarSteamTask = string.IsNullOrEmpty(Properties.Settings.Default.SteamID) ? Task.FromResult(0) : Padre.ImportarSteam(Properties.Settings.Default.SteamID);
                var importarGOGTask = string.IsNullOrEmpty(Properties.Settings.Default.GOGUsername) ? Task.FromResult(0) : Padre.ImportarGOG(Properties.Settings.Default.GOGUsername);
                await Task.WhenAll(importarSteamTask, importarGOGTask);
                juegosagregados += importarSteamTask.Result;
                juegosagregados += importarGOGTask.Result;
                int totalJuegos = Padre.JuegosEnPropiedad.Count;


                // PREPARAR UI
                ProgressBarJuegos.Visibility = Visibility.Visible;
                LabelJuegoActual.Visibility = Visibility.Visible;
                ProgressBarJuegos.Value = 0;
                ProgressBarJuegos.Maximum = totalJuegos;

                // OBTENER INFO DE HOWLONGTOBEAT Y DE IGDB
                List<JuegoRankeado> JuegosAAñadir = new();
                bool databaseactualizada = false;
                for (int i = 0; i < totalJuegos; i++)
                {
                    JuegoRankeado juego = Padre.JuegosEnPropiedad[i];
                    var JuegoEnDB = Padre.BaseDeDatos.FirstOrDefault(j => j.Titulo == juego.Titulo);
                    if ((juego.Tiempos == null) || (juego.Tiempos.HistoriaPrincipal == "0" && juego.Tiempos.HistoriaPrincipalYSecundarias == "0" && juego.Tiempos.Platinar == "0"))
                    {
                        LabelJuegoActual.Text = $"({i}/{totalJuegos}) Leyendo tiempos de: {juego.Titulo}";
                        if (JuegoEnDB != null)
                        {
                            if ((JuegoEnDB.Tiempos != null) && (JuegoEnDB.Tiempos.HistoriaPrincipal != "0" || JuegoEnDB.Tiempos.HistoriaPrincipalYSecundarias != "0" || JuegoEnDB.Tiempos.Platinar != "0"))
                            {
                                juego.Tiempos = JuegoEnDB.Tiempos;
                            }
                            else
                            {
                                GameFinisher.MainWindow.HowLongToBeat.Game juegito = await Search(juego.Titulo);
                                if (juegito != null)
                                {
                                    juego.Tiempos = new MainWindow.JuegoRankeado.TiemposPara
                                    {
                                        HistoriaPrincipal = juegito.MainStory,
                                        HistoriaPrincipalYSecundarias = juegito.MainPlusExtra,
                                        Platinar = juegito.Completionist,
                                    };
                                }
                                JuegoEnDB.Tiempos = juego.Tiempos;
                                databaseactualizada = true;
                            }
                        }
                        else
                        {
                            GameFinisher.MainWindow.HowLongToBeat.Game juegito = await Search(juego.Titulo);
                            if (juegito != null)
                            {
                                juego.Tiempos = new MainWindow.JuegoRankeado.TiemposPara
                                {
                                    HistoriaPrincipal = juegito.MainStory,
                                    HistoriaPrincipalYSecundarias = juegito.MainPlusExtra,
                                    Platinar = juegito.Completionist,
                                };
                            }
                            Padre.BaseDeDatos.Add(juego);
                            databaseactualizada = true;
                        }
                    }

                    if ((juego.Año == null)&&(JuegoEnDB.Año != null))
                    {
                        juego.Año = JuegoEnDB.Año;
                        juego.Calificación = JuegoEnDB.Calificación;
                        juego.Sinopsis = JuegoEnDB.Sinopsis;
                    }
                    else
                    {
                        try
                        {
                            LabelJuegoActual.Text = $"({i}/{totalJuegos}) Leyendo info extra de: {juego.Titulo}";
                            var gme = await IGDBVOID(juego.Titulo);
                            if (gme != null)
                            {
                                juego.Año = gme.Juego.FirstReleaseDate.ToString() ?? "";
                                juego.Calificación = gme.Juego.TotalRating.ToString() ?? "";
                                if (gme.Juego.Summary != null)
                                {
                                    juego.Sinopsis = gme.Juego.Summary.ToString() ?? "";
                                }
                            }
                        }
                        catch
                        {
                            //
                        }
                    }

                    ProgressBarJuegos.Value = i + 1;
                }

                //GUARDAR AMBOS JSON
                ActualizarArchivoJson(JsonConvert.SerializeObject(Padre.JuegosEnPropiedad), "OwnedGames");
                if (databaseactualizada)
                {
                    ActualizarArchivoJson(JsonConvert.SerializeObject(Padre.BaseDeDatos), "Database");
                }

                // PREPARAR UI
                HandyControl.Controls.MessageBox.Show($"{juegosagregados} juegos importados ({Padre.JuegosEnPropiedad.Count} totales)");
                ProgressBarJuegos.Visibility = Visibility.Collapsed;
                LabelJuegoActual.Visibility = Visibility.Collapsed;
                Importador.Content = "Importar Juegos";
                Importador.IsEnabled = true;
                BotónCierre.IsEnabled = true;
            }
        }
    }
}
