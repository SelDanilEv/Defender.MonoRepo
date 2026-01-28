namespace MigratorLib.Settings;

public enum Env
{
    Local,
    Dev,
    Prod
}

public static class EnvHelper
{
    public static string AsDBPrefix(this Env env)
    {
        return env switch
        {
            Env.Local => "local_",
            Env.Dev => "dev_",
            Env.Prod => "prod_",
            _ => ""
        };
    }

    public static string AsDBName(this Env env, string collectionName)
    {
        return $"{env.AsDBPrefix()}{collectionName}";
    }

    public static string AsConnectionStringKey(this Env env) => env switch
    {
        Env.Local => "LocalMongoDBConnectionString",
        Env.Dev => "DevMongoDBConnectionString",
        Env.Prod => "CloudMongoDBConnectionString",
        _ => throw new ArgumentException("No connection string for env")
    };
}
