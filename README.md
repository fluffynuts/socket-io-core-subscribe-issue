Socket.Io.Core issue reproduction
---

To repro automatically:
1. `npm ci` in the root
2. `npm test` in the root
  - should:
    - install node_modules for the `minimal-pubsub` project
    - restore .net packages in the test project
    - start up the `minimal-pubsub` server to run for 10s only
    - kick off the tests with `dotnet test`

To repro manually:

1. in `minimal-pubsub`, do:
    a. `npm ci`
    b. `npm start`
2. open the `SocketIoCoreTests.sln` solution
3. run the only test in the solution
