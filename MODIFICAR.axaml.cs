using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace Alb8m2;

public partial class MODIFICAR : Window
{
    public bool exito = false;
    private bool closingInProgress = false;
    public Cancion nueva;
    public MODIFICAR()
    {
        string iconPath = "LOGO_1.ico";
        // Set the application icon
        Icon = new WindowIcon(iconPath);
        InitializeComponent();
        nueva = MainWindow.getCancion();
        Console.WriteLine("titulo es null"+(TituloBox==null));
        TituloBox.Text = nueva.titulo;
        Autor.Text = nueva.autor;
        Bpm.Value = nueva.bpm;
        int cosa = nueva.minutos;
        string cosa2 = cosa.ToString();
        SeleccionarItemPorValor(Minutos,cosa2);
        cosa = nueva.segundos;
        cosa2 = cosa.ToString();
        SeleccionarItemPorValor(Segundos,cosa2);
        SeleccionarItemPorValor(Key,nueva.key);
        Fecha.SelectedDate = nueva.fechaLanzamiento;
        Portada.Text = nueva.rutaimagen;
        Audio.Text = nueva.rutaaudio;
    }
    
    private void Bpm_TextInput(object? sender, TextInputEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Text) && !int.TryParse(e.Text, out int numero))
        {
            e.Handled = true; // Ignorar la entrada no válida
        }
        
        // Validar el rango (0-999)
        if (!string.IsNullOrEmpty(Bpm.Text) && int.TryParse(Bpm.Text, out int valor) && (valor < 0 || valor > 999))
        {
            e.Handled = true; // Ignorar la entrada que supera el rango
        }
    }

    private void clic_textbox(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
    }

    private void release_Textbox(object? sender, PointerReleasedEventArgs e)
    {
        e.Handled = true;
    }

    private async void  RechazarButton_OnClick(object? sender, RoutedEventArgs e)
    {
     
        var box = MessageBoxManager
            .GetMessageBoxStandard("Aviso", "¿Estas seguro de que quieres salir?",
                ButtonEnum.YesNo);

        var result = await box.ShowAsync();
        if (result == ButtonResult.Yes)
        {
            exito = false;
            Close();
        }
        
    }

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        if (closingInProgress)
        {
            return; // Evitar bucle infinito
        }
        e.Cancel = true; // Cancelamos el cierre predeterminado

        var box = MessageBoxManager
            .GetMessageBoxStandard("Aviso", "¿Estás seguro de que quieres cerrar la ventana?",
                ButtonEnum.YesNo);

        var result = await box.ShowAsync();
        if (result == ButtonResult.Yes)
        {
            exito = false;
            closingInProgress = true;
           Close(); // Llamamos al cierre después de confirmar
        }
    }

    private async void aceptarButton(object? sender, RoutedEventArgs e)
    {
        bool repe = false;
        HashSet<Cancion> temas = MainWindow.getAlbum();
        foreach (var tema in temas)
        {
            if (Autor.Text != null && (tema.titulo.ToLower() == TituloBox.Text.ToLower()))
            {
                repe = true;
            }
        }
        
        Console.WriteLine(Fecha.SelectedDate.HasValue);
        if (string.IsNullOrEmpty(TituloBox.Text))
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "No puedes dejar el titulo vacio", ButtonEnum.Ok);
            var result = await box.ShowAsync();
            
        }else if(string.IsNullOrEmpty(Autor.Text))
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "No puedes dejar el autor vacio", ButtonEnum.Ok);
            var result = await box.ShowAsync();
            
        }else if(string.IsNullOrEmpty(Bpm.Text))
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "No puedes dejar los bpm vacios", ButtonEnum.Ok);
            var result = await box.ShowAsync();
            
        }else if(int.TryParse(Bpm.Text,out int bpm) && (bpm < 50 || bpm > 999))
        {
                // Código a ejecutar si los BPM están fuera del rango permitido
                var box = MessageBoxManager.GetMessageBoxStandard("Error", "Los BPMs deben ser un valor entre 50 y 1000", ButtonEnum.Ok);
                var result = await box.ShowAsync();
        }else if (Fecha.SelectedDate.HasValue == false ) {
            Console.WriteLine("entra en el if");
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "No debes dejar la fecha vacia", ButtonEnum.Ok);
            var result = await box.ShowAsync();
        }else if (Minutos.SelectedIndex == -1 || Segundos.SelectedIndex == -1)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Selecciona el minutaje exacto de tu canción", ButtonEnum.Ok);
            var result = await box.ShowAsync();
        }else if (Key.SelectedIndex == -1)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Selecciona el tono de tu canción", ButtonEnum.Ok);
            var result = await box.ShowAsync();
        }else if(string.IsNullOrEmpty(Portada.Text)) {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Selecciona la portada de tu canción", ButtonEnum.Ok);
            var result = await box.ShowAsync();
        }else if(string.IsNullOrEmpty(Audio.Text)) {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Selecciona el audio de tu canción", ButtonEnum.Ok);
            var result = await box.ShowAsync();
        }
        else if (repe)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Ya existe el titulo, introduzca otro", ButtonEnum.Ok);
            var result = await box.ShowAsync();
        }
        else
        {
            DateTimeOffset fechaOffset = Fecha.SelectedDate.Value;
            DateTime fechaDateTime = fechaOffset.DateTime;
            byte[] imagen = ObtenerBytes(Portada.Text);
            byte[] audio = ObtenerBytes(Audio.Text);

            int mibpm;
            if (int.TryParse(Bpm.Text, out mibpm))
            {
                Console.WriteLine("Conversión BPM exitosa");
            }
            else
            {
                Console.WriteLine("Error: BPM no es un número válido");
                // Puedes manejar el error de BPM aquí según tus necesidades.
                // Por ejemplo, mostrar un mensaje de error al usuario.
                return; // Termina la ejecución porque no se puede continuar sin un BPM válido.
            }

            int bpmnum = mibpm;
            ComboBoxItem item = Minutos.SelectedItem as ComboBoxItem;
            int minutosnum;
            if (item != null)
            {
                // Accede al contenido del ComboBoxItem
                object contenido = item.Content;
                string contenidoComoString = contenido.ToString();
                // Verifica si el contenido es de tipo int antes de intentar la conversión
                
                if (int.TryParse(contenidoComoString, out  minutosnum))
                {
                    Console.WriteLine("Conversión exitosa: " + minutosnum);
                }
                else
                {
                    Console.WriteLine("El contenido no es un número entero válido.");
                }
            }
            else
            {
                Console.WriteLine("No hay un ComboBoxItem seleccionado.");
                return;
            }
            ComboBoxItem item2 = Segundos.SelectedItem as ComboBoxItem;
            int segundosnum;
            if (item2 != null)
            {
                // Accede al contenido del ComboBoxItem
                object contenido = item2.Content;
                string contenidoComoString = contenido.ToString();
                // Verifica si el contenido es de tipo int antes de intentar la conversión
                
                if (int.TryParse(contenidoComoString, out  segundosnum))
                {
                    Console.WriteLine("Conversión exitosa: " + segundosnum);
                }
                else
                {
                    Console.WriteLine("El contenido no es un número entero válido.");
                }
            }
            else
            {
                Console.WriteLine("No hay un ComboBoxItem seleccionado.");
                return;
            }
            
            foreach (var tema in MainWindow._album)
            {
                if (tema == nueva)
                {
                    MainWindow._album.Remove(tema);
                }
            }
            
            string keyValue = "";
            if (Key.SelectedItem is Avalonia.Controls.ComboBoxItem selectedItem)
            {
                keyValue = selectedItem.Content.ToString();
            }
            nueva = new Cancion(TituloBox.Text, Autor.Text, bpmnum, minutosnum, segundosnum, fechaDateTime, imagen, audio,keyValue);
            exito = true;
            closingInProgress = true;
            Close();
        }
    }
    static byte[] ObtenerBytes(string ruta)
    {
        try
        {
            // Verificar si el archivo existe
            if (File.Exists(ruta))
            {
                // Leer todos los bytes del archivo
                byte[] bytes = File.ReadAllBytes(ruta);
                return bytes;
            }
            else
            {
                Console.WriteLine($"El archivo en la ruta '{ruta}' no existe.");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al leer la imagen: {ex.Message}");
            return null;
        }
    }

    private async void PortadaClick(object? sender, RoutedEventArgs e)
    {
        // Crear el OpenFileDialog
        OpenFileDialog openFileDialog = new OpenFileDialog();

        // Configurar propiedades según sea necesario
        openFileDialog.Title = "Seleccionar Archivo";
        openFileDialog.AllowMultiple = false;
        openFileDialog.Filters.Add(new FileDialogFilter { Name = "Imágenes", Extensions = { "jpg", "jpeg", "png" } });

        // Mostrar el diálogo y esperar la respuesta
        var result = await openFileDialog.ShowAsync(this);

        // Procesar el resultado del diálogo
        if (result != null && result.Length > 0)
        {
            // Seleccionaste al menos un archivo
            string archivoSeleccionado = result[0];
            Console.WriteLine("Archivo seleccionado: " + archivoSeleccionado);
            Portada.Text = archivoSeleccionado;
        }
        else
        {
            // No seleccionaste ningún archivo
            Console.WriteLine("No se seleccionó ningún archivo.");
        }
    }

    private async void AudioClick (object? sender, RoutedEventArgs e)
    {
        // Crear el OpenFileDialog
        OpenFileDialog openFileDialog = new OpenFileDialog();

        // Configurar propiedades según sea necesario
        openFileDialog.Title = "Seleccionar Archivo de Audio";
        openFileDialog.AllowMultiple = false;

        // Filtro para archivos de audio (mp3)
        openFileDialog.Filters.Add(new FileDialogFilter { Name = "Archivos de Audio", Extensions = { "mp3" } });

        // Mostrar el diálogo y esperar la respuesta
        var result = await openFileDialog.ShowAsync(this);

        // Procesar el resultado del diálogo
        if (result != null && result.Length > 0)
        {
            // Seleccionaste al menos un archivo de audio
            string archivoSeleccionado = result[0];
            Audio.Text = archivoSeleccionado;
        }
    }
    private void SeleccionarItemPorValor(Avalonia.Controls.ComboBox comboBox, string valor)
    {
        for (int i = 0; i < comboBox.Items.Count; i++)
        {
            // Verifica si el elemento es un Avalonia.Controls.ComboBoxItem
            if (comboBox.Items[i] is Avalonia.Controls.ComboBoxItem comboBoxItem)
            {
                // Accede al contenido del ComboBoxItem (puede ser un control interno como TextBlock)
                string contenido = comboBoxItem.Content?.ToString();
                if (valor == contenido)
                {
                    // Si el valor coincide, establece el índice seleccionado y sale del bucle
                    comboBox.SelectedIndex = i;
                    break;
                }

                // Realiza alguna operación con el contenido, por ejemplo, imprímelo en la consola
                Console.WriteLine(contenido);
            }
        }
    }
}