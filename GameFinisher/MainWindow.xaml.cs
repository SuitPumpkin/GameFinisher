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
using System.Security.Cryptography;
using GameFinisher.Properties;
using System.Net.Http.Json;
using System.Net;
using IGDB;
using IGDB.Models;
using GameFinisher;
using System.Diagnostics;
using System.Linq;
using System.Collections.ObjectModel;
using IGDB.Serialization;
using System.Windows.Forms;
using HandyControl.Data;
using System.Data.SQLite;

namespace GameFinisher
{
    public class Utilities()
    {
        /// <summary><para> Guarda los juegos en formato <typeparamref name="JSON"/> en el archivo mencionado</para></summary>
        /// <param name="listadoJuegos">Contenido a guardar</param>
        /// <param name="nombre">Nombre del archivo</param>
        public static async void ActualizarArchivoJson(string listadoJuegos, string nombre)
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
        /// <summary><para> Carga los juegos desde el JSON mencionado </para></summary>
        /// <returns>Una lista de <typeparamref name="JuegoRankeado"/></returns>
        public static async Task<List<Rankeo.JuegoRankeado>> CargarJuegos(string nombre)
        {
            string jsonFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{nombre}.json");
            if (System.IO.File.Exists(jsonFilePath))
            {
                string jsonData = await System.IO.File.ReadAllTextAsync(jsonFilePath);
                return JsonConvert.DeserializeObject<List<Rankeo.JuegoRankeado>>(jsonData) ?? new List<Rankeo.JuegoRankeado>();
            }
            return new List<Rankeo.JuegoRankeado>();
        }
        public static int LevenshteinDistance(string s, string t)
        {
            if (string.IsNullOrEmpty(s)) { return t.Length; }
            if (string.IsNullOrEmpty(t)) { return s.Length; }
            var d = new int[s.Length + 1, t.Length + 1];
            for (int i = 0; i <= s.Length; i++) { d[i, 0] = i; }
            for (int j = 0; j <= t.Length; j++) { d[0, j] = j; }
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
    }
    public class APIs()
    {
        public class ApiConsummer()
        {
            public static readonly HttpClient httpClient = new HttpClient();
            public static async Task<string?> Get(string url)
            {
                try
                {
                    HttpResponseMessage response = await httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync();
                }
                catch (HttpRequestException e)
                {
                    Debug.WriteLine($"Error al obtener datos de la API: {e.Message}");
                    return null;
                }
            }
        }
        //api wrapper de howlongtobeat
        public class HowLongToBeat
        {
            private static Dictionary<string, List<Game>> Cache = new();
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
                public double MainStory => (Comp_Main / 3600);
                public double MainPlusExtra => (Comp_Plus / 3600);
                public double Completionist => (Comp_100 / 3600);
            }
            public class SearchResponse
            {
                public int Count { get; set; }
                public List<Game> Data { get; set; }
            }
            public static async Task<List<Game>> Search(string busqueda)
            {
                if (Cache.ContainsKey(busqueda)) return Cache[busqueda];
                HttpClient httpClient = new();
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
                var request = new HttpRequestMessage(HttpMethod.Post, "https://howlongtobeat.com/api/search/ae7a009728ec3f3f");
                request.Headers.Add("User-Agent", "PostmanRuntime/7.42.0");
                request.Headers.Add("Origin", "https://howlongtobeat.com");
                request.Headers.Add("Referer", "https://howlongtobeat.com/");
                request.Headers.Add("Accept", "*/*");
                request.Headers.Add("Accept-Language", "es-419,es;q=0.9,es-ES;q=0.8,en;q=0.7,en-GB;q=0.6,en-US;q=0.5,es-MX;q=0.4");
                request.Headers.Add("Cookie", "_li_dcdm_c=.howlongtobeat.com; _lc2_fpi=7f22cff1ceab--01j79ng9y25yk0x4h98xkydjkf; _lc2_fpi_meta=%7B%22w%22%3A1725827655619%7D; _ga=GA1.1.1315493206.1725827656; _ga_LNSNNH2NMQ=GS1.1.1726683046.22.1.1726684316.0.0.0");
                request.Content = content;
                var response = await httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<SearchResponse>(jsonResponse);
                if (result == null || result.Data.Count == 0) return new List<Game>();
                Cache[busqueda] = result.Data;
                return result.Data;
            }
        }
        //api wrapper de IGDB
        public class IGDBmini
        {
            private static Dictionary<string, List<Game>> Cache = new();
            private static Dictionary<long, string> GenerosCache = new();
            private static IGDBClient igdbClient = new IGDBClient("zj3dihoaqe9ougkayckq6h7c31umqz", "uqmapybmo5di3lh0q1wua4s4dsyzkn");
            public class Game
            {
                public long Id { get; set; }
                public string Name { get; set; }
                public string year { get; set; }
                public string Summary { get; set; }
                public double? Rating { get; set; }
                public double? AggregatedRating { get; set; }
                public List<string>? Generos { get; set; }
                public string? Franquisia { get; set; }
                public string? Portada { get; set; }
            }
            // Función principal para buscar juegos en IGDB
            public static async Task<List<Game>> Search(string searchedTitle)
            {
                // Revisa si ya está en la caché
                if (Cache.ContainsKey(searchedTitle)) return Cache[searchedTitle];

                var games = await igdbClient.QueryAsync<IGDB.Models.Game>(IGDBClient.Endpoints.Games,
                    query: $"fields cover.url, franchise.name, *; search \"{searchedTitle}\"; limit 50;");

                var tasks = games.Select(async juego =>
                {
                    // Ejecuta las consultas de géneros, franquicia y portada en paralelo
                    var genresTask = GetGenres(juego);
                    if (juego.Franchise == null) { juego.Franchise = new IGDB.IdentityOrValue<Franchise>( value: new Franchise() { Name="" }); }
                    if (juego.Cover == null) { juego.Cover = new IGDB.IdentityOrValue<Cover>( value: new Cover() { Url="" }); }
                    if (juego.FirstReleaseDate == null) { juego.FirstReleaseDate = new DateTimeOffset() { }; }
                    await Task.WhenAll(genresTask);

                    return new Game
                    {
                        Id = juego.Id??0,
                        Name = juego.Name,
                        Summary = juego.Summary,
                        year = $"({juego.FirstReleaseDate.Value.Year.ToString()??""})",
                        Rating = juego.Rating,
                        AggregatedRating = juego.AggregatedRating,
                        Generos = await genresTask,
                        Franquisia = juego.Franchise.Value.Name,
                        Portada = $"https:{juego.Cover.Value.Url.Replace("t_thumb", "t_cover_small")}"
                    };
                });

                List<Game> results = (await Task.WhenAll(tasks)).ToList();
                Cache[searchedTitle] = results;
                return results;
            }
            // Función para obtener géneros de un juego
            private static async Task<List<string>> GetGenres(IGDB.Models.Game game)
            {
                if (game.Genres == null || game.Genres.Ids == null || game.Genres.Ids.Length == 0)
                    return new List<string>();

                // Usamos los IDs de géneros para obtener los nombres desde la caché
                return game.Genres.Ids
                    .Where(id => GenerosCache.ContainsKey(id))
                    .Select(id => GenerosCache[id])
                    .ToList();
            }
            // Método para inicializar los géneros una vez al iniciar el programa
            public static async Task InicializarGeneros()
            {
                var generos = await igdbClient.QueryAsync<IGDB.Models.Genre>(IGDBClient.Endpoints.Genres,
                    query: "fields id, name; limit 500;"); // Ajusta el límite según la cantidad de géneros disponibles
                // Guardamos los géneros en la caché
                foreach (var genero in generos)
                {
                    if (genero.Id != null)
                    {
                        GenerosCache[genero.Id.Value] = genero.Name;
                    }
                }
            }
        }

    }
    public class Importadores
    {
        //GOG galaxy 2.0
        public class GOG
        {
            public class GOGGame
            {
                public string Title { get; set; }
                public string Platforms { get; set; }
                public string Genres { get; set; }
                public DateTime ReleaseDate { get; set; }
                public int Playtime { get; set; }
                public DateTime LastPlayed { get; set; }
            }
            public class GOGDBHelper
            {
                private string dbPath;
                public GOGDBHelper(string dbPath)
                {
                    this.dbPath = dbPath;
                }
                public List<GOGGame> ObtenerGOGDB()
                {
                    var games = new List<GOGGame>();

                    string connectionString = $"Data Source={dbPath};Version=3;";
                    using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                    {
                        connection.Open();
                        string query = @"
                                SELECT 
                                    title AS Title, 
                                    platformList AS Platforms, 
                                    genreList AS Genres, 
                                    releaseDate AS ReleaseDate, 
                                    playtime AS Playtime, 
                                    lastPlayed AS LastPlayed
                                FROM MasterDB
                                WHERE isHidden = 0;"; // Asegurando que solo muestra los juegos no ocultos

                        using (SQLiteCommand command = new SQLiteCommand(query, connection))
                        {
                            using (SQLiteDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    GOGGame game = new GOGGame()
                                    {
                                        Title = reader["Title"].ToString(),
                                        Platforms = reader["Platforms"].ToString(),
                                        Genres = reader["Genres"].ToString(),
                                        ReleaseDate = Convert.ToDateTime(reader["ReleaseDate"]),
                                        Playtime = Convert.ToInt32(reader["Playtime"]),
                                        LastPlayed = Convert.ToDateTime(reader["LastPlayed"])
                                    };
                                    games.Add(game);
                                }
                            }
                        }
                    }

