FSharp.JsonSkippable
====================


[![NuGet](https://img.shields.io/nuget/dt/FSharp.JsonSkippable.svg?style=flat)](https://www.nuget.org/packages/FSharp.JsonSkippable/) [![Build status](https://ci.appveyor.com/api/projects/status/cpc7ej4b31sstihx/branch/master?svg=true)](https://ci.appveyor.com/project/cmeeren/fsharp-jsonskippable/branch/master)

JsonSkippable is an F# library for use with Newtonsoft.Json that allows you to easily differentiate between null and omitted JSON properties in a simple and strongly typed manner, for both serialization and deserialization.

For optimal effect, use it together with a library that allows you to serialize `option ` as `null`, such as [Microsoft.FSharpLu.Json](https://github.com/Microsoft/fsharplu/wiki/FSharpLu.Json). You can also use FSharp.JsonSkippable on its own just to control the presence of properties when serializing and deserializing.

TL;DR
-----

This library defines the following type:

```F#
type Skippable<'T> =
  | Skip
  | Include of 'T
```

as well as a module with helper methods (`Skippable.map`, `Skippable.bind`, etc.) and a Newtonsoft.Json `ContractResolver` that manages the serialization and deserialization of the `Skippable` type.

You can define your serializable types like this to have full control of whether to include or exclude properties:

```F#
type Example =
  { A: int option Skippable
    B: bool option Skippable }
```

Example
-------

```F#
// Where you set up serialization

open Newtonsoft.Json

let settings =
  JsonSerializerSettings(
    ContractResolver = 
      FSharp.JsonSkippable.Serialization.SkippableContractResolver()
    )
settings.Converters.Add(Microsoft.FSharpLu.Json.CompactUnionJsonConverter())
let serialize x = JsonConvert.SerializeObject(x, settings)
let deserialize<'a> x = JsonConvert.DeserializeObject<'a>(x, settings)


// Where you define and use your types

open FSharp.JsonSkippable

[<CLIMutable>]
type Example =
  { A: int option Skippable
    B: bool option Skippable }
    
    
// Example of serialization
    
let x1 = { A = Include (Some 2); B = Include (Some true) }
let x1s = serialize x1  // {"A":2,"B":true}

let x2 = { A = Include None; B = Skip }
let x2s = serialize x2  // {"A":null}


// Example of deserialization

let x1' = deserialize<Example> x1s
x1' = x1  // true

let x2' = deserialize<Example> x2s
x2' = x2  // true


// You can then pattern match on the members like you'd expect
match x1'.A with
| Include (Some i) -> ...  // A was an int
| Include None -> ...  // A was null
| Skip -> ...  // A was not present
```

