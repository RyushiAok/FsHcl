namespace FsHcl.Hcl

open System
open System.Collections
open System.Globalization
open System.Reflection
open Microsoft.FSharp.Reflection

/// HCL value constructors and conversions.
module Values =
    let private recordFlags =
        BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.Instance

    /// Escapes a string for use as an HCL string literal.
    let escapeString (value: string) =
        value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t")
            .Replace("${", "$${")
            .Replace("%{", "%%{")

    /// Creates an HCL string value.
    let str value = Value.String value

    /// Creates an HCL boolean value.
    let bool value = Value.Bool value

    let private isNumericType (valueType: Type) =
        match Type.GetTypeCode valueType with
        | TypeCode.Byte
        | TypeCode.SByte
        | TypeCode.Int16
        | TypeCode.UInt16
        | TypeCode.Int32
        | TypeCode.UInt32
        | TypeCode.Int64
        | TypeCode.UInt64
        | TypeCode.Single
        | TypeCode.Double
        | TypeCode.Decimal -> true
        | _ -> false

    let private numericLiteral argumentName (value: obj) =
        if not (isNumericType (value.GetType())) then
            invalidArg argumentName $"Expected a numeric value, but got {value.GetType().FullName}"

        match value with
        | :? double as number when Double.IsNaN number || Double.IsInfinity number ->
            invalidArg argumentName "HCL numbers must be finite"
        | :? single as number when Single.IsNaN number || Single.IsInfinity number ->
            invalidArg argumentName "HCL numbers must be finite"
        | _ -> Convert.ToString(value, CultureInfo.InvariantCulture)

    /// Creates an HCL number value from a CLR numeric value.
    let number value =
        match box value with
        | null -> nullArg (nameof value)
        | boxed -> boxed |> numericLiteral (nameof value) |> Value.Number

    /// Creates a raw HCL expression value.
    let raw value = Value.Raw value

    /// Creates an HCL null value.
    let null_ = Value.Null

    /// Marks a string as a raw HCL expression when converting CLR or F# values.
    let expr value = Expr value

    /// Creates an HCL function call expression.
    let call name arguments = Value.FunctionCall(name, arguments)

    let private recordFields (value: obj) =
        FSharpType.GetRecordFields(value.GetType(), recordFlags)
        |> Array.map (fun property -> property.Name, property.GetValue value)
        |> Array.toList

    let private dictionaryFields (values: IDictionary) =
        values
        |> Seq.cast<DictionaryEntry>
        |> Seq.map (fun entry ->
            match entry.Key with
            | :? string as key -> key, entry.Value
            | null -> invalidArg (nameof values) "HCL object keys must not be null"
            | key -> invalidArg (nameof values) $"HCL object keys must be strings, but got {key.GetType().FullName}")
        |> Seq.toList

    let rec private valueOfObj (value: obj | null) : Value =
        match value with
        | null -> Value.Null
        | :? Value as value -> value
        | :? Expr as expression ->
            let (Expr value) = expression
            Value.Raw value
        | :? string as value -> Value.String value
        | :? bool as value -> Value.Bool value
        | :? IDictionary as values ->
            values
            |> dictionaryFields
            |> List.map (fun (key, value) -> key, valueOfObj value)
            |> Value.Object
        | :? IEnumerable as values ->
            values |> Seq.cast<obj> |> Seq.map valueOfObj |> Seq.toList |> Value.List
        | value when isNumericType (value.GetType()) ->
            value |> numericLiteral (nameof value) |> Value.Number
        | value when FSharpType.IsRecord(value.GetType(), recordFlags) ->
            value
            |> recordFields
            |> List.map (fun (key, fieldValue) -> key, valueOfObj fieldValue)
            |> Value.Object
        | value -> invalidArg (nameof value) $"Unsupported HCL value type: {value.GetType().FullName}"

    /// Converts a record, primitive value, sequence, dictionary, or `Expr` into an HCL value.
    let ofValue value = valueOfObj (box value)

    /// Converts a record into an HCL object value.
    let ofRecord record =
        match ofValue record with
        | Value.Object _ as value -> value
        | _ -> invalidArg (nameof record) "Expected a record"

    /// Creates a Terraform `jsonencode(...)` expression from a CLR or F# value.
    let jsonencode value = call "jsonencode" [ ofValue value ]

    type ObjectBuilder() =
        member _.Yield(field: string * Value) = [ field ]
        member _.YieldFrom(fields: (string * Value) list) = fields
        member _.Combine(left, right: unit -> (string * Value) list) = left @ right ()
        member _.Delay(build: unit -> (string * Value) list) = build
        member _.Run(build: unit -> (string * Value) list) = Value.Object(build ())
        member _.Zero() : (string * Value) list = []
        member _.For(values: 'a seq, build: 'a -> (string * Value) list) =
            values |> Seq.collect build |> Seq.toList

    type ListBuilder() =
        member _.Yield(value: Value) = [ value ]
        member _.YieldFrom(values: Value list) = values
        member _.Combine(left, right: unit -> Value list) = left @ right ()
        member _.Delay(build: unit -> Value list) = build
        member _.Run(build: unit -> Value list) = Value.List(build ())
        member _.Zero() : Value list = []
        member _.For(values: 'a seq, build: 'a -> Value list) = values |> Seq.collect build |> Seq.toList

    type ObjectFieldBuilder(key: string) =
        inherit ObjectBuilder()

        member _.Run(build: unit -> (string * Value) list) = key, Value.Object(build ())

    /// HCL object value computation expression.
    let obj = ObjectBuilder()

    /// HCL list value computation expression.
    let arr = ListBuilder()

    /// Creates an object-valued field with a computation expression.
    let objField key = ObjectFieldBuilder(key)

    /// Creates an object field.
    let field key value = key, value

    /// Creates a string object field.
    let stringField key value = field key (str value)

    /// Creates a boolean object field.
    let boolField key value = field key (bool value)

    /// Creates a number object field.
    let numberField key value = field key (number value)

    /// Creates a raw expression object field.
    let rawField key value = field key (raw value)