                    return games;
                }
            }
        }
    }
    public class Rankeo
    {
        public async Task<List<JuegoRankeado>> Buscar(string Busqueda)
        {
            List<JuegoRankeado> Lista = new();
            var hltbTask = APIs.HowLongToBeat.Search(Busqueda);
            var igdbTask = APIs.IGDBmini.Search(Busqueda);

            // Espera ambas tareas en paralelo
            await Task.WhenAll(hltbTask, igdbTask);
            List<APIs.HowLongToBeat.Game> Hltb = await hltbTask;
            List<APIs.IGDBmini.Game> Igdb = await igdbTask;
            Parallel.ForEach(Hltb, hltbGame =>
            {
                var igdbMatch = Igdb.AsParallel().FirstOrDefault(igdbGame =>
                    Utilities.LevenshteinDistance(hltbGame.Game_Name, igdbGame.Name) <= 3);

                if (igdbMatch != null)
                {
                    var juegoRankeado = new JuegoRankeado
                    {
                        Titulo = hltbGame.Game_Name,
                        Portada = igdbMatch.Portada,
                        Año = igdbMatch.year,
                        Sinopsis = igdbMatch.Summary,
                        Franquisia = igdbMatch.Franquisia,
                        Tiempos = new JuegoRankeado.TiemposPara
                        {
                            HistoriaPrincipal = hltbGame.MainStory,
                            HistoriaSecundarias = hltbGame.MainPlusExtra,
                            Completar100 = hltbGame.Completionist
                        },
                        CalificacionUsuarios = igdbMatch.Rating,
                        CalificacionCriticos = igdbMatch.AggregatedRating,
                        Generos = igdbMatch.Generos
                    };
                    lock (Lista)
                    {
                        Lista.Add(juegoRankeado);
                    }
                }
            });
            return Lista;
        }
        public class JuegoRankeado
        {
            // Información básica
            public required string Titulo { get; set; }
            public string? Portada { get; set; }
            public string? Cabecera { get; set; }
            public string? Año { get; set; }
            public string? Sinopsis { get; set; }
            public string? Franquisia { get; set; }

            // Estilos de juego con sus tiempos
            public class TiemposPara
            {
                public double HistoriaPrincipal { get; set; } // En horas
                public double HistoriaSecundarias { get; set; } // En horas
                public double Completar100 { get; set; } // En horas
            }
            public TiemposPara? Tiempos { get; set; }

            // Calificación y géneros
            public double? CalificacionUsuarios { get; set; } // Puntuación de usuarios
            public double? CalificacionCriticos { get; set; } // Puntuación de críticos
            public List<string>? Generos { get; set; } // Lista de géneros del juego

            // Calidad por hora de juego para cada estilo
            public double CalificaciónMedia
            {
                get
                {
                    if (CalificacionCriticos.HasValue && CalificacionUsuarios.HasValue)
                        return (CalificacionCriticos.Value + CalificacionUsuarios.Value) / 2;
                    else if (CalificacionUsuarios.HasValue)
                        return CalificacionUsuarios ?? 5;
                    else if (CalificacionCriticos.HasValue)
                        return CalificacionCriticos ?? 5;
                    return 5;
                }
            }
            public double CalidadPorHoraHistoriaPrincipal => CalcularCalidadPorHora(Tiempos?.HistoriaPrincipal ?? 0, CalificaciónMedia);
            public double CalidadPorHoraHistoriaSecundarias => CalcularCalidadPorHora(Tiempos?.HistoriaSecundarias ?? 0, CalificaciónMedia);
            public double CalidadPorHoraCompletar100 => CalcularCalidadPorHora(Tiempos?.Completar100 ?? 0, CalificaciónMedia);

            // Método para calcular calidad por hora
            private static double CalcularCalidadPorHora(double horas, double calificacion) { return (horas > 0 && calificacion > 0) ? (calificacion / horas)*10 : 0; }
        }
    }
    public partial class MainWindow : System.Windows.Window
    {
        // Elementos multifuncionales
        public List<Rankeo.JuegoRankeado> BaseDeDatos { get; set; } = new();
        public List<Rankeo.JuegoRankeado> JuegosEnPropiedad { get; set; } = new();
        public ObservableCollection<Rankeo.JuegoRankeado> ResultadosBusqueda { get; set; } = new();
        //Funcionalidad basica de la ventana
        public MainWindow()
        {
            PrepararVentana();
            InitializeComponent();
            DataContext = this;
        } //Construir Ventana
        private async Task PrepararVentana()
        {
            //Cargar la base de datos inicial
            BaseDeDatos = await Utilities.CargarJuegos("Database");
            //Cargar los juegos que el usuario posee
            JuegosEnPropiedad = await Utilities.CargarJuegos("OwnedGames");
            CantidadDeJuegos.Text = JuegosEnPropiedad.Count().ToString();
            await APIs.IGDBmini.InicializarGeneros();
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
        //Interactibilidad de la UI
        private async void BusquedaHecha(object sender, HandyControl.Data.FunctionEventArgs<string> e)
        {
            if (sender is SearchBar BarraDeBusqueda)
            {
                BordeListaResultados.Visibility = Visibility.Collapsed;
                BarraDeBusqueda.IsEnabled = false;
                string Busqueda = BarraDeBusqueda.Text;
                Debug.WriteLine($"Buscando \"{Busqueda}\"...");

                // Verificar si el juego ya está en BaseDeDatos
                var resultadosEnBaseDeDatos = BaseDeDatos.Where(juego => juego.Titulo.Contains(Busqueda, StringComparison.OrdinalIgnoreCase)).ToList();

                if (resultadosEnBaseDeDatos.Any())
                {
                    // Si hay resultados en la base de datos, no hacer búsqueda y mostrar esos resultados
                    Debug.WriteLine($"Resultados encontrados en BaseDeDatos para \"{Busqueda}\"...");
                    ResultadosBusqueda.Clear();
                    foreach (var juego in resultadosEnBaseDeDatos)
                    {
                        ResultadosBusqueda.Add(juego);
                        Debug.WriteLine($"se añadió de BaseDeDatos: {juego.Titulo} {juego.Portada}");
                    }
                }
                else
                {
                    // Si no hay resultados en la base de datos, proceder con la búsqueda en línea
                    Rankeo rank = new();
                    var resultados = rank.Buscar(Busqueda);
                    await Task.WhenAll(resultados);
                    // Limpiar resultados anteriores
                    ResultadosBusqueda.Clear();
                    // Añadir los resultados nuevos
                    foreach (Rankeo.JuegoRankeado Juego in resultados.Result)
                    {
                        Debug.WriteLine($"se añadió: {Juego.Titulo} {Juego.Portada}");
                        ResultadosBusqueda.Add(Juego);
                    }
                    if (resultados.Result.Count <= 0)
                    {
                        GrowlInfo info = new()
                        {
                            Message = $"No hay resultados de \"{Busqueda}\".",
                            ShowDateTime = false,
                            Token = "GrowlContainer",
                        };
                        Growl.Warning(info);
                    }
                }
                BordeListaResultados.Visibility = Visibility.Visible;
                Debug.WriteLine($"Finalizado \"{Busqueda}\"");
                BarraDeBusqueda.IsEnabled = true;
            }
        }//Se buscó algo en la barra de busqueda
        private void AbrirExportImport(object sender, MouseButtonEventArgs e)
        {
            //ImportarGOG();
        } //Se uso el botón de importar o exportar para abrir ese menú
        private void CerrarResultados(object sender, System.Windows.Input.MouseEventArgs e)
        {
            BordeListaResultados.Visibility = Visibility.Collapsed;
        }
        public void ImportarGOG()
        {
            Importadores.GOG.GOGDBHelper GOGUwU = new("C:\\ProgramData\\GOG.com\\Galaxy\\storage\\galaxy-2.0.db");
            List<Importadores.GOG.GOGGame> juegos = GOGUwU.ObtenerGOGDB();
            //procesarlo y guardarlo
        }
    }
}