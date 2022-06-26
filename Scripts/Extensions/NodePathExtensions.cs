using Godot;
using System;
using System.Collections.Generic;

public static class NodePathExtensions
{
    public static List<string> ToNameList(this NodePath nodePath)
    {
        var result = new List<string>();
        for (int i = 0; i < nodePath.GetNameCount(); i ++) result.Add(nodePath.GetName(i));
        return result;
    }

    public static NodePath FromList(List<string> names, bool isAbsolute=true)
    {
        var result = new NodePath(String.Join("/", names));
        if (isAbsolute) result = "/" + result;
        return result;
    }
}