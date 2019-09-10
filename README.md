# Fable.CloudFlareWorkers

Write [CloudFlare Workers](https://workers.cloudflare.com/) in idiomatic, type-safe F# and compile them to JS using [Fable](https://github.com/fable-compiler/Fable)

### Install the CloudFlare Worker APIs
```
dotnet add package Fable.CloudFlareWorkers
```
### Write your first worker
```fs
module App

open CloudFlareWorkers

let worker request =
    async {
        match Request.method request, Request.url request with
        | HttpMethod.GET, "/" ->
            return Response.create(body="Home", status=200)

        | HttpMethod.POST, "/echo" ->
            let! body = Request.body request
            return Response.create(body=body, status=200)

        | otherwise ->
            let body = "<h1>Not Found</h1>"
            let headers = [ "Content-type", "text/html" ]
            return Response.create(body=body, status=404, headers=headers)
    }

Worker.initialize worker
```