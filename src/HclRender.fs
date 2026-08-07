namespace FsHcl.Hcl

open System
open FsHcl.Hcl.Values

/// HCL rendering functions.
[<AutoOpen>]
module Render =
    /// Default Terraform-style rendering options.
    let defaults = {
        indentSize = 2
        alignAttributes = true
        trailingNewline = true
    }

    let private padding width = String.replicate width " "

    let private maxKeyWidth nodes =
        nodes
        |> List.choose (function
            | Attribute(key, _) -> Some key.Length
            | _ -> None)
        |> function
            | [] -> 0
            | widths -> List.max widths

    /// Splits nodes into groups separated by Blank lines,
    /// pairing each node with its group's max attribute key width.
    let private withGroupMaxKey nodes =
        let rec split acc current =
            function
            | [] -> List.rev (List.rev current :: acc)
            | Blank :: rest -> split (List.rev (Blank :: current) :: acc) [] rest
            | node :: rest -> split acc (node :: current) rest

        split [] [] nodes
        |> List.collect (fun group ->
            let maxKey = maxKeyWidth group
            group |> List.map (fun node -> (node, maxKey)))

    let rec private renderFields options indent fields =
        let maxKey =
            if options.alignAttributes then
                fields |> List.map (fst >> String.length) |> List.fold max 0
            else
                0

        fields
        |> List.map (fun (key, value) -> key, maxKey, value)
        |> List.map (fun (key, maxKey, value) ->
            let renderedKey = if options.alignAttributes then key.PadRight maxKey else key
            $"{padding indent}{renderedKey} = {renderValueAt options indent value}")

    and private renderValueAt options indent value =
        match value with
        | Null -> "null"
        | String value -> $"\"{escapeString value}\""
        | TemplateString value -> $"\"{value}\""
        | Bool true -> "true"
        | Bool false -> "false"
        | Number value -> value
        | Raw value -> value
        | Heredoc(delimiter, content, indented) ->
            let marker = if indented then "<<-" else "<<"
            $"{marker}{delimiter}\n{content}\n{padding indent}{delimiter}"
        | Object [] -> "{}"
        | Object fields ->
            let childIndent = indent + options.indentSize

            let body =
                renderFields options childIndent fields
                |> String.concat "\n"

            "{\n" + body + "\n" + padding indent + "}"
        | List [] -> "[]"
        | List values ->
            let childIndent = indent + options.indentSize

            let body =
                values
                |> List.map (fun value -> $"{padding childIndent}{renderValueAt options childIndent value},")
                |> String.concat "\n"

            "[\n" + body + "\n" + padding indent + "]"
        | FunctionCall(name, arguments) ->
            let renderedArguments =
                arguments
                |> List.map (renderValueAt options indent)
                |> String.concat ", "

            $"{name}({renderedArguments})"
        | Conditional(condition, trueExpr, falseExpr) -> $"{condition} ? {trueExpr} : {falseExpr}"
        | ForTuple(variable, collection, expr, condition) ->
            let cond =
                match condition with
                | Some c -> $" if {c}"
                | None -> ""

            $"[for {variable} in {collection} : {expr}{cond}]"
        | ForObject(keyVar, valueVar, collection, keyExpr, valueExpr, grouping, condition) ->
            let cond =
                match condition with
                | Some c -> $" if {c}"
                | None -> ""

            let dots = if grouping then "..." else ""
            $"{{for {keyVar}, {valueVar} in {collection} : {keyExpr} => {valueExpr}{dots}{cond}}}"

    let rec private renderContainer options indent opener closer body =
        let pad = padding indent
        let childIndent = indent + options.indentSize

        let renderedBody =
            body
            |> withGroupMaxKey
            |> List.collect (fun (node, maxKey) -> renderNode options childIndent maxKey node)

        match renderedBody with
        | [] -> [ $"{pad}{opener}{closer}" ]
        | renderedBody -> [ pad + opener ] @ renderedBody @ [ pad + closer ]

    and private renderNode options indent maxKey node =
        let pad = padding indent

        match node with
        | Attribute(key, value) ->
            let renderedKey = if options.alignAttributes then key.PadRight maxKey else key
            [ $"{pad}{renderedKey} = {renderValueAt options indent value}" ]
        | Block(name, labels, body) ->
            let renderedLabels =
                labels
                |> List.map (fun label -> $"\"{escapeString label}\"")
                |> String.concat " "

            let header =
                if renderedLabels = "" then
                    $"{name} "
                else
                    $"{name} {renderedLabels} "

            renderContainer options indent (header + "{") "}" body
        | ObjectAssignment(name, body) -> renderContainer options indent ($"{name} = {{") "}" body
        | ListAssignment(name, values) ->
            let childIndent = indent + options.indentSize

            match values with
            | [] -> [ $"{pad}{name} = []" ]
            | values ->
                let body =
                    values
                    |> List.map (fun value ->
                        $"{padding childIndent}{renderValueAt options childIndent value},")

                [ $"{pad}{name} = [" ] @ body @ [ $"{pad}]" ]
        | LineComment text -> [ $"{pad}# {text}" ]
        | BlockComment lines ->
            [ $"{pad}/*" ]
            @ (lines
               |> List.map (fun line -> if line = "" then $"{pad}" else $"{pad}{line}"))
            @ [ $"{pad}*/" ]
        | RawLine value -> [ pad + value ]
        | Blank -> [ "" ]
        | Empty -> []

    /// Renders an HCL document with explicit options.
    let withOptions options nodes =
        if options.indentSize < 0 then
            invalidArg (nameof options) "indentSize must be non-negative"

        let rendered =
            nodes
            |> withGroupMaxKey
            |> List.collect (fun (node, maxKey) -> renderNode options 0 maxKey node)
            |> String.concat "\n"

        if options.trailingNewline then
            rendered + "\n"
        else
            rendered

    /// Renders an HCL document with default options.
    let document nodes = withOptions defaults nodes

    /// Renders a single HCL node with default options.
    let node value = document [ value ]

    /// Renders top-level nodes separated by a blank line.
    let join nodes =
        nodes |> List.map node |> String.concat "\n"
