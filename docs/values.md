---
title: Values
category: Documentation
categoryindex: 1
index: 2
---

# Values

This page describes all value types that FsHcl supports.

All examples assume the following open declaration:

```fsharp
open FsHcl.Hcl
```

## Strings

`str` creates a quoted string value. FsHcl escapes special characters automatically.

```fsharp
attr "name" (str "example")
// name = "example"
```

## Template Strings

`templateStr` creates a string that preserves `${...}` interpolation sequences. FsHcl does not escape these sequences.

```fsharp
attr "name" (templateStr "${var.prefix}-instance")
// name = "${var.prefix}-instance"
```

### Extended String Interpolation (F# 8+)

Use extended interpolated strings (FS-1132) when you mix F# interpolation with HCL `${...}` or `%{...}` syntax. Prefix a triple-quoted string with `$$`. Single `{` and `%` become literal characters. Use `{{ }}` for F# interpolation.

```fsharp
let env = "prod"
attr "greeting" (templateStr $$"""%{if var.name}Hello, ${var.name} ({{env}})%{endif}""")
// greeting = "%{if var.name}Hello, ${var.name} (prod)%{endif}"
```

## Numbers

`number` creates a numeric value. It accepts any CLR numeric type.

```fsharp
attr "count" (number 3)
attr "ratio" (number 0.5)
// count = 3
// ratio = 0.5
```

## Booleans

`bool` creates a boolean value.

```fsharp
attr "enabled" (bool true)
// enabled = true
```

## Null

`null_` represents the HCL null value.

```fsharp
attr "value" null_
// value = null
```

## Raw Expressions

`raw` emits an unquoted value. Use `raw` for Terraform references and expressions.

```fsharp
attr "role" (raw "aws_iam_role.example.arn")
// role = aws_iam_role.example.arn
```

## Heredoc

`heredoc` creates a heredoc string with a delimiter and content.

```fsharp
attr "policy" (heredoc "EOF" "{\n  \"Version\": \"2012-10-17\"\n}")
// policy = <<EOF
// {
//   "Version": "2012-10-17"
// }
// EOF
```

`heredocIndent` creates an indented heredoc (`<<-` syntax).

```fsharp
attr "script" (heredocIndent "SCRIPT" "#!/bin/bash\necho hello")
// script = <<-SCRIPT
// #!/bin/bash
// echo hello
// SCRIPT
```

## Function Calls

`call` creates a function call expression. Pass the function name and a list of arguments.

```fsharp
attr "name" (call "coalesce" [ raw "var.name"; str "fallback" ])
// name = coalesce(var.name, "fallback")
```

## Conditional Expressions

`conditional` creates a ternary expression. Pass the condition, true expression, and false expression.

```fsharp
attr "count" (conditional "var.enabled" "1" "0")
// count = var.enabled ? 1 : 0
```

## For Expressions

### For-Tuple

`forTuple` creates a list comprehension expression.

```fsharp
attr "ids" (forTuple "v" "var.instances" "v.id")
// ids = [for v in var.instances : v.id]
```

`forTupleIf` adds a condition filter.

```fsharp
attr "ids" (forTupleIf "v" "var.instances" "v.id" "v.enabled")
// ids = [for v in var.instances : v.id if v.enabled]
```

### For-Object

`forObject` creates a map comprehension expression.

```fsharp
attr "map" (forObject "k" "v" "var.items" "k" "v.value")
// map = {for k, v in var.items : k => v.value}
```

`forObjectGroup` adds grouping mode (`...`).

```fsharp
attr "grouped" (forObjectGroup "k" "v" "var.items" "k" "v.value")
// grouped = {for k, v in var.items : k => v.value...}
```

## Object Values

Use the `obj` computation expression to build an object value.

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

Use `objField` to nest objects inside another object.

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

## List Values

`Value.List` creates a list value from a list of values.

```fsharp
attr "items" (Value.List [ str "a"; str "b" ])
// items = [
//   "a",
//   "b",
// ]
```

## CLR Value Conversion

### ofValue

`ofValue` converts an F# value to an HCL value. It accepts these types:

- Records and anonymous records (become HCL objects)
- Primitives: strings, numbers, booleans
- Sequences and arrays (become HCL lists)
- Dictionaries (become HCL objects)
- `Expr` values (become raw expressions)

```fsharp
attr "config" (ofValue {| Name = "example"; Count = 3 |})
// config = {
//   Name  = "example"
//   Count = 3
// }
```

### ofRecord

`ofRecord` converts a record to an HCL object value. It raises an error if the input is not a record.

```fsharp
attr "tags" (ofRecord {| Environment = "prod"; Team = "infra" |})
// tags = {
//   Environment = "prod"
//   Team        = "infra"
// }
```

### jsonencode

`jsonencode` wraps a converted value in a Terraform `jsonencode(...)` call.

```fsharp
attr "body" (
    jsonencode {|
        Version = "2012-10-17"
        Statement = [| {| Effect = "Allow"; Action = "s3:GetObject" |} |]
    |}
)
```

### expr

Use `expr` to mark a string as a raw HCL expression inside a record passed to `ofValue`.

```fsharp
attr "config" (ofValue {|
    Name = "example"
    Ref = expr "local.id"
|})
// config = {
//   Name = "example"
//   Ref  = local.id
// }
```
