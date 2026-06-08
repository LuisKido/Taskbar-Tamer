using Godot;
using TaskbarTamer.Core.Persistence;

namespace TaskbarTamer.Game;

/// <summary>
/// Lee/escribe el <see cref="SaveData"/> en <c>user://</c> usando el FileAccess de
/// Godot. La (de)serialización la hace core/ (<see cref="SaveSerializer"/>); aquí solo
/// vive el I/O de archivo, que es lo único que depende del motor.
/// </summary>
public static class SaveStore
{
    private const string SavePath = "user://savegame.json";

    public static bool Exists() => Godot.FileAccess.FileExists(SavePath);

    public static SaveData? Load()
    {
        if (!Godot.FileAccess.FileExists(SavePath))
            return null;

        using Godot.FileAccess file = Godot.FileAccess.Open(SavePath, Godot.FileAccess.ModeFlags.Read);
        if (file is null)
        {
            GD.PushError($"No se pudo abrir el save: {Godot.FileAccess.GetOpenError()}");
            return null;
        }

        string json = file.GetAsText();
        return SaveSerializer.Deserialize(json);
    }

    public static void Save(SaveData data)
    {
        string json = SaveSerializer.Serialize(data);

        using Godot.FileAccess file = Godot.FileAccess.Open(SavePath, Godot.FileAccess.ModeFlags.Write);
        if (file is null)
        {
            GD.PushError($"No se pudo escribir el save: {Godot.FileAccess.GetOpenError()}");
            return;
        }

        file.StoreString(json);
    }
}
