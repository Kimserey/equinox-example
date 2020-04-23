module Todo

module Events =
    type TodoData =  { id: int; title: string; completed: bool }

    type Event =
        | Added         of TodoData
        | Updated       of TodoData
        interface TypeShape.UnionContract.IUnionContract

    let codec = FsCodec.NewtonsoftJson.Codec.Create<Event>()

module Fold =
    type State = { items : Events.TodoData list; nextId : int }

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
    | Add of Events.TodoData
    | Update of Events.TodoData

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
            let newState = Fold.fold state events
            newState.items,events)

    member __.List(clientId) : Async<Events.TodoData seq> =
        let stream = resolve clientId
        stream.Query (fun s -> s.items |> Seq.ofList)

    member __.Create(clientId, template: Events.TodoData) : Async<Events.TodoData> = async {
        let! newState = handle clientId (Command.Add template)
        return List.head newState }

    member __.Patch(clientId, item: Events.TodoData) : Async<Events.TodoData> = async {
        let! newState = handle clientId (Command.Update item)
        return List.find (fun x -> x.id = item.id) newState }
