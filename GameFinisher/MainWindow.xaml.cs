using HandyControl.Controls;
using HandyControl.Tools.Extension;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HtmlAgilityPack;
using System;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Web;
using System.Text.RegularExpressions;
using static GameFinisher.MainWindow.HowLongToBeat;
using System.Security.Cryptography;
using GameFinisher.Properties;
using System.Net.Http.Json;

namespace GameFinisher
{
    public class ApiService()
    {
        private static readonly HttpClient httpClient = new();
        public static async Task<string?> Get(string url)
        {
            try
            {
                // Realiza una solicitud GET a la API
                HttpResponseMessage response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode(); // Lanza una excepción si la respuesta no es exitosa
                // Lee el contenido de la respuesta como una cadena
                string responseData = await response.Content.ReadAsStringAsync();
                return responseData;
            }
            catch (HttpRequestException e)
            {
                // Maneja errores de solicitud HTTP
                System.Diagnostics.Debug.WriteLine($"Error al obtener datos de la API: {e.Message}");
                return null;
            }
        }
    }
    public partial class MainWindow : System.Windows.Window
    {
        // Elementos multifuncionales

        public List<JuegoRankeado> BaseDeDatos { get; set; } = new();
        public List<JuegoRankeado> JuegosEnPropiedad { get; set; } = new();
        public class HowLongToBeat
        {
            private static Dictionary<string, Game> cache = new Dictionary<string, Game>();
            private static int LevenshteinDistance(string s, string t)
            {
                if (string.IsNullOrEmpty(s)) return t.Length;
                if (string.IsNullOrEmpty(t)) return s.Length;

                var d = new int[s.Length + 1, t.Length + 1];

                for (int i = 0; i <= s.Length; i++)
                    d[i, 0] = i;

                for (int j = 0; j <= t.Length; j++)
                    d[0, j] = j;

                for (int i = 1; i <= s.Length; i++)
                {
                    for (int j = 1; j <= t.Length; j++)
                    {
                        int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                        d[i, j] = Math.Min(
                            Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                            d[i - 1, j - 1] + cost);
                    }
                }

                return d[s.Length, t.Length];
            }
            public class Game
            {
                public int Game_Id { get; set; }
                public required string Game_Name { get; set; }
                public required string Game_Alias { get; set; }
                public required string Game_Type { get; set; }
                public required string Game_Image { get; set; }
                public int Comp_Main { get; set; } // Main Story Time in Seconds
                public int Comp_Plus { get; set; } // Main + Extra Time in Seconds
                public int Comp_100 { get; set; }  // Completionist Time in Seconds
                public int Release_Year { get; set; }

                // Métodos para convertir el tiempo a horas/minutos
                public string GetFormattedTime(int seconds)
                {
                    int hours = seconds / 3600;
                    int minutes = (seconds % 3600) / 60;
                    return $"{hours}h {minutes}m";
                }

