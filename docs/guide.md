# Usage Guide

This guide covers all features of FsHcl with examples.
Every snippet assumes the following opens:

```fsharp
open FsHcl.Hcl.Render
open FsHcl.Hcl.Syntax
open FsHcl.Hcl.Values
```

## Values

### Strings

`str` creates a quoted string value. Special characters are escaped automatically.

```fsharp
attr "name" (str "example")
// name = "example"
```

### Template Strings

`templateStr` preserves `${...}` interpolation sequences without escaping.

```fsharp
attr "name" (templateStr "${var.prefix}-instance")
// name = "${var.prefix}-instance"
```

### Numbers

`number` accepts any CLR numeric type (`int`, `float`, `decimal`, etc.).

```fsharp
attr "count" (number 3)
attr "ratio" (number 0.5)
attr "price" (number 9.99m)
// count = 3
// ratio = 0.5
// price = 9.99
```

### Booleans

```fsharp
attr "enabled" (bool true)
// enabled = true
```

### Null

```fsharp
attr "value" null_
// value = null
```

### Raw Expressions

`raw` emits a value without quoting, useful for Terraform references.

```fsharp
attr "role" (raw "aws_iam_role.example.arn")
// role = aws_iam_role.example.arn
```

### Heredoc Strings

`heredoc` creates a `<<` heredoc. `heredocIndent` creates a `<<-` indented heredoc.

```fsharp
attr "policy" (heredoc "EOF" "{\n  \"Version\": \"2012-10-17\"\n}")
// policy = <<EOF
// {
//   "Version": "2012-10-17"
// }
// EOF

attr "script" (heredocIndent "SCRIPT" "#!/bin/bash\necho hello")
// script = <<-SCRIPT
// #!/bin/bash
// echo hello
// SCRIPT
```

### Function Calls

`call` creates an HCL function call expression.

```fsharp
attr "name" (call "coalesce" [ raw "var.name"; str "fallback" ])
// name = coalesce(var.name, "fallback")
```

### Conditional Expressions

```fsharp
attr "count" (conditional "var.enabled" "1" "0")
// count = var.enabled ? 1 : 0
```

### For Expressions

For-tuple creates a list comprehension:

```fsharp
attr "ids" (forTuple "v" "var.instances" "v.id")
// ids = [for v in var.instances : v.id]

attr "ids" (forTupleIf "v" "var.instances" "v.id" "v.enabled")
// ids = [for v in var.instances : v.id if v.enabled]
```

For-object creates a map comprehension:

```fsharp
attr "map" (forObject "k" "v" "var.items" "k" "v.value")
// map = {for k, v in var.items : k => v.value}

attr "grouped" (forObjectGroup "k" "v" "var.items" "k" "v.value")
// grouped = {for k, v in var.items : k => v.value...}

attr "filtered" (forObjectIf "k" "v" "var.items" "k" "v.value" "v.enabled")
// filtered = {for k, v in var.items : k => v.value if v.enabled}
```

### Object Values

`obj` builds an object value with typed fields.

```fsharp
attr "tags" (
    obj {
        stringField "Name" "example"
        boolField "Managed" true
        numberField "Priority" 1
        rawField "ref" "local.id"
    }
)
// tags = {
//   Name     = "example"
//   Managed  = true
//   Priority = 1
//   ref      = local.id
// }
```

Use `objField` to nest objects:

```fsharp
attr "config" (
    obj {
        objField "nested" {
            stringField "name" "example"
        }
    }
)
// config = {
//   nested = {
//     name = "example"
//   }
// }
```

An empty object renders on one line:

```fsharp
attr "tags" (obj { () })
// tags = {}
```

### List Values

`arr` builds a list value:

```fsharp
attr "items" (arr { str "a"; str "b" })
// items = [
//   "a",
//   "b",
// ]
```

An empty list renders on one line:

```fsharp
attr "items" (arr { () })
// items = []
```

### CLR Value Conversion

`ofValue` converts F# and CLR values to HCL values.
It supports primitives, strings, records, anonymous records, sequences, and dictionaries.
`expr` marks a string as a raw expression inside converted values.

```fsharp
attr "config" (ofValue {| Name = "example"; Count = 3 |})
```

`jsonencode` wraps the converted value in a Terraform `jsonencode(...)` call:

