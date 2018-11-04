FSharp.JsonSkippable
====================

JsonSkippable is an F# library for use with Newtonsoft.Json that allows you to easily differentiate between null and omitted JSON properties in a simple and strongly typed manner, for both serialization and deserialization.

Use it together with a library that allows you to serialize `option ` as `null`, such as [Microsoft.FSharpLu.Json](https://github.com/Microsoft/fsharplu/wiki/FSharpLu.Json), for optimal effect. (You can also use FSharp.JsonSkippable on its own just to control the presence of properties when serializing and deserializing.)

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

