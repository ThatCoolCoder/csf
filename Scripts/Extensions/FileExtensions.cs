using Godot;
using System;
using System.IO;

public static class FileExtensions
{
    public static byte[] GetBuffer(this Godot.File file)
    {
        return file.GetBuffer((long) file.GetLen());
    }
}