                // Para formatear las horas correctamente
                public string MainStory => GetFormattedTime(Comp_Main);
                public string MainPlusExtra => GetFormattedTime(Comp_Plus);
                public string Completionist => GetFormattedTime(Comp_100);
            }
            public class SearchResponse
            {
                public int Count { get; set; }
                public int PageCurrent { get; set; }
                public int PageSize { get; set; }
                public int PageTotal { get; set; }
                public string Title { get; set; }
                public List<Game> Data { get; set; }
            }
            public static async Task<Game> Search(string busqueda)
            {
                if (cache.ContainsKey(busqueda)) return cache[busqueda];
                HttpClient httpClient = new();
                busqueda = Regex.Replace(busqueda, @"(:?\s*DEMO|\s*Demo)", "", RegexOptions.IgnoreCase).Trim();
                busqueda = busqueda.Replace(":", "");
                busqueda = busqueda.Replace("&", "");
                busqueda = busqueda.Replace("®", "");
                busqueda = busqueda.Replace("™", "");
                busqueda = busqueda.Replace("Free Trial", "");
                busqueda = busqueda.Replace(" Ch.", "chapter ");
                busqueda = busqueda.Replace(" -", " ");
                // Ajustar la búsqueda como lo hace el sitio
                var jsonPayload = new
                {
                    searchType = "games",
                    searchTerms = busqueda.Split(" "),
                    searchPage = 1,
                    size = 20,
                    searchOptions = new
                    {
                        games = new
                        {
                            userId = 0,
                            platform = "",
                            sortCategory = "popular",
                            rangeCategory = "main",
                            rangeTime = new { min = 0, max = 0 },
                            gameplay = new { perspective = "", flow = "", genre = "" },
                            modifiedSince = ""
                        },
                        users = false,
                        filter = "",
                        sort = 0,
                        randomizer = 0
                    }
                };
                var content = new StringContent(JsonConvert.SerializeObject(jsonPayload), Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(HttpMethod.Post, "https://howlongtobeat.com/api/search/21fda17e4a1d49be");
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Mobile Safari/537.36 Edg/128.0.0.0");
                request.Headers.Add("Origin", "https://howlongtobeat.com");
                request.Headers.Add("Referer", "https://howlongtobeat.com/");
                request.Headers.Add("Accept", "*/*");
                request.Headers.Add("Accept-Language", "es-419,es;q=0.9,es-ES;q=0.8,en;q=0.7,en-GB;q=0.6,en-US;q=0.5,es-MX;q=0.4");
                request.Headers.Add("Cookie", "_li_dcdm_c=.howlongtobeat.com; _lc2_fpi=7f22cff1ceab--01j79ng9y25yk0x4h98xkydjkf; _lc2_fpi_meta=%7B%22w%22%3A1725827655619%7D; _ga=GA1.1.1315493206.1725827656; _ga_LNSNNH2NMQ=GS1.1.1726683046.22.1.1726684316.0.0.0");
                request.Content = content;
                System.Diagnostics.Debug.WriteLine("Procesando:\n"+busqueda);
                var response = await httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                string jsonResponse = await response.Content.ReadAsStringAsync();
                // Deserializar la respuesta
                var result = JsonConvert.DeserializeObject<SearchResponse>(jsonResponse);
                if (result == null || result.Data.Count == 0) return null;
                // Ahora evaluamos el nombre del juego y sus alias
                Game mejorJuego = null;
                int menorDistancia = int.MaxValue;
                foreach (var juego in result.Data)
                {
                    // Evaluar la distancia de Levenshtein entre el término de búsqueda y el nombre del juego
                    int distanciaNombre = LevenshteinDistance(busqueda, juego.Game_Name);
                    // Separar los alias y evaluar cada uno
                    string[] alias = juego.Game_Alias?.Split(',') ?? Array.Empty<string>();
                    foreach (var a in alias)
                    {
                        int distanciaAlias = LevenshteinDistance(busqueda, a.Trim()); // Trim para eliminar espacios

                        if (distanciaAlias < menorDistancia)
                        {
                            menorDistancia = distanciaAlias;
                            mejorJuego = juego;
                        }
                    }
                    // Si el nombre es una mejor coincidencia, actualizar
                    if (distanciaNombre < menorDistancia)
                    {
                        menorDistancia = distanciaNombre;
                        mejorJuego = juego;
                    }
                }
                if (mejorJuego != null) cache[busqueda] = mejorJuego;
                return mejorJuego;
            }
        }
        public class JuegoRankeado
        {
            public class TiemposPara
            {
                public string HistoriaPrincipal { get; set; }
                public string HistoriaPrincipalYSecundarias { get; set; }
                public string Platinar { get; set; }
            }
            public required string Titulo { get; set; }
            public string? Portada { get; set; }
            public string? Cabecera { get; set; }
            public string? IdSteam { get; set; }
            public string? IdGOG { get; set; }
            public TiemposPara? Tiempos { get; set; }
        }

        //Funcionalidad basica de la ventana

