namespace TodoApp.Controllers

open System
open Microsoft.AspNetCore.Mvc
open Todo

[<CLIMutable>]
type TodoDto = { id: int; title: string; completed: bool }

[<ApiController>]
[<Route("[controller]")>]
type TodoController (service: Service) =
    inherit ControllerBase()

    let clientId = Guid.Empty.ToString("N")

    [<HttpGet>]
    member __.Get() = async {
        let! xs = service.List(clientId)
        return xs
    }

    [<HttpPost>]
    member __.Post([<FromBody>]value : TodoDto) : Async<TodoDto> = async {
        let! _ = service.Create(clientId, { id = 0; title = value.title; completed = false })
        return value
    }

    [<HttpPatch "{id}">]
    member __.Patch(id, [<FromBody>]value : TodoDto) : Async<TodoDto> = async {
        let! _ = service.Patch(clientId, { id = id; title = value.title; completed = value.completed })
        return value
    }