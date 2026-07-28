namespace FsHcl.Hcl

/// Wrapper for values that should be rendered as raw HCL expressions.
type Expr = internal Expr of string

/// A value that can appear on the right-hand side of an HCL attribute.
type Value =
    | Null
    | String of string
    | TemplateString of string
    | Bool of bool
    | Number of string
    | Raw of string
    | Heredoc of delimiter: string * content: string * indented: bool
    | Object of (string * Value) list
    | List of Value list
    | FunctionCall of name: string * arguments: Value list
    | Conditional of condition: string * trueExpr: string * falseExpr: string
    | ForTuple of variable: string * collection: string * expr: string * condition: string option
    | ForObject of
        keyVar: string *
        valueVar: string *
        collection: string *
        keyExpr: string *
        valueExpr: string *
        grouping: bool *
        condition: string option

/// A renderable HCL syntax node.
type Node =
    | Attribute of key: string * value: Value
    | Block of name: string * labels: string list * body: Node list
    | ObjectAssignment of name: string * body: Node list
    | ListAssignment of name: string * body: Node list
    | ListItem of Value
    | LineComment of string
    | BlockComment of string list
    | RawLine of string
    | Blank
    | Empty

/// Rendering controls for generated HCL.
type RenderOptions = {
    indentSize: int
    alignAttributes: bool
    trailingNewline: bool
}
