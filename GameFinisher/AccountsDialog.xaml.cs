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
        private async void ImportarJuegos(object sender, RoutedEventArgs e)
        {
            var resultado = HandyControl.Controls.MessageBox.Show("Seguro? \nEste proceso puede tomar un rato la primera vez si tienes muchos juegos", "Continuar?", MessageBoxButton.YesNo);
            if (resultado == MessageBoxResult.No) { return;}
            if (System.Windows.Window.GetWindow(this) is MainWindow Padre)
            {
                // Deshabilitar el botón mientras se ejecuta la tarea
                if (sender is Button boton) { boton.Content = "Importando Juegos"; boton.IsEnabled = false; }
                BotónCierre.IsEnabled = false;

                // Actualizar el campo LastUpdate con la fecha y hora actual
                Properties.Settings.Default.LastGameImport = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                Properties.Settings.Default.Save();
                LastUpdate.Text = $"Última vez: {Properties.Settings.Default.LastGameImport}";

                int juegosagregados = 0;

                // Cargar Steam si existe
                string steamid = Properties.Settings.Default.SteamID;
                if (!string.IsNullOrEmpty(steamid)) { juegosagregados += await Padre.ImportarSteam(steamid); }

                // Cargar GOG si existe
                string goguser = Properties.Settings.Default.GOGUsername;
                if (!string.IsNullOrEmpty(goguser)) { juegosagregados += await Padre.ImportarGOG(goguser); }

                // Cargar juegos previos guardados (si existen)
                var juegosGuardados = Padre.JuegosEnPropiedad;

                // Mostrar ProgressBar y Label
                ProgressBarJuegos.Visibility = Visibility.Visible;
                LabelJuegoActual.Visibility = Visibility.Visible;

                // Inicializar barra de progreso
                ProgressBarJuegos.Value = 0;
                int totalJuegos = Padre.JuegosEnPropiedad.Count;
                ProgressBarJuegos.Maximum = totalJuegos;

                // Calcular las horas para cada juego y actualizar barra de progreso
                for (int i = 0; i < totalJuegos; i++)
                {
                    MainWindow.JuegoRankeado juego = Padre.JuegosEnPropiedad[i];
                    // Verificar si el juego ya ha sido procesado previamente
                    var juegoExistente = juegosGuardados.FirstOrDefault(j => j.Titulo == juego.Titulo);
                    if (juegoExistente != null)
                    {
                        // Si el juego ya existe, copiar los tiempos calculados
                        juego.Tiempos = juegoExistente.Tiempos;
                        continue;
                    }
                    // Actualizar el label con el juego actual
                    LabelJuegoActual.Text = $"({i}/{totalJuegos}) Leyendo tiempos de: {juego.Titulo}";
                    // Solicitar información a HowLongToBeat
                    Game juegito = await MainWindow.HowLongToBeat.Search(juego.Titulo);
                    if (juegito != null)
                    {
                        juego.Tiempos = new MainWindow.JuegoRankeado.TiemposPara
                        {
                            HistoriaPrincipal = juegito.MainStory,
                            HistoriaPrincipalYSecundarias = juegito.MainPlusExtra,
                            Platinar = juegito.Completionist,
                        };
                    }
                    // Actualizar barra de progreso
                    ProgressBarJuegos.Value = i + 1;
                }
                //GUARDAR EN OWNEDGAMES.JSON
                Padre.ActualizarArchivoJson(JsonConvert.SerializeObject(Padre.JuegosEnPropiedad), "OwnedGames");
                bool databaseactualizada = false;
                foreach (JuegoRankeado juegowo in Padre.JuegosEnPropiedad)
                {
                    var juegoBasado = Padre.BaseDeDatos.FirstOrDefault(j => j.Titulo == juegowo.Titulo);
                    if (juegoBasado != null)
                    {
                        // Si el juego ya existe, actualizar la info
                        if (juegoBasado.IdSteam != juegowo.IdSteam)
                        {
                            juegoBasado.IdSteam = juegowo.IdSteam;
                            databaseactualizada = true;
                        }
                        if (juegoBasado.IdGOG != juegowo.IdGOG)
                        {
                            juegoBasado.IdGOG = juegowo.IdGOG;
                            databaseactualizada = true;
                        }
                        if (juegoBasado.Tiempos != juegowo.Tiempos)
                        {
                            juegoBasado.Tiempos = juegowo.Tiempos;
                            databaseactualizada = true;
                        }
                    }
                    else
                    {
                        // Si no existe, añadir el juego filtrado
                        Padre.BaseDeDatos.Add(juegowo);
                        databaseactualizada = true;
                    }
                }
                if (databaseactualizada)
                {
                    Padre.ActualizarArchivoJson(JsonConvert.SerializeObject(Padre.BaseDeDatos), "Database");
                }

                // Mostrar mensaje al finalizar
                HandyControl.Controls.MessageBox.Show($"{juegosagregados} juegos importados ({Padre.JuegosEnPropiedad.Count} totales)");

                // Ocultar ProgressBar y Label al finalizar
                ProgressBarJuegos.Visibility = Visibility.Collapsed;
                LabelJuegoActual.Visibility = Visibility.Collapsed;

                // Rehabilitar el botón
                if (sender is Button botonFinal) { botonFinal.Content = "Importar Juegos"; botonFinal.IsEnabled = true; }
                BotónCierre.IsEnabled = true;
            }
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
    }
}
