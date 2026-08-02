# Usage Guide

All snippets assume the following open:

```fsharp
open FsHcl.Hcl
```

## Values

### Strings

```fsharp
attr "name" (str "example")
// name = "example"
```

### Template Strings

Preserves `${...}` interpolation sequences without escaping.

```fsharp
attr "name" (templateStr "${var.prefix}-instance")
// name = "${var.prefix}-instance"
```

#### Tip: Extended string interpolation (F# 8+)

When mixing F# interpolation with HCL's `${...}` or `%{...}` syntax,
use [extended interpolated strings (FS-1132)](https://github.com/fsharp/fslang-design/blob/main/FSharp-8.0/FS-1132-better-interpolated-triple-quoted-strings.md)
to avoid manual escaping.
Prefix a triple-quoted string with `$$` so that single `{` and `%` are literal
and `{{ }}` is used for F# interpolation:

```fsharp
let env = "prod"
attr "greeting" (templateStr $$"""%{if var.name}Hello, ${var.name} ({{env}})%{endif}""")
// greeting = "%{if var.name}Hello, ${var.name} (prod)%{endif}"
```

### Numbers

```fsharp
attr "count" (number 3)
attr "ratio" (number 0.5)
// count = 3
// ratio = 0.5
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

Emits unquoted values. Use for Terraform references.

```fsharp
attr "role" (raw "aws_iam_role.example.arn")
// role = aws_iam_role.example.arn
```

### Heredoc

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

```fsharp
attr "ids" (forTuple "v" "var.instances" "v.id")
// ids = [for v in var.instances : v.id]

attr "ids" (forTupleIf "v" "var.instances" "v.id" "v.enabled")
// ids = [for v in var.instances : v.id if v.enabled]

attr "map" (forObject "k" "v" "var.items" "k" "v.value")
// map = {for k, v in var.items : k => v.value}

attr "grouped" (forObjectGroup "k" "v" "var.items" "k" "v.value")
// grouped = {for k, v in var.items : k => v.value...}
```

### Object Values

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

### List Values

```fsharp
attr "items" (arr { str "a"; str "b" })
// items = [
//   "a",
//   "b",
// ]
```

### CLR Value Conversion

`ofValue` converts F# records, anonymous records, primitives, sequences, and dictionaries into HCL values.

```fsharp
attr "config" (ofValue {| Name = "example"; Count = 3 |})
// config = {
//   Name  = "example"
//   Count = 3
// }
```

`jsonencode` wraps the converted value in a Terraform `jsonencode(...)` call:

```fsharp
attr "body" (
    jsonencode {|
        Version = "2012-10-17"
        Statement = [| {| Effect = "Allow"; Action = "s3:GetObject" |} |]
    |}
)
```

## Syntax

### Blocks

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

### Attributes

```fsharp
attr "name" (str "example")
optAttr "description" (Some (str "A resource"))  // included
optAttr "deprecated" None                         // omitted
```

### Object Assignments

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
comment "Line comment"
blockComment [ "Multi-line"; "block comment" ]
// # Line comment
// /*
// Multi-line
// block comment
// */
```

### Loops

```fsharp
hcl {
    for region in [ "us-east-1"; "eu-west-1" ] do
        attr region (str region)
}
|> document
// us-east-1 = "us-east-1"
// eu-west-1 = "eu-west-1"
```

## Rendering

| Function      | Description                           |
| ------------- | ------------------------------------- |
| `document`    | Render with default options           |
| `Render.node` | Render a single node                  |
| `Render.join` | Render nodes separated by blank lines |
| `withOptions` | Render with custom options            |

### RenderOptions

| Option            | Default | Description                       |
| ----------------- | ------- | --------------------------------- |
| `indentSize`      | `2`     | Spaces per indent level           |
| `alignAttributes` | `true`  | Pad attribute keys to equal width |
| `trailingNewline` | `true`  | Append newline at end of output   |

```fsharp
let options = { indentSize = 4; alignAttributes = false; trailingNewline = false }
hcl { block "locals" { attr "x" (number 1) } } |> withOptions options
// locals {
//     x = 1
// }
```

## Terraform Helpers

Available with `open FsHcl.TerraformHcl`.

| Function                         | HCL Output                       |
| -------------------------------- | -------------------------------- |
| `terraform { ... }`              | `terraform { ... }`              |
| `provider "aws" { ... }`         | `provider "aws" { ... }`         |
| `resource "type" "name" { ... }` | `resource "type" "name" { ... }` |
| `data "type" "name" { ... }`     | `data "type" "name" { ... }`     |
| `variable "name" { ... }`        | `variable "name" { ... }`        |
| `output "name" { ... }`          | `output "name" { ... }`          |
| `locals { ... }`                 | `locals { ... }`                 |
| `module_ "name" { ... }`         | `module "name" { ... }`          |
| `import_ { ... }`                | `import { ... }`                 |
| `moved_ { ... }`                 | `moved { ... }`                  |
| `removed_ { ... }`               | `removed { ... }`                |
| `check "name" { ... }`           | `check "name" { ... }`           |

`to_`, `from_`, and `id` are used inside `import` / `moved` / `removed` blocks:

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

## Recommended Pattern

Define project-specific helper functions that wrap `attr`, `str`, `raw`, etc. This removes boilerplate and makes the HCL read declaratively.

```fsharp
module MyProject =
    open FsHcl.Hcl

    let name value = attr "name" (str value)
    let region value = attr "region" (str value)
    let role value = attr "role" (raw value)
    let function_name value = attr "function_name" (raw value)
    let depends_on values = list_ "depends_on" { for v in values do item (raw v) }
```

```fsharp
open MyProject
open FsHcl.Hcl
open FsHcl.TerraformHcl

hcl {
    resource "aws_iam_role" "deploy" {
        name "github-actions-deploy"
        role "data.aws_iam_policy_document.assume.json"
    }

    resource "aws_lambda_function_url" "api" {
        function_name "data.aws_lambda_function.api.function_name"
        attr "authorization_type" (str "NONE")
        depends_on [ "aws_lambda_function.api" ]
    }
}
|> document
// resource "aws_iam_role" "deploy" {
//   name = "github-actions-deploy"
//   role = data.aws_iam_policy_document.assume.json
// }
// resource "aws_lambda_function_url" "api" {
//   function_name      = data.aws_lambda_function.api.function_name
//   authorization_type = "NONE"
//   depends_on = [
//     aws_lambda_function.api,
//   ]
// }
```

Guidelines:

- Wrap attributes that appear more than once across resources.
- Use `str` for literal string values, `raw` for Terraform references — the helper makes this distinction explicit so callers don't need to think about it.
- Extract repeated nested blocks (e.g. `statement`, `condition`) into functions that take the varying parts as parameters.

See [examples/HelloLambda](https://github.com/ryushiaok/FsHcl/tree/main/examples/HelloLambda) for a full working example.
