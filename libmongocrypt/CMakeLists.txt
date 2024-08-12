
configure_file(cs.sln cs.sln COPYONLY)
configure_file(MongoDB.Driver.snk MongoDB.Driver.snk COPYONLY)

configure_file(MongoDB.Libmongocrypt/MongoDB.Libmongocrypt.csproj MongoDB.Libmongocrypt/MongoDB.Libmongocrypt.csproj COPYONLY)
configure_file(MongoDB.Libmongocrypt/Package.include.template.csproj MongoDB.Libmongocrypt/Package.csproj.include)

configure_file(MongoDB.Libmongocrypt.Test/MongoDB.Libmongocrypt.Test.csproj MongoDB.Libmongocrypt.Test/MongoDB.Libmongocrypt.Test.csproj COPYONLY)
configure_file(MongoDB.Libmongocrypt.Test/Package.include.template.csproj MongoDB.Libmongocrypt.Test/Package.csproj.include)

configure_file(MongoDB.Libmongocrypt.Test/MongoDB.Libmongocrypt.Test.csproj MongoDB.Libmongocrypt.Test32/MongoDB.Libmongocrypt.Test32.csproj COPYONLY)
configure_file(MongoDB.Libmongocrypt.Test/Package.include.template.csproj MongoDB.Libmongocrypt.Test32/Package.csproj.include)

configure_file(MongoDB.Libmongocrypt.Example/MongoDB.Libmongocrypt.Example.csproj MongoDB.Libmongocrypt.Example/MongoDB.Libmongocrypt.Example.csproj COPYONLY)
configure_file(MongoDB.Libmongocrypt.Example/Package.include.template.csproj MongoDB.Libmongocrypt.Example/Package.csproj.include)


