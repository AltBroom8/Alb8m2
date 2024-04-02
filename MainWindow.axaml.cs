using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using NAudio.Wave;
using Avalonia.Threading;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Timer = System.Timers.Timer;

namespace Alb8m2;
public partial class MainWindow : Window
{
    public static HashSet<Cancion> _album = new HashSet<Cancion>();
    public static Cancion _actual = null;
    private WaveOutEvent _waveOut = new WaveOutEvent();
    private bool playing = false;
    private Timer timer;
    private Point startPoint;
    private WaveChannel32 waveChannel32;
    private bool hold = false;
    private bool primera = true;
    private bool play = false;
    private bool draggingSlider = false;
    private double ultimaPosicionUsuario = 0;
    private double ultimaPosicionActualizada = 0;
    private HashSet<Cancion> buscar = new HashSet<Cancion>();
    private bool closingInProgress = false;
    
    public static Cancion getCancion()
    {
        return _actual;
    }
    public static HashSet<Cancion> getAlbum()
    {
        return _album;
    }
    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
        InitializeComponent();
        timer = new Timer(1000);

        // Agrega definiciones de columna y fila al Grid
        // Agrega definiciones de columna y fila al Grid
        mainGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        mainGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        mainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        mainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        LoadCanciones();
        MostrarElementos();
        if (primera)
        {
            primera = false;
            AvanceSlider();
            
        }
        timer = new Timer(250); // Actualizar cada 100 milisegundos (ajusta según sea necesario)
        timer.Elapsed += async (sender, e) => await AvanceSlider();
        timer.Start();
        BarraReproduccion.IsEnabled = true;

    }
    
    private async void MostrarElementos()
    {
        mainGrid.Children.Clear();
        mainGrid.ColumnDefinitions.Clear();
        mainGrid.RowDefinitions.Clear();
        int columnCount = 4; // Cambia el número de columnas según tus necesidades
        int rowCount = (int)Math.Ceiling((double)_album.Count / columnCount);  // Ajustar el número de filas según la cantidad de elementos

        // Añadir definiciones de columna y fila al Grid
        for (int i = 0; i < columnCount; i++)
        {
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        }

        for (int i = 0; i < rowCount; i++)
        {
            mainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        }
        List<Cancion> orden = _album.OrderBy(tema => tema.titulo).ToList();
        _album.Clear();
        foreach (var tema in orden)
        {
            _album.Add(tema);
        }
        int index = 0;
        string searchText = SearchBox.Text;
        HashSet<Cancion> conjuntoCanciones = (string.IsNullOrEmpty(searchText)) ? _album : buscar;
        // Añadir StackPanel con datos de las canciones al Grid
        foreach (var cancion in conjuntoCanciones)
        {
            // Crear un StackPanel
            var stackPanel = CrearStackPanelElemento(cancion);

            // Definir la posición en el Grid
            Grid.SetColumn(stackPanel, index % columnCount);
            Grid.SetRow(stackPanel, index / columnCount);

            // Añadir el StackPanel al Grid
            mainGrid.Children.Add(stackPanel);

            index++;
        }
        
    }
    private Border CrearStackPanelElemento(Cancion cancion)
    {
        // Crear un StackPanel para cada canción
        var stackPanel = new StackPanel
        {
            Width = 300,
            Height = 300,
            Background = new SolidColorBrush(0x47F595FF),
            Margin = new Thickness(5),
            Tag = cancion,
        };

        // Agregar la portada (asumiendo que cancion.Imagen es la ruta de la imagen)
        var imagen = new Image
        {
            Source = new Avalonia.Media.Imaging.Bitmap(new MemoryStream(cancion.imagenPortada)), // Utilizar Bitmap para cargar desde MemoryStream
            Width = 250, // Ajusta según sea necesario
            Height = 250, // Ajusta según sea necesario
        };

        // Agregar el nombre de la canción
        var nombreTextBlock = new TextBlock
        {
            Text = $"{cancion.titulo}",
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 5, 0, 0),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
        };

        // Agregar el autor de la canción
        var autorTextBlock = new TextBlock
        {
            Text = $"{cancion.autor}",
            Margin = new Thickness(0, 5, 0, 0),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
        };

        // Agregar elementos al StackPanel
        stackPanel.Children.Add(imagen);
        stackPanel.Children.Add(nombreTextBlock);
        stackPanel.Children.Add(autorTextBlock);
        var border = new Border
        {
            Child = stackPanel,
            BorderBrush = null,
            BorderThickness = new Thickness(0),
        };
        bool isMouseOver = false;

        stackPanel.PointerMoved += (sender, e) =>
        {
            var position = e.GetPosition(stackPanel);

            // Verificar si el puntero está dentro del StackPanel
            if (position.X >= 0 && position.Y >= 0 &&
                position.X <= stackPanel.Bounds.Width && position.Y <= stackPanel.Bounds.Height)
            {
                // Cambiar el borde o realizar otras acciones cuando el ratón está dentro
                if (!isMouseOver)
                {
                    // Only execute if it was not already over
                    isMouseOver = true;
                    border.BorderBrush = new SolidColorBrush(Colors.Black);
                    border.BorderThickness = new Thickness(3);
                
                    // Cambiar el cursor a Selector
                    stackPanel.Cursor = new Cursor(StandardCursorType.Hand);
                }
            }
            else
            {
                // Restaurar el borde o realizar otras acciones cuando el ratón está fuera
                if (isMouseOver)
                {
                    // Only execute if it was previously over
                    isMouseOver = false;
                    border.BorderBrush = null;
                    border.BorderThickness = new Thickness(0);
                
                    // Restaurar el cursor predeterminado
                    stackPanel.Cursor = new Cursor(StandardCursorType.Arrow);
                }
            }
        };

        stackPanel.PointerExited += (sender, e) =>
        {
            // Restaurar el borde o realizar otras acciones cuando el ratón sale
            if (isMouseOver)
            {
                isMouseOver = false;
                border.BorderBrush = null;
                border.BorderThickness = new Thickness(0);
            
                // Restaurar el cursor predeterminado
                stackPanel.Cursor = new Cursor(StandardCursorType.Arrow);
            }
        };
        stackPanel.PointerPressed += (sender, e) =>
        {
            // Reducir la escala al hacer clic
            imagen.RenderTransform = new ScaleTransform(0.95, 0.95);
            InfoButton.Content = new Image
            {
                Source = new Avalonia.Media.Imaging.Bitmap(new MemoryStream(cancion.imagenPortada)),
                Width = 100,
                Height = 100,
            };
            _actual = cancion;
            ReproducirCancionDesdeBytes(_actual.audio);
            string directorioEjecutable = AppDomain.CurrentDomain.BaseDirectory;
            string directorioProyecto = Path.Combine(directorioEjecutable, "..", "..","..");
            string rutaImagen = Path.Combine(directorioProyecto, "pause.png");
            PlayImage.Source = new Bitmap(rutaImagen);
            playing = true;

        };

        stackPanel.PointerReleased += (sender, e) =>
        {
            // Restaurar la escala al soltar el clic
            imagen.RenderTransform = new ScaleTransform(1.0, 1.0);
        };
        
        return border;
    }
    
    private void OnNuevoButtonClick(object sender, RoutedEventArgs e)
    {
        // Lógica para abrir la nueva interfaz aquí
        // Por ejemplo, puedes crear una nueva ventana y mostrarla
        var nuevo = new Nuevo();

        // Manejador de eventos para el evento Closed
        nuevo.Closed += (senderClosed, eClosed) =>
        {
            Console.WriteLine("Valor de nuevo.exito: " + nuevo.exito);
            if (nuevo.exito == true)
            {
                _album.Add(nuevo.nueva);
                Console.WriteLine("Se ha añadido la canción");
                MostrarElementos();
            }
        };
        // Muestra la ventana
        nuevo.Show();
    }
    
    private async Task ReproducirCancionDesdeBytes(byte[] bytesCancion)
    {
        try
        {
            Console.WriteLine($"Length of bytesCancion: {bytesCancion.Length}");

            // Crear un MemoryStream desde el array de bytes
            using (var memoryStream = new MemoryStream(bytesCancion))
            {
                // Crear un objeto Mp3FileReader desde el MemoryStream
                var reader = new Mp3FileReader(memoryStream);

                // Envolver el lector en WaveChannel32 para permitir la búsqueda
                waveChannel32 = new WaveChannel32(reader);

                // Inicializar la reproducción sin iniciarla automáticamente
                _waveOut.Volume = 1.0f;
                if (_waveOut.PlaybackState == PlaybackState.Playing)
                {
                    _waveOut.Stop();
                }
                _waveOut.Dispose(); // Liberar recursos
                _waveOut = new WaveOutEvent();

                _waveOut.Init(waveChannel32);

                // Reiniciar la posición de reproducción al inicio del archivo
                waveChannel32.Position = 0;
                BarraReproduccion.Maximum = reader.TotalTime.TotalSeconds;

                // Suscribirse al evento PlaybackStopped
                var playbackStoppedTask = new TaskCompletionSource<object>();
                _waveOut.PlaybackStopped += (sender, args) =>
                {
                    // La reproducción ha terminado
                    Console.WriteLine("Playback stopped");

                    // Completa la tarea solo si no ha sido completada anteriormente
                    if (!playbackStoppedTask.Task.IsCompleted)
                    {
                        playbackStoppedTask.SetResult(null);
                    }
                };
                // Iniciar la reproducción en segundo plano
                _waveOut.Play();
                play = true;
                
                // Esperar hasta que la reproducción haya terminado sin bloquear la interfaz de usuario
                await playbackStoppedTask.Task.ConfigureAwait(false);

                Console.WriteLine("Playback finished");

                bool playbackStarted = _waveOut.PlaybackState == PlaybackState.Playing;

                if (playbackStarted)
                {
                    Console.WriteLine("Playback started");
                }
                else
                {
                    Console.WriteLine("Playback failed to start");
                }
                
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error during playback: {e.Message}");
        }
    }

  private async Task AvanceSlider()
{
    // Verificar si la canción actual no es nula y el slider no está siendo arrastrado
    if (_actual != null && _waveOut != null && !draggingSlider)
    {
        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                // Obtener la posición actual del slider
                var newPosition = BarraReproduccion.Value;

                // Calcular la diferencia de tiempo desde la última actualización
                double diferenciaTiempo = Math.Abs(newPosition - ultimaPosicionActualizada);

                // Actualizar la posición de la canción solo si la diferencia es mayor a 1.0 segundos
                if (diferenciaTiempo > 1.0)
                {
                    Console.WriteLine("entramos alla");
                    // Detener la reproducción y ajustar la posición de la canción
                    _waveOut.Pause();

                    // Verificar si la posición fue cambiada manualmente por el usuario
                    if (Math.Abs(newPosition - ultimaPosicionUsuario) > 0.01)
                    {
                        // El usuario cambió manualmente la posición del slider, ajustar la posición de la canción
                        waveChannel32.Position = (long)(newPosition * _waveOut.OutputWaveFormat.AverageBytesPerSecond);
                        ultimaPosicionUsuario = newPosition;
                    }
                    else
                    {
                        // La posición del slider se actualizó automáticamente, ajustar la posición de la canción en consecuencia
                        waveChannel32.Position = (long)(ultimaPosicionActualizada * _waveOut.OutputWaveFormat.AverageBytesPerSecond);
                        BarraReproduccion.Value = ultimaPosicionActualizada;
                    }

                    _waveOut.Play(); // Continuar la reproducción desde la nueva posición

                    // Actualizar la variable ultimaPosicionActualizada
                    ultimaPosicionActualizada = newPosition;
                    Tiempo.Content = ObtenerFormatoTiempo(newPosition);
                    string directorioEjecutable = AppDomain.CurrentDomain.BaseDirectory;
                    string directorioProyecto = Path.Combine(directorioEjecutable, "..", "..","..");
                    string rutaImagen = Path.Combine(directorioProyecto, "pause.png");
                    play = true;
                    PlayImage.Source = new Bitmap(rutaImagen);
                    
                }
                else
                {
                    Console.WriteLine("entramos");
                    // La diferencia es menor o igual a 1.0 segundos, actualizar el slider y la posición de la canción
                    BarraReproduccion.Value = waveChannel32.Position / (double)_waveOut.OutputWaveFormat.AverageBytesPerSecond;
                    ultimaPosicionActualizada = BarraReproduccion.Value;
                    Tiempo.Content = ObtenerFormatoTiempo(newPosition);
                    
                }
            });
        }
        catch (Exception e)
        {
            Console.WriteLine($"An error occurred: {e.Message}");
        }
    }
}

    private async void InfoButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_actual == null)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "No hay ninguna cancion seleccionada", ButtonEnum.Ok);
            await box.ShowAsync();
        }
        else
        {
            string data = _actual.ToString();
            var box = MessageBoxManager.GetMessageBoxStandard("Info", data, ButtonEnum.Ok);
            await box.ShowAsync();
        }
    }

    private async void PpButton_OnClick(object? sender, RoutedEventArgs e)
    {
        string directorioEjecutable = AppDomain.CurrentDomain.BaseDirectory;
        string directorioProyecto = Path.Combine(directorioEjecutable, "..", "..","..");
        if (_actual == null)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "No hay ninguna cancion seleccionada", ButtonEnum.Ok);
            await box.ShowAsync();
        }
        else
        {
            if (play)
            {
                play = false;
                _waveOut.Pause();
                string rutaImagen = Path.Combine(directorioProyecto, "play.png");
                PlayImage.Source = new Bitmap(rutaImagen);
            }
            else
            {
                play = true;
                _waveOut.Play();
                string rutaImagen = Path.Combine(directorioProyecto, "pause.png");
                PlayImage.Source = new Bitmap(rutaImagen);
            }
        }
    }
    private string ObtenerFormatoTiempo(double segundos)
    {
        // Convierte los segundos a minutos y segundos
        int minutos = (int)segundos / 60;
        int segundosRestantes = (int)segundos % 60;

        // Formatea el tiempo con ceros a la izquierda si es necesario
        return $"{minutos}:{segundosRestantes:D2}";
    }
    private async void borrar(object? sender, RoutedEventArgs e)
    {
        
        if (_actual == null)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "No hay ninguna cancion seleccionada", ButtonEnum.Ok);
            await box.ShowAsync();
        }
        else
        {
            
            if (_album.Contains(_actual))
            {
                // Pausar la reproducción antes de eliminar la canción actual
                if (_waveOut != null && _waveOut.PlaybackState == PlaybackState.Playing)
                {
                    _waveOut.Stop();
                    _waveOut = new WaveOutEvent();
                }
                
                // Eliminar la canción actual
                _album.Remove(_actual);
                BarraReproduccion.Value = 0;
                Tiempo.Content = "0:00";
                Console.WriteLine("llegamos aqui");
                Console.WriteLine(infoImage.Source);
                InvalidateVisual();
                // Restablecer la canción actual a null
                _actual = null;
                string directorioEjecutable = AppDomain.CurrentDomain.BaseDirectory;
                string directorioProyecto = Path.Combine(directorioEjecutable, "..", "..","..");
                string rutaImagen = Path.Combine(directorioProyecto, "not_found.png");
                infoImage.Source = new Bitmap(rutaImagen);
                InfoButton.Content = new Image
                {
                    Source = new Avalonia.Media.Imaging.Bitmap(rutaImagen),
                    Width = 100,
                    Height = 100,
                };
                rutaImagen = Path.Combine(directorioProyecto, "play.png");
                PlayImage.Source = new Bitmap(rutaImagen);
                MostrarElementos();
            }
        }
    }

    private void busqueda(object? sender, KeyEventArgs e)
    {
        buscar.Clear();
        string text;
        HashSet<Cancion> criterio = new HashSet<Cancion>();
        if (SearchBox.Text == "")
        {
            Console.WriteLine("Entra en el if");
            buscar = new HashSet<Cancion>();
        }
        else
        {
            if (SearchBox.Text != null)
            {
                text = SearchBox.Text;
                text = text.ToLower();
                foreach (var tema in _album)
                {
                    if (ComparaNPrimerasLetras(tema.titulo, text, text.Length))
                    {
                        criterio.Add(tema);
                    }
                }
            }

            List<Cancion> criterioOrdenado = criterio.OrderBy(tema => tema.titulo).ToList();
            
            foreach (var cancion in criterioOrdenado)
            {
                buscar.Add(cancion);
            }
        }
        MostrarElementos();
    }
    
    public static bool ComparaNPrimerasLetras(string cadena1, string cadena2, int n)
    {
        // Verifica que ambas cadenas tengan al menos n caracteres
        if (cadena1.Length < n || cadena2.Length < n)
        {
            return false;
        }
        string subcadena1 = cadena1.Substring(0, n);
        Console.WriteLine(subcadena1);
        string subcadena2 = cadena2.Substring(0, n);
        Console.WriteLine(subcadena2);
        Console.WriteLine(n);
        Console.WriteLine(string.Equals(subcadena1, subcadena2, StringComparison.OrdinalIgnoreCase));
        // Toma las primeras n letras de cada cadena y compáralas sin importar mayúsculas y minúsculas
        return string.Equals(subcadena1, subcadena2, StringComparison.OrdinalIgnoreCase);
    }

    private async void ModificarFuncion(object? sender, RoutedEventArgs e)
    {
        if (_actual == null)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "No hay ninguna cancion seleccionada", ButtonEnum.Ok);
            await box.ShowAsync();
        }
        else
        {
            var edit = new MODIFICAR();

            // Manejador de eventos para el evento Closed
            edit.Closed += (senderClosed, eClosed) =>
            {
                Console.WriteLine("Valor de edit.exito: " + edit.exito);
                if (edit.exito == true)
                {
                    _album.Add(edit.nueva);
                    Console.WriteLine("Se ha añadido la canción");
                    MostrarElementos();
                }
            };
            // Muestra la ventana
            edit.Show();
        }
    }
    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        string rutaArchivo = "databank.data";
        FileMode modoArchivo = File.Exists(rutaArchivo) ? FileMode.Append : FileMode.Create;

        using (FileStream archivo = new FileStream(rutaArchivo, modoArchivo))
        using (BinaryWriter bw = new BinaryWriter(archivo))
        {
            foreach (Cancion cancion in _album)
            {
                // Escribir campos en el archivo binario
                bw.Write(cancion.titulo);
                bw.Write(cancion.autor);
                bw.Write(cancion.fechaLanzamiento.ToBinary());
                bw.Write(cancion.bpm);
                bw.Write(cancion.minutos);
                bw.Write(cancion.segundos);
                bw.Write(cancion.key);

                // Escribir la longitud de la imagen como un valor 'int'
                Console.WriteLine("Antes de escribir, el length de la imagen es " + cancion.imagenPortada.Length);
                bw.Write(cancion.imagenPortada.Length);

                // Escribir los bytes de la imagen
                bw.Write(cancion.imagenPortada);
                
                Console.WriteLine("Antes de escribir, el length del audio es " + cancion.audio.Length);
                Console.WriteLine(cancion.audio.Length+" es un "+cancion.audio.Length.GetType());
                bw.Write(cancion.audio.Length);

                // Escribir los bytes del audio
                bw.Write(cancion.audio);

                bw.Write(cancion.rutaimagen);
                bw.Write(cancion.rutaaudio);
            }
        }
        
        if (closingInProgress)
        {
            return; // Evitar bucle infinito
        }
        e.Cancel = true; // Cancelamos el cierre predeterminado

        var box = MessageBoxManager
            .GetMessageBoxStandard("Aviso", "¿Estás seguro de que quieres cerrar la ventana?\n"+
                "Los cambios se guardaran automáticamente.",
                ButtonEnum.YesNo);

        var result = await box.ShowAsync();
        if (result == ButtonResult.Yes)
        {
            closingInProgress = true;
            Close(); // Llamamos al cierre después de confirmar
        }
    }
    private void LoadCanciones()
    {
        _album.Clear();
        string rutaArchivo = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "databank.data");

        FileMode modoArchivo = File.Exists(rutaArchivo) ? FileMode.Open : FileMode.OpenOrCreate;

        if (modoArchivo == FileMode.Open)
        {
            using (FileStream archivo = new FileStream(rutaArchivo, FileMode.Open))
            using (BinaryReader br = new BinaryReader(archivo))
            {
                while (br.BaseStream.Position < br.BaseStream.Length)
                {
                    // Leer campos desde el archivo binario
                    string titulo = br.ReadString();
                    string autor = br.ReadString();
                    DateTime fechaLanzamiento = DateTime.FromBinary(br.ReadInt64());
                    int bpm = br.ReadInt32();
                    int minutos = br.ReadInt32();
                    int segundos = br.ReadInt32();
                    string key = br.ReadString();
                    int imagenPortadaLength = br.ReadInt32();
                    Console.WriteLine("El length de imagen es " + imagenPortadaLength);
                    byte[] imagenPortada = new byte[imagenPortadaLength];
                    br.Read(imagenPortada, 0, imagenPortada.Length);
                    
                    int audioLength = br.ReadInt32();
                    Console.WriteLine("El lenght del audio es "+audioLength);
                    byte[] audio = new byte[audioLength];
                    br.Read(audio, 0, audio.Length);
                    string rutaimagen = br.ReadString();
                    string rutaaudio = br.ReadString();
                    Cancion cancion = new Cancion(titulo, autor, bpm, minutos, segundos, fechaLanzamiento, imagenPortada, audio, key);
                    cancion.rutaimagen = rutaimagen;
                    cancion.rutaaudio = rutaaudio;
                    bool iguales = false;
                    foreach (var tema in _album)
                    {
                        if (cancion.Equals(tema))
                        {
                            iguales = true;
                        }
                        
                    }
                    if (iguales == false)
                    {
                        _album.Add(cancion);
                    }
                    
                    
                }
            }
        }
    }
    
}

