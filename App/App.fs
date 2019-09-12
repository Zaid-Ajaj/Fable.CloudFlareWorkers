module App

open CloudFlareWorkers

let worker (request: IHttpRequest) =
    async {
        match request.method, request.path with
        | HttpMethod.GET, "/" ->
            return Response.create(body="Home, sweet home", status=200)

        | HttpMethod.POST, "/echo" ->
            let! body = request.body()
            return Response.create(body=body, status=200)

        | HttpMethod.GET, "/headers" ->
            let headers =
                request.headers()
                |> Map.toList
                |> List.map (fun (key, value) -> sprintf "(%s, %s)" key value)
                |> String.concat "; "
                |> sprintf "[%s]"

            return Response.create(body=headers, status=200)

        | otherwise ->
            let body = "{ \"message\": \"Not Found\" }"
            let headers = Map.ofList [ "content-type", "application/json" ]
            return Response.create(body, status=404, headers=headers)
    }

let echo (context: IRequestContext) =
    async {
        let request = context.request
        return! context.fetch request
    }

Worker.initialize echo