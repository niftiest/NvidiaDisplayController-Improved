using System;
using System.IO;
using FluentResults;
using Newtonsoft.Json;
using NvidiaDisplayController.Objects;
using NvidiaDisplayController.Objects.Entities;

namespace NvidiaDisplayController.Global.Controllers;

public class DataController
{
    private static readonly string _directory = AppContext.BaseDirectory;

    public string DataPath => Path.Combine(_directory, @"Data\Data.json");

    public void Write(Computer data)
    {
        var directory = Path.GetDirectoryName(DataPath)!;
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var serializeObject = JsonConvert.SerializeObject(data, new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects
        });

        File.WriteAllText(DataPath, serializeObject);
    }

    public Result<Computer> Load()
    {
        if (!File.Exists(DataPath))
        {
            var computer = new Computer();
            Write(computer);
            return Result.Ok(computer);
        }

        using StreamReader reader = new(DataPath);
        {
            var json = reader.ReadToEnd();

            var computer = JsonConvert.DeserializeObject<Computer>(json);
            reader.Close();

            var result = computer is null ? Result.Fail(new Error("")) : Result.Ok(computer);
            return result;
        }
    }
}