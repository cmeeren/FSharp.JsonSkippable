module FSharp.JsonSkippable.Tests.SerializationTests

open Xunit
open Hedgehog
open Swensen.Unquote
open Newtonsoft.Json
open Newtonsoft.Json.Linq

open FSharp.JsonSkippable
open FSharp.JsonSkippable.Serialization

let settings = JsonSerializerSettings(ContractResolver = SkippableContractResolver())
let serialize x = JsonConvert.SerializeObject(x, settings)
let deserialize<'a> x = JsonConvert.DeserializeObject<'a>(x, settings)

type TestNonSkippable =
  { X: int
    XS: int
    Y: string
    YS: string
    Z: bool
    ZS: bool }

type TestSkippable =
  { X: int
    XS: int Skippable
    Y: string
    YS: string Skippable
    Z: bool
    ZS: bool Skippable }

let hasProp prop (json: string) =
  JObject.Parse(json).Item prop |> isNull |> not

[<Fact>]
let ``serialize and deserialize returns original input`` () =
  Property.check <| property {
    let! input = GenX.auto
    test <@ input |> serialize |> deserialize<TestSkippable> = input @>
}

[<Fact>]
let ``normal values are always included in the output`` () =
  Property.check <| property {
    let! input = GenX.auto<TestSkippable>
    let input = { input with XS = Skip; YS = Skip; ZS = Skip }
    let serialized = serialize input
    test <@ serialized |> hasProp "X" @>
    test <@ serialized |> hasProp "Y" @>
    test <@ serialized |> hasProp "Z" @>
}

[<Fact>]
let ``Skip values are never included in the output`` () =
  Property.check <| property {
    let! input = GenX.auto<TestSkippable>
    let input = { input with XS = Skip; YS = Skip; ZS = Skip }
    let serialized = serialize input
    test <@ serialized |> hasProp "XS" |> not @>
    test <@ serialized |> hasProp "YS" |> not @>
    test <@ serialized |> hasProp "ZS" |> not @>
}

[<Fact>]
let ``Include values are always included in the output`` () =
  Property.check <| property {
    let! input = GenX.auto<TestSkippable>
    let! x = GenX.auto
    let! y = GenX.auto
    let! z = GenX.auto
    let input = { input with XS = Include x; YS = Include y; ZS = Include z }
    let serialized = serialize input
    test <@ serialized |> hasProp "XS" @>
    test <@ serialized |> hasProp "YS" @>
    test <@ serialized |> hasProp "ZS" @>}

[<Fact>]
let ``Include values are serialized similarly to normal values`` () =
  Property.check <| property {
    let! normal = GenX.auto<TestNonSkippable>
    let skippable =
      { X = normal.X
        XS = Include normal.XS
        Y = normal.Y
        YS = Include normal.YS
        Z = normal.Z
        ZS = Include normal.ZS }
    test <@ serialize skippable = serialize normal @>
    test <@ skippable |> serialize |> deserialize<TestNonSkippable> = normal @>
}
