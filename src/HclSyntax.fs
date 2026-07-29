namespace FsHcl.Hcl

open FsHcl.Hcl.Values

/// Computation expressions and constructors for HCL syntax nodes.
[<AutoOpen>]
module Syntax =
    [<AbstractClass>]
    type NodeCollectionBuilder() =
        member _.Yield(node: Node) = [ node ]
        member _.YieldFrom(nodes: Node list) = nodes
        member _.Combine(left, right: unit -> Node list) = left @ right ()
        member _.Delay(build: unit -> Node list) = build
        member _.Zero() : Node list = []

        member _.For(values: 'a seq, build: 'a -> Node list) =
            values |> Seq.collect build |> Seq.toList

    type BodyBuilder() =
        inherit NodeCollectionBuilder()

        member _.Run(build: unit -> Node list) = build ()

    type ContainerBuilder internal (buildNode: Node list -> Node) =
        inherit NodeCollectionBuilder()

        member _.Run(build: unit -> Node list) = build () |> buildNode

    /// HCL document computation expression.
    let hcl = BodyBuilder()

    /// Creates an unlabeled HCL block.
    let block name =
        ContainerBuilder(fun body -> Block(name, [], body))

    /// Creates an HCL block with string labels.
    let blockWithLabels name labels =
        ContainerBuilder(fun body -> Block(name, labels, body))

    /// Creates an object assignment, for example `variables = { ... }`.
    let object_ name =
        ContainerBuilder(fun body -> ObjectAssignment(name, body))

    /// Creates a list assignment, for example `patterns = [ ... ]`.
    let list_ name =
        ContainerBuilder(fun body -> ListAssignment(name, body))

    /// Creates an HCL attribute.
    let attr key value = Attribute(key, value)

    /// Creates an optional HCL attribute, omitted when `None`.
    let optAttr key =
        function
        | Some value -> Attribute(key, value)
        | None -> Empty

    /// Creates a list item.
    let item value = ListItem value

    /// Creates an unparsed HCL line.
    let rawLine value = RawLine value

    /// Creates a line comment (`# ...`).
    let comment text = LineComment text

    /// Creates a block comment (`/* ... */`).
    let blockComment lines = BlockComment lines

    /// Creates a blank line.
    let blank = Blank
