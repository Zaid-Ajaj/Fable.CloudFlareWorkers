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
            let body = "{ \"message\": \"Not Found\" }"
            let headers = [ "Content-type", "application/json" ]
            return Response.create(body, status=404, headers=headers)
    }

Worker.initialize worker