```fsharp
attr "body" (
    jsonencode {|
        Version = "2012-10-17"
        Statement = [|
            {| Effect = "Allow"; Action = "s3:GetObject" |}
        |]
    |}
)
```

## Syntax

### Blocks

`block` creates an unlabeled block.
`blockWithLabels` creates a block with string labels.

```fsharp
hcl {
    block "locals" {
        attr "region" (str "us-east-1")
    }

    blockWithLabels "resource" [ "aws_s3_bucket"; "example" ] {
        attr "bucket" (str "my-bucket")
    }
}
|> document
// locals {
//   region = "us-east-1"
// }
// resource "aws_s3_bucket" "example" {
//   bucket = "my-bucket"
// }
```

Empty blocks render on one line:

```fsharp
blockWithLabels "data" [ "aws_caller_identity"; "current" ] { }
// data "aws_caller_identity" "current" {}
```

### Attributes

`attr` creates a key-value attribute.
`optAttr` creates an attribute that is omitted when `None`.

```fsharp
hcl {
    attr "name" (str "example")
    optAttr "description" (Some (str "A resource"))
    optAttr "deprecated" None
}
|> document
// name        = "example"
// description = "A resource"
```

### Object Assignments

`object_` creates a `name = { ... }` assignment:

```fsharp
object_ "variables" {
    attr "region" (str "us-east-1")
    attr "env" (str "prod")
}
// variables = {
//   region = "us-east-1"
//   env    = "prod"
// }
```

### List Assignments

`list_` creates a `name = [ ... ]` assignment with `item` entries:

```fsharp
list_ "allowed_accounts" {
    item (str "111111111111")
    item (str "222222222222")
}
// allowed_accounts = [
//   "111111111111",
//   "222222222222",
// ]
```

### Comments

```fsharp
hcl {
    comment "This is a line comment"
    blockComment [ "Multi-line"; "block comment" ]
}
|> document
// # This is a line comment
// /*
// Multi-line
// block comment
// */
```

### Blank Lines and Raw Lines

```fsharp
hcl {
    attr "a" (number 1)
    blank
    attr "b" (number 2)
    rawLine "# hand-written line"
}
|> document
// a = 1
//
// b = 2
// # hand-written line
```

### Loops in Computation Expressions

`for` loops generate nodes dynamically:

```fsharp
let regions = [ "us-east-1"; "eu-west-1" ]

hcl {
    for region in regions do
        attr region (str region)
}
|> document
// us-east-1 = "us-east-1"
// eu-west-1 = "eu-west-1"
```

## Rendering

### `document`

Renders a list of nodes with default options (2-space indent, aligned attributes, trailing newline).

```fsharp
hcl { attr "name" (str "example") } |> document
```

### `node`

Renders a single node:

```fsharp
attr "name" (str "example") |> Render.node
```

### `join`

Renders a list of top-level nodes separated by blank lines:

```fsharp
[ block "a" { attr "x" (number 1) }
  block "b" { attr "y" (number 2) } ]
|> Render.join
// a {
//   x = 1
// }
//
// b {
//   y = 2
// }
```

### `withOptions`

Customizes rendering with `RenderOptions`:

```fsharp
let options = {
    indentSize = 4
    alignAttributes = false
    trailingNewline = false
}

hcl {
    block "locals" {
        attr "long_name" (str "value")
        attr "x" (number 1)
    }
}
|> withOptions options
// locals {
//     long_name = "value"
//     x = 1
// }
```

| Option | Default | Description |
|--------|---------|-------------|
| `indentSize` | `2` | Number of spaces per indent level |
| `alignAttributes` | `true` | Pad attribute keys to the same width |
| `trailingNewline` | `true` | Append a newline at the end of the output |

## Terraform Helpers

`FsHcl.TerraformHcl` provides shorthand builders for Terraform blocks.
Add `open FsHcl.TerraformHcl` to use them.

```fsharp
open FsHcl.TerraformHcl
```

### Block Builders

