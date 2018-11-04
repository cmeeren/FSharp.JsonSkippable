namespace FSharp.JsonSkippable.Serialization

open System
open System.Reflection

open Newtonsoft.Json
open Newtonsoft.Json.Serialization

open FSharp.JsonSkippable


type private SkippableJsonConverter<'a>() =
  inherit JsonConverter()

  static member Instance = SkippableJsonConverter<'a>()

  override __.CanConvert (t: Type) =
    t = typeof<Skippable<'a>>

  override __.WriteJson(writer: JsonWriter, value: obj, serializer: JsonSerializer) =
    match value with
    | :? Skippable<'a> as s ->
        match s with
        | Include x -> serializer.Serialize(writer, x)
        | Skip -> failwith "Converter got Skip case. This may happen if Skippable is used for elements in a collection. Skippable is only supported as members on a type."
    | x -> failwithf "Converter got unknown type %s" (x.GetType().FullName)

  override __.ReadJson(reader: JsonReader, t: Type, existing: obj, serializer: JsonSerializer) =
    let valueType = t.GetGenericArguments().[0]
    if valueType <> typeof<'a> then failwith "Types don't match, this should never happen"
    let token = Linq.JToken.ReadFrom reader
    match token.Type with
    | Linq.JTokenType.Null when not typeof<'a>.IsValueType -> Unchecked.defaultof<'a> |> Include |> box
    | _ -> serializer.Deserialize(token.CreateReader(), valueType) :?> 'a |> Include |> box


type SkippableContractResolver() =
  inherit DefaultContractResolver()

  static member private GetSkippableJsonConverter<'a>() =
    SkippableJsonConverter<'a>.Instance

  override this.ResolveContract(t: Type) =
    let contract = base.ResolveContract t
    if isNull contract.Converter
        && t.IsGenericType
        && t.GetGenericTypeDefinition() = typedefof<Skippable<_>>
    then
      let innerType = t.GetGenericArguments().[0]
      let (genericMethod: MethodInfo) = this.GetAndMakeGenericMethod("GetSkippableJsonConverter", innerType)
      let converter = genericMethod.Invoke(null, null) :?> JsonConverter
      contract.Converter <- converter
    contract

  override this.CreateProperty(mi: MemberInfo, ms: MemberSerialization) =
    let jsonProperty = base.CreateProperty(mi, ms)
    let t = jsonProperty.PropertyType
    if t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<Skippable<_>> then
      let innerType = t.GetGenericArguments().[0]
      let (genericMethod: MethodInfo) = this.GetAndMakeGenericMethod("SetJsonPropertyValuesForSkippableMember", innerType)
      genericMethod.Invoke(null, [| mi.Name; jsonProperty |] ) |> ignore
    jsonProperty

  static member SetJsonPropertyValuesForSkippableMember<'a>(memberName: string, jsonProperty:JsonProperty) =
    if isNull jsonProperty.ShouldSerialize then
      jsonProperty.ShouldSerialize <-
        fun declaringObject ->
            if not <| isNull jsonProperty.GetIsSpecified && jsonProperty.GetIsSpecified.Invoke declaringObject then
              true
            else
              match SkippableContractResolver.TryGetPropertyValue(declaringObject, memberName) with
              | Some (x: obj) -> match (x :?> Skippable<'a>) with Skip -> false | _ -> true
              | None ->
                  match SkippableContractResolver.TryGetFieldValue(declaringObject, memberName) with
                  | Some (x: obj) -> match (x :?> Skippable<'a>) with Skip -> false | _ -> true
                  | None -> failwith "Could not find relevant property or field"
    if isNull jsonProperty.Converter then
      jsonProperty.Converter <- SkippableContractResolver.GetSkippableJsonConverter<'a>()

  member this.GetAndMakeGenericMethod(methodName: string, [<ParamArray>] typeArguments: Type array) =
    let method = this.GetType().GetMethod(methodName, BindingFlags.NonPublic ||| BindingFlags.Public ||| BindingFlags.Static ||| BindingFlags.FlattenHierarchy)
    method.MakeGenericMethod(typeArguments)

  static member TryGetPropertyValue(declaringObject: obj, propertyName: string) =
      let propertyInfo = declaringObject.GetType().GetProperty(propertyName, BindingFlags.NonPublic ||| BindingFlags.Public ||| BindingFlags.Instance)
      match propertyInfo with
      | null -> None
      | pi -> Some <| pi.GetValue(declaringObject, BindingFlags.GetProperty, null, null, null)

  static member TryGetFieldValue(declaringObject: obj, fieldName: string) =
      let fieldInfo = declaringObject.GetType().GetField(fieldName, BindingFlags.NonPublic ||| BindingFlags.Public ||| BindingFlags.Instance)
      match fieldInfo with
      | null -> None
      | fi -> Some <| fi.GetValue(declaringObject, BindingFlags.GetProperty, null, null, null)
