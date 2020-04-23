namespace TodoApp

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Equinox.MemoryStore
open Todo
open Serilog
open Microsoft.OpenApi.Models

type Startup private () =
    new (configuration: IConfiguration) as this =
        Startup() then
        this.Configuration <- configuration

    member this.ConfigureServices(services: IServiceCollection) =
        services.AddControllers() |> ignore

        services.AddSingleton<Todo.Service>(fun sc ->
            let resolver =
                Equinox.MemoryStore.Resolver(VolatileStore(), Todo.Events.codec, Todo.Fold.fold, Todo.Fold.initial)

            let streamName id =
                FsCodec.StreamName.create "Todos" id

            Todo.Service(fun id -> Equinox.Stream(Serilog.Log.Logger, resolver.Resolve (streamName id), maxAttempts = 3)))
        |> ignore

        services.AddSwaggerGen(fun c ->
            c.SwaggerDoc("v1", OpenApiInfo(Title = "My API", Version = "v1"))
        ) |> ignore

    member this.Configure(app: IApplicationBuilder, env: IWebHostEnvironment) =
        if (env.IsDevelopment()) then
            app.UseDeveloperExceptionPage() |> ignore

        app.UseRouting() |> ignore

        app.UseEndpoints(fun endpoints ->
            endpoints.MapControllers() |> ignore
            ) |> ignore

        app.UseSwagger() |> ignore

        app.UseSwaggerUI(fun c ->
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1")
        ) |> ignore


    member val Configuration : IConfiguration = null with get, set
