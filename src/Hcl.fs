namespace FsHcl

open System.Globalization

/// Typed HCL document construction and rendering primitives.
module Hcl =
    /// A scalar or expression value that can appear on the right hand side of an HCL attribute.
    type Value =
        | String of string
        | Bool of bool
        | Number of decimal
        | Raw of string

    /// A renderable HCL node.
    type Node =
        | Line of string
        | Attr of key: string * value: Value
        | Block of opener: string * closer: string * body: Node list
        | Blank
        | Empty

    /// Rendering controls for generated HCL.
    type RenderOptions = {
        indentSize: int
        alignAttributes: bool
        trailingNewline: bool
    }

    /// Default Terraform-style rendering options.
    let defaultRenderOptions = {
        indentSize = 2
        alignAttributes = true
        trailingNewline = true
    }

    /// Escapes a string for use as an HCL string literal.
    let escapeString (s: string) =
        s
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t")
            .Replace("${", "$${")
            .Replace("%{", "%%{")

    /// Creates an HCL string value.
    let str = String

    /// Creates an HCL boolean value.
    let bool = Bool

    /// Creates an HCL number value.
    let number = Number

    /// Creates a raw HCL expression value.
    let raw = Raw

    let private renderValue =
        function
        | String value -> $"\"{escapeString value}\""
        | Bool true -> "true"
        | Bool false -> "false"
        | Number value -> value.ToString(CultureInfo.InvariantCulture)
        | Raw value -> value

    let private maxKeyWidth nodes =
        nodes
        |> List.choose (function
            | Attr(key, _) -> Some key.Length
            | _ -> None)
        |> function
            | [] -> 0
            | keys -> List.max keys

    let rec private renderNodeInner options indent maxKey =
        let pad = String.replicate indent " "

        function
        | Line value -> [ pad + value ]
        | Attr(key, value) ->
            let renderedValue = renderValue value

            if options.alignAttributes then
                [ $"{pad}{key.PadRight(maxKey)} = {renderedValue}" ]
            else
                [ $"{pad}{key} = {renderedValue}" ]
        | Block(opener, closer, body) ->
            let childMaxKey = maxKeyWidth body
            let childIndent = indent + options.indentSize

            [ pad + opener ]
            @ (body |> List.collect (renderNodeInner options childIndent childMaxKey))
            @ [ pad + closer ]
        | Blank -> [ "" ]
        | Empty -> []

    /// Renders an HCL document with explicit options.
    let renderWith options nodes =
        if options.indentSize < 0 then
            invalidArg (nameof options) "indentSize must be non-negative"

        let rendered =
            nodes
            |> List.collect (renderNodeInner options 0 (maxKeyWidth nodes))
            |> String.concat "\n"

        if options.trailingNewline then rendered + "\n" else rendered

    /// Renders an HCL document with default options.
    let render nodes = renderWith defaultRenderOptions nodes

    /// Renders a single HCL node with default options.
    let renderNode node = render [ node ]

    /// Renders multiple top-level nodes separated by a blank line.
    let renderJoin nodes =
        nodes |> List.map renderNode |> String.concat "\n"

    /// Computation expression builder for top-level HCL documents.
    type BodyBuilder() =
        member _.Yield(node: Node) = [ node ]
        member _.YieldFrom(nodes: Node list) = nodes
        member _.Combine(a: Node list, b: unit -> Node list) = a @ b ()
        member _.Delay(f: unit -> Node list) = f
        member _.Run(f: unit -> Node list) : Node list = f ()
        member _.Zero() : Node list = []
        member _.For(xs: 'a seq, f: 'a -> Node list) = xs |> Seq.collect f |> Seq.toList

    /// Computation expression builder for HCL blocks.
    type BlockBuilder(opener: string, closer: string) =
        member _.Yield(node: Node) = [ node ]
        member _.YieldFrom(nodes: Node list) = nodes
        member _.Combine(a: Node list, b: unit -> Node list) = a @ b ()
        member _.Delay(f: unit -> Node list) = f
        member _.Run(f: unit -> Node list) : Node = Block(opener, closer, f ())
        member _.Zero() : Node list = []
        member _.For(xs: 'a seq, f: 'a -> Node list) = xs |> Seq.collect f |> Seq.toList

    /// HCL document computation expression.
    let hcl = BodyBuilder()

    /// Creates an unlabeled HCL block.
    let block name =
        BlockBuilder($"{name} {{", "}")

    /// Creates an HCL block with string labels.
    let blockWithLabels name labels =
        let renderedLabels =
            labels
            |> List.map (fun label -> $"\"{escapeString label}\"")
            |> String.concat " "

        if renderedLabels = "" then
            block name
        else
            BlockBuilder($"{name} {renderedLabels} {{", "}")

    /// Creates an object assignment block, for example `variables = { ... }`.
    let object_ name = BlockBuilder($"{name} = {{", "}")

    /// Creates a list assignment block, for example `patterns = [ ... ]`.
    let list_ name = BlockBuilder($"{name} = [", "]")

    /// Creates an HCL attribute.
    let attr key value = Attr(key, value)

    /// Creates an optional HCL attribute, omitted when `None`.
    let optAttr key =
        function
        | Some value -> Attr(key, value)
        | None -> Empty

    /// Creates a list item.
    let item value = Line($"{renderValue value},")

    /// Creates a blank line.
    let blank = Blank
