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
list_ "allowed_accounts" [
    str "111111111111"
    str "222222222222"
]
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

### Top-level Blocks

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

### Dynamic Blocks

| Function                                           | Description                                   |
| -------------------------------------------------- | --------------------------------------------- |
| `dynamic_ "name" "collection" { ... }`             | `dynamic` block with `for_each` and `content` |
| `dynamicWithIterator "name" "coll" "iter" { ... }` | Same as above with a custom `iterator` name   |

The computation-expression body becomes the inner `content` block automatically:

```fsharp
resource "aws_security_group" "example" {
    dynamic_ "ingress" "var.rules" {
        attr "from_port" (raw "ingress.value.port")
        attr "to_port" (raw "ingress.value.port")
        attr "protocol" (str "tcp")
    }
}
// dynamic "ingress" {
//   for_each = var.rules
//   content {
//     from_port = ingress.value.port
//     to_port   = ingress.value.port
//     protocol  = "tcp"
//   }
// }
```

### Meta-arguments

| Function                     | HCL Output                        |
| ---------------------------- | --------------------------------- |
| `count (number 3)`           | `count = 3`                       |
| `for_each (raw "…")`         | `for_each = …`                    |
| `depends_on [r1; r2]`        | `depends_on = [\n  r1,\n  r2,\n]` |
| `provider_ "aws.west"`       | `provider = aws.west`             |
| `provisioner "type" { ... }` | `provisioner "type" { ... }`      |
| `connection { ... }`         | `connection { ... }`              |

### lifecycle Block

| Function                          | HCL Output                      |
| --------------------------------- | ------------------------------- |
| `lifecycle { ... }`               | `lifecycle { ... }`             |
| `create_before_destroy true`      | `create_before_destroy = true`  |
| `prevent_destroy true`            | `prevent_destroy = true`        |
| `ignore_changes ["tags"; "name"]` | `ignore_changes = [tags, name]` |
| `ignore_changes_all`              | `ignore_changes = all`          |
| `replace_triggered_by ["ref"]`    | `replace_triggered_by = [ref]`  |
| `precondition { ... }`            | `precondition { ... }`          |
| `postcondition { ... }`           | `postcondition { ... }`         |

```fsharp
resource "aws_instance" "example" {
    lifecycle {
        create_before_destroy true
        ignore_changes [ "tags" ]

        precondition {
            condition_ "var.instance_type != \"\""
            error_message "instance_type must not be empty"
        }
    }
}
```

### terraform Sub-blocks

| Function                     | HCL Output                    |
| ---------------------------- | ----------------------------- |
| `required_providers { ... }` | `required_providers { ... }`  |
| `required_version ">= 1.6"`  | `required_version = ">= 1.6"` |
| `backend "s3" { ... }`       | `backend "s3" { ... }`        |
| `cloud { ... }`              | `cloud { ... }`               |

```fsharp
terraform {
    required_version ">= 1.6.0"

    required_providers {
        object_ "aws" {
            attr "source" (str "hashicorp/aws")
            attr "version" (str "~> 5.0")
        }
    }

    backend "s3" {
        attr "bucket" (str "my-state")
    }
}
```

### variable / output Arguments

| Function                 | HCL Output               |
| ------------------------ | ------------------------ |
| `type_ "string"`         | `type = string`          |
| `default_ (str "value")` | `default = "value"`      |
| `description "text"`     | `description = "text"`   |
| `sensitive true`         | `sensitive = true`       |
| `nullable false`         | `nullable = false`       |
| `validation { ... }`     | `validation { ... }`     |
| `value_ (raw "expr")`    | `value = expr`           |
| `condition_ "expr"`      | `condition = expr`       |
| `error_message "text"`   | `error_message = "text"` |

```fsharp
variable "instance_type" {
    type_ "string"
    default_ (str "t3.micro")
    description "EC2 instance type"

    validation {
        condition_ "contains([\"t3.micro\", \"t3.small\"], var.instance_type)"
        error_message "Must be t3.micro or t3.small."
    }
}

output "db_password" {
    value_ (raw "aws_db_instance.main.password")
    description "The database password"
    sensitive true
}
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
        depends_on [ "aws_lambda_function.api" ]  // from TerraformHcl
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
- For repeated nested blocks (e.g. `statement`, `condition`), define thin `block` wrappers (`let statement = block "statement"`) rather than functions that hide the body.

See [examples/HelloLambda](https://github.com/ryushiaok/FsHcl/tree/main/examples/HelloLambda) for a full working example.