| Function | HCL Output |
|----------|-----------|
| `terraform { ... }` | `terraform { ... }` |
| `provider "aws" { ... }` | `provider "aws" { ... }` |
| `resource "type" "name" { ... }` | `resource "type" "name" { ... }` |
| `data "type" "name" { ... }` | `data "type" "name" { ... }` |
| `variable "name" { ... }` | `variable "name" { ... }` |
| `output "name" { ... }` | `output "name" { ... }` |
| `locals { ... }` | `locals { ... }` |
| `module_ "name" { ... }` | `module "name" { ... }` |
| `import_ { ... }` | `import { ... }` |
| `moved_ { ... }` | `moved { ... }` |
| `removed_ { ... }` | `removed { ... }` |
| `check "name" { ... }` | `check "name" { ... }` |

### Movement Attributes

`to_` and `from_` create raw expression attributes for `import`, `moved`, and `removed` blocks.
`id` creates a string `id` attribute for `import` blocks.

```fsharp
hcl {
    moved_ {
        from_ "aws_s3_bucket.old"
        to_ "aws_s3_bucket.new"
    }

    import_ {
        to_ "aws_s3_bucket.existing"
        id "my-bucket-name"
    }
}
|> document
// moved {
//   from = aws_s3_bucket.old
//   to   = aws_s3_bucket.new
// }
// import {
//   to = aws_s3_bucket.existing
//   id = "my-bucket-name"
// }
```

## Building Project-Specific Helpers

FsHcl provides generic building blocks — `attr`, `str`, `raw`, and so on.
When you use them directly, every attribute carries the same `attr "key" (str "value")` boilerplate.
The recommended approach is to **define thin helper functions** that wrap these primitives for your project's vocabulary,
so the final HCL definition reads like a declarative specification rather than a sequence of API calls.

### Before: raw API calls

```fsharp
hcl {
    resource "aws_iam_role" "deploy" {
        attr "name" (str "github-actions-deploy")
        attr "assume_role_policy" (raw "data.aws_iam_policy_document.assume_role.json")
    }

    resource "aws_lambda_function_url" "api" {
        attr "function_name" (raw "data.aws_lambda_function.api.function_name")
        attr "authorization_type" (str "NONE")
    }
}
|> document
```

Every line has `attr`, `str`, and `raw` noise.
The reader must parse each call to understand what the attribute means.

### After: project-specific helpers

First, define helpers that give each attribute a meaningful name:

```fsharp
module MyProject =
    open FsHcl.Hcl.Syntax
    open FsHcl.Hcl.Values

    // String attributes
    let name value = attr "name" (str value)
    let authorization_type value = attr "authorization_type" (str value)

    // Expression attributes
    let assume_role_policy value = attr "assume_role_policy" (raw value)
    let function_name value = attr "function_name" (raw value)

    // List attribute
    let depends_on values =
        list_ "depends_on" {
            for v in values do
                item (raw v)
        }

    // Reusable nested block
    let statement body = block "statement" { yield! body }
```

Then the HCL definition becomes declarative:

```fsharp
open MyProject

hcl {
    resource "aws_iam_role" "deploy" {
        name "github-actions-deploy"
        assume_role_policy "data.aws_iam_policy_document.assume_role.json"
    }

    resource "aws_lambda_function_url" "api" {
        function_name "data.aws_lambda_function.api.function_name"
        authorization_type "NONE"
    }
}
|> document
```

The `attr`, `str`, and `raw` details are hidden.
Each line states what it means directly.

### Guidelines

**Wrap attributes that appear more than once.**
If you use `attr "name" (str ...)` in several resources, define `let name value = attr "name" (str value)`.

**Distinguish string attributes from expression attributes.**
HCL attributes fall into two categories:
literal values (`str`) and Terraform references (`raw`).
The helper's implementation makes this distinction explicit so callers don't need to think about it.

```fsharp
// String — the value is quoted
let bucket value = attr "bucket" (str value)

// Expression — the value is a Terraform reference
let role value = attr "role" (raw value)
```

**Extract repeated blocks.**
If your project has multiple `statement` or `condition` blocks with the same structure,
define a helper that takes the varying parts as parameters:

```fsharp
let allowActions actions resources =
    block "statement" {
        attr "effect" (str "Allow")

        list_ "actions" {
            for a in actions do
                item (str a)
        }

        list_ "resources" {
            for r in resources do
                item (str r)
        }
    }
```

**Keep helpers in a module per project or per Terraform workspace.**
This mirrors how Terraform organizes `.tf` files by concern.

See [examples/HelloLambda](https://github.com/ryushiaok/FsHcl/tree/main/examples/HelloLambda) for a full working example that follows this pattern.
