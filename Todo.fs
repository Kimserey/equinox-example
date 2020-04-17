module TodoBackend

let Category = "Todos"
let streamName (id : string) = FsCodec.StreamName.create Category id

module Events =
    type Todo =  { id: int; title: string; completed: bool }

    type Event =
        | Added         of Todo
        | Updated       of Todo
        interface TypeShape.UnionContract.IUnionContract

    let codec = FsCodec.NewtonsoftJson.Codec.Create<Event>()

module Fold =
    type State = { items : Events.Todo list; nextId : int }

    let initial = { items = []; nextId = 0 }

    let evolve s e =
        match e with
        | Events.Added item ->
            { s with
                items = item :: s.items
                nextId = s.nextId + 1 }

        | Events.Updated value ->
            let items =
                s.items |> List.map (function { id = id } when id = value.id -> value | item -> item)

            { s with items = items }

    let fold : State -> Events.Event seq -> State =
        Seq.fold evolve

type Command =
    | Add of Events.Todo
    | Update of Events.Todo

let interpret c (state : Fold.State) =
    match c with
    | Add value ->
        [Events.Added { value with id = state.nextId }]

    | Update value ->
        match state.items |> List.tryFind (function { id = id } -> id = value.id) with
        | Some current when current <> value -> [Events.Updated value]
        | _ -> []

type Service (resolve : string -> Equinox.Stream<Events.Event, Fold.State>) =

    let handle clientId command =
        let stream = resolve clientId
        stream.Transact(fun state ->
            let events = interpret command state
            let state' = Fold.fold state events
            state'.items,events)

    member __.List(clientId) : Async<Events.Todo seq> =
        let stream = resolve clientId
        stream.Query (fun s -> s.items |> Seq.ofList)

    member __.Create(clientId, template: Events.Todo) : Async<Events.Todo> = async {
        let! state' = handle clientId (Command.Add template)
        return List.head state' }

    member __.Patch(clientId, item: Events.Todo) : Async<Events.Todo> = async {
        let! state' = handle clientId (Command.Update item)
        return List.find (fun x -> x.id = item.id) state' }