        public MainWindow()
        {
            InitializeComponent();
            PrepararVentana();
        } //Construir Ventana
        private async void PrepararVentana()
        {
            //Cargar la base de datos inicial
            BaseDeDatos = await CargarJuegos("Database");
            //Cargar los juegos que el usuario posee
            JuegosEnPropiedad = await CargarJuegos("OwnedGames");
        } //Preparativos Ventana
        private void MoverVentana(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                DragMove();
            }
        } //Función: Mover Ventana
        private void CerrarVentana(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        } //Función: Cerrar Ventana

        //Importacion de bibliotecas de juegos

        public async Task<int> ImportarSteam(string Idsteam)
        {
            try
            {
                // Obtener datos de la API de Steam
                string respuesta = await ApiService.Get($"https://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/?key=F64EEA6A04936153AC17B9A9D18389D7&steamid={Idsteam}&format=json&include_appinfo=true&include_played_free_games=true");
                if (respuesta == null)
                {
                    // Si no hay respuesta, termina la ejecución
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        HandyControl.Controls.MessageBox.Show("No se pudo obtener la lista de juegos de Steam.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                    return 0;
                }
                // Parsear JSON
                var data = JsonConvert.DeserializeObject<JObject>(respuesta);
                var games = data["response"]?["games"]?.ToObject<List<JObject>>();
                if (games == null)
                {
                    // Si no se encuentra la lista de juegos, termina la ejecución
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        HandyControl.Controls.MessageBox.Show("No se encontraron juegos en la respuesta.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                    return 0;
                }
                // Extraer información relevante
                List<JuegoRankeado> listaJuegos = games.Select(game => new JuegoRankeado
                {
                    IdSteam = game["appid"]?.ToString(),
                    Titulo = game["name"]?.ToString(),
                    Portada = $"https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/{game["appid"]?.ToString()}/hero_capsule.jpg",
                    Cabecera = $"https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/{game["appid"]?.ToString()}/header.jpg",
                }).ToList();
                // Comparar y actualizar los juegos en JuegosListados
                int agregados = 0;
                Parallel.ForEach(listaJuegos, (juegito) =>
                {
                    var juegoExistente = JuegosEnPropiedad.FirstOrDefault(j => j.Titulo == juegito.Titulo);
                    if (juegoExistente == null)
                    {
                        lock (JuegosEnPropiedad)
                        {
                            JuegosEnPropiedad.Add(juegito);
                        }
                        Interlocked.Increment(ref agregados);
                    }
                });
                return agregados;
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    HandyControl.Controls.MessageBox.Show($"Error al importar juegos de Steam: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
                return 0;
            }
        } //Importar desde Steam
        public async Task<int> ImportarGOG(string GOGUser)
        {
            try
            {
                string respuesta = await ApiService.Get($"https://www.gog.com/u/{GOGUser}/games/stats?sort=alphabetically&order=asc&page=1");
                if (respuesta == null)
                {
                    // Si no hay respuesta para la primera página, termina la ejecución
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        HandyControl.Controls.MessageBox.Show("No se pudo obtener la lista de juegos de GOG", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                    return 0;
                }
                // Parsear la primera página para obtener la cantidad total de páginas y juegos
                var data = JsonConvert.DeserializeObject<JObject>(respuesta);
                int totalPaginas = data.Value<int>("pages");
                // Crear una lista para almacenar los juegos filtrados
                List<JuegoRankeado> juegos = new List<JuegoRankeado>();
                // Procesar la primera página
                var itemsPrimeraPagina = data["_embedded"]?["items"];
                if (itemsPrimeraPagina != null)
                {
                    foreach (var item in itemsPrimeraPagina)
                    {
                        var game = item["game"];
                        if (game != null)
                        {
                            var juegoFiltrado = new JuegoRankeado
                            {
                                IdGOG = game["id"]?.ToString(),
                                Titulo = game["title"]?.ToString(),
                                Cabecera = game["image"]?.ToString()
                            };
                            juegos.Add(juegoFiltrado);
                        }
                    }
                }
                // Hacer solicitudes para las páginas restantes
                for (int iterador = 2; iterador <= totalPaginas; iterador++)
                {
                    respuesta = await ApiService.Get($"https://www.gog.com/u/{GOGUser}/games/stats?sort=alphabetically&order=asc&page={iterador}");
                    if (respuesta != null)
                    {
                        data = JsonConvert.DeserializeObject<JObject>(respuesta);
                        var items = data["_embedded"]?["items"];
                        if (items != null)
                        {
                            foreach (var item in items)
                            {
                                var game = item["game"];
                                if (game != null)
                                {
                                    var juegoFiltrado = new JuegoRankeado
                                    {
                                        IdGOG = game["id"]?.ToString(),
                                        Titulo = game["title"]?.ToString(),
                                        Cabecera = game["image"]?.ToString()
                                    };
                                    juegos.Add(juegoFiltrado);
                                }
                            }
                        }
                    }
                }
                // Comparar y actualizar los juegos en JuegosListados
                int agregados = 0;
                foreach (var juegito in juegos)
                {
                    var juegoExistente = JuegosEnPropiedad.FirstOrDefault(j => j.Titulo == juegito.Titulo);
                    if (juegoExistente != null)
                    {
                        // Si el juego ya existe, actualizar el IdGOG
                        if (juegoExistente.IdGOG != juegito.IdGOG)
                        {
                            juegoExistente.IdGOG = juegito.IdGOG;
                        }
                    }
                    else
                    {
                        // Si no existe, añadir el juego filtrado
                        JuegosEnPropiedad.Add(juegito);
                        agregados++;
                    }
                }
                return agregados;
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    HandyControl.Controls.MessageBox.Show($"Error al importar juegos de GOG: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
                return 0;
            }
        } //Importar desde GOG

        //Interactibilidad de la UI

        private void BusquedaHecha(object sender, HandyControl.Data.FunctionEventArgs<string> e)
        {
            if (sender is SearchBar BarraDeBusqueda)
            {
                BarraDeBusqueda.Clear();
                OutputText.Text = $"GOGUser: {Properties.Settings.Default.GOGUsername}\nSteamID: {Properties.Settings.Default.SteamID}";
            }
        } //Se buscó algo en la barra de busqueda
        private void AbrirCuentas(object sender, MouseButtonEventArgs e)
        {
            var settingsDialog = new AccountsDialog();

            // Mostrar el diálogo directamente con el UserControl como contenido
            HandyControl.Controls.Dialog.Show(content: settingsDialog,"Root");
        } //Se uso el botón de Cuenta
        private async void AbrirAjustes(object sender, MouseButtonEventArgs e)
        {
            //Abrir ajustes

        } //se uso el botón de Ajustes

        // Funciones DUMMY

        public async void ActualizarArchivoJson(string listadoJuegos, string nombre)
        {
            try
            {
                // Formatear el texto para que sea JSON legible
                var juegosFormateados = JsonConvert.DeserializeObject(listadoJuegos);
                string jsonFormateado = JsonConvert.SerializeObject(juegosFormateados, Formatting.Indented);

                // Definir el nombre y la ruta del archivo
                string archivoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{nombre}.json");

                // Guardar el archivo JSON en el escritorio
                await System.IO.File.WriteAllTextAsync(archivoPath, jsonFormateado);

                // Mostrar mensaje de éxito
                HandyControl.Controls.MessageBox.Show($"El archivo {nombre}.json ha sido guardado.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                // Manejar cualquier error
                HandyControl.Controls.MessageBox.Show($"Error al guardar el archivo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public async Task<List<JuegoRankeado>> CargarJuegos(string nombre)
        {
            string jsonFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{nombre}.json");
            if (System.IO.File.Exists(jsonFilePath))
            {
                string jsonData = await System.IO.File.ReadAllTextAsync(jsonFilePath);
                return JsonConvert.DeserializeObject<List<JuegoRankeado>>(jsonData) ?? new List<JuegoRankeado>();
            }
            return new List<JuegoRankeado>();
        }
    }
}