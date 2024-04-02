using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;

namespace Alb8m2;
public class Cancion
{
    public string titulo { get; set; }
    public string autor { get; set; }
    public int bpm { get; set; }
    public DateTime fechaLanzamiento { get; set; }
    public int minutos { get; set; }
    public int segundos { get; set; }
    public string key { get; set; }
    public byte[] imagenPortada { get; set; }
    public byte[] audio { get; set; }
    public string rutaimagen { get; set; }
    public string rutaaudio { get; set; }
    
    
    public Cancion(string titulo, string autor,int bpm, int minutos, int segundos, DateTime fechaLanzamiento, byte[] imagenPortada, byte[] audio,string key)
    {
        this.titulo = titulo;
        this.autor = autor;
        this.bpm = bpm;
        this.minutos = minutos;
        this.segundos = segundos;
        this.fechaLanzamiento = fechaLanzamiento;
        this.imagenPortada = imagenPortada;
        this.audio = audio;
        this.key = key;
    }
    public override string ToString()
    {
        string misSec =(segundos<10)?'0'+segundos.ToString():segundos.ToString();
        return $"Título: {titulo}\n" +
               $"Autor: {autor}\n" +
               $"BPM: {bpm}\n" +
               $"Fecha de Lanzamiento: {fechaLanzamiento.ToShortDateString()}\n" +
               $"Duración: {minutos}:{misSec}\n"+
               $"Key: {key}\n" ;
    }
    public byte[] ToBinary()
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            WriteString(writer, titulo);
            WriteString(writer, autor);
            writer.Write(bpm);
            writer.Write(fechaLanzamiento.Ticks);
            writer.Write(minutos);
            writer.Write(segundos);
            WriteString(writer, key);
            WriteByteArray(writer, imagenPortada);
            WriteByteArray(writer, audio);
            WriteString(writer, rutaimagen);
            WriteString(writer, rutaaudio);
            return stream.ToArray();
        }
    }
    
    private void WriteString(BinaryWriter writer, string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        writer.Write(bytes.Length);
        writer.Write(bytes);
    }

    private void WriteByteArray(BinaryWriter writer, byte[] value)
    {
        writer.Write(value.Length);
        writer.Write(value);
    }

    public static Cancion FromBinary(byte[] data)
    {
        Cancion cancion;

        using (MemoryStream stream = new MemoryStream(data))
        using (BinaryReader reader = new BinaryReader(stream))
        {
            string titulo = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
            string autor = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
            int bpm = reader.ReadInt32();
            DateTime fechaLanzamiento = new DateTime(reader.ReadInt64()); 
            int minutos = reader.ReadInt32();
            int segundos = reader.ReadInt32();
            string key = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
            int imagenPortadaLength = reader.ReadInt32();
            byte[] imagenPortada = reader.ReadBytes(imagenPortadaLength);
            int audioLength = reader.ReadInt32();
            byte[] audio = reader.ReadBytes(audioLength);
            string rutaimagen = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
            string rutaaudio = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
            cancion = new Cancion(titulo,autor,bpm,minutos,segundos,fechaLanzamiento,imagenPortada,audio,key);
        }

        return cancion;
    }
    
    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;

        Cancion other = (Cancion)obj;

        // Check for equality based on all properties
        return string.Equals(titulo, other.titulo) &&
               string.Equals(autor, other.autor) &&
               bpm == other.bpm &&
               fechaLanzamiento == other.fechaLanzamiento &&
               minutos == other.minutos &&
               segundos == other.segundos &&
               string.Equals(key, other.key) &&
               imagenPortada.SequenceEqual(other.imagenPortada) &&
               audio.SequenceEqual(other.audio) &&
               string.Equals(rutaimagen, other.rutaimagen) &&
               string.Equals(rutaaudio, other.rutaaudio);
    }
    

}