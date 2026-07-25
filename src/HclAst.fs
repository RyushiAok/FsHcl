namespace FsHcl.Hcl

/// Wrapper for values that should be rendered as raw HCL expressions.
type Expr = internal Expr of string

/// A value that can appear on the right-hand side of an HCL attribute.
type Value =
    | Null
    | String of string
    | Bool of bool
    | Number of string
    | Raw of string
    | Object of (string * Value) list
    | List of Value list
    | FunctionCall of name: string * arguments: Value list

/// A renderable HCL syntax node.
type Node =
    | Attribute of key: string * value: Value
    | Block of name: string * labels: string list * body: Node list
    | ObjectAssignment of name: string * body: Node list
    | ListAssignment of name: string * body: Node list
    | ListItem of Value
    | RawLine of string
    | Blank
    | Empty

/// Rendering controls for generated HCL.
type RenderOptions = {
    indentSize: int
    alignAttributes: bool
    trailingNewline: bool
